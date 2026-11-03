using System.Linq;
using Photon.Pun;
using Pockets.Api;
using UnityEngine;

namespace Pockets.Net;

/// <summary>
/// Runtime component added to every valuable on every client. It carries the store RPCs and hides the
/// valuable the way <c>ItemEquippable</c> hides an equipped item (shrunk, colliders off, grab released),
/// since the vanilla inventory only accepts items.
/// Flow: the owner asks the master (<see cref="RequestStoreRpc"/>), the master validates with
/// <see cref="PocketSync.CanStore"/> and broadcasts <see cref="StoredRpc"/>, every client applies it.
/// </summary>
internal sealed class PocketedValuable : MonoBehaviourPun
{
	private const float HiddenScaleFactor = 0.01f;

	private PhysGrabObject physGrabObject = null!;
	private ValuableObject valuable = null!;
	private Rigidbody? body;
	private Collider[] colliders = new Collider[0];
	private bool[] colliderStates = new bool[0];
	private Vector3 originalScale = Vector3.one;
	private bool bodyWasKinematic;

	internal ValuableObject Valuable => valuable;

	internal PhysGrabObject PhysGrabObject => physGrabObject;

	internal bool IsPocketed { get; private set; }

	/// <summary>The player whose pocket holds this valuable (valid while <see cref="IsPocketed"/>).</summary>
	internal int PlayerKey { get; private set; } = PlayerKeys.NoPlayer;

	/// <summary>The slot index holding this valuable (valid while <see cref="IsPocketed"/>).</summary>
	internal int Slot { get; private set; } = -1;

	/// <summary>Opaque key for the pocket registry: the view id in multiplayer, the Unity instance id in singleplayer (where every view id is 0).</summary>
	internal int Key => SemiFunc.IsMultiplayer() ? photonView.ViewID : gameObject.GetInstanceID();

	private void Awake()
	{
		physGrabObject = GetComponent<PhysGrabObject>();
		valuable = GetComponent<ValuableObject>();
		body = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		// Vanilla re-asserts the deactivation every frame while an item is equipped.
		if (IsPocketed)
		{
			physGrabObject.OverrideDeactivate();
		}
	}

	private void OnDestroy()
	{
		if (IsPocketed)
		{
			PocketsApi.Forget(PlayerKey, Slot);
		}
	}

	/// <summary>Called by the local player's slot key. Asks the master to store this valuable in <paramref name="slot"/>.</summary>
	internal void RequestStore(int slot)
	{
		PlayerAvatar? avatar = PlayerAvatar.instance;
		if (avatar == null)
		{
			return;
		}

		int playerKey = PlayerKeys.Of(avatar);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC(nameof(RequestStoreRpc), RpcTarget.MasterClient, slot, playerKey);
		}
		else
		{
			ProcessStoreRequest(slot, playerKey);
		}
	}

	[PunRPC]
	private void RequestStoreRpc(int slot, int playerKey, PhotonMessageInfo info = default)
	{
		if (!SemiFunc.IsMasterClient())
		{
			return;
		}

		PlayerAvatar? avatar = PlayerKeys.Resolve(playerKey);
		if (avatar == null || !SemiFunc.MasterAndOwnerOnlyRPC(info, avatar.photonView))
		{
			return;
		}

		ProcessStoreRequest(slot, playerKey);
	}

	private void ProcessStoreRequest(int slot, int playerKey)
	{
		if (!PocketSync.CanStore(this, slot, playerKey, out string reason))
		{
			PocketsPlugin.Logger.LogInfo($"Store refused for {name}: {reason}");
			return;
		}

		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC(nameof(StoredRpc), RpcTarget.All, playerKey, slot);
		}
		else
		{
			ApplyStored(playerKey, slot);
		}
	}

	[PunRPC]
	private void StoredRpc(int playerKey, int slot, PhotonMessageInfo info = default)
	{
		if (!SemiFunc.MasterOnlyRPC(info))
		{
			return;
		}

		ApplyStored(playerKey, slot);
	}

	private void ApplyStored(int playerKey, int slot)
	{
		PlayerPockets pockets = PocketsApi.GetOrCreate(playerKey);
		if (IsPocketed || !pockets.State.TryStore(slot, Key))
		{
			PocketsPlugin.Logger.LogWarning($"Could not register {name} in slot {slot} of player {playerKey}; ignoring the store.");
			return;
		}

		pockets.Valuables[slot] = valuable;
		IsPocketed = true;
		PlayerKey = playerKey;
		Slot = slot;

		Hide();

		PocketsApi.RaiseStored(new PocketEvent(playerKey, slot, Key, valuable));
		RefreshLocalSlot(playerKey, slot);
	}

	private void Hide()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			foreach (PhysGrabber grabber in physGrabObject.playerGrabbing.ToList())
			{
				grabber.OverrideGrabRelease(photonView.ViewID);
			}

			// Only the master owns the haul list. Vanilla would also drop it once the colliders are off.
			if (!valuable.committedToExtraction)
			{
				valuable.RemoveFromDollarHaulList();
			}
		}

		originalScale = transform.localScale;
		colliders = GetComponentsInChildren<Collider>();
		colliderStates = colliders.Select(collider => collider.enabled).ToArray();
		foreach (Collider collider in colliders)
		{
			collider.enabled = false;
		}

		transform.localScale = originalScale * HiddenScaleFactor;

		if (body != null)
		{
			bodyWasKinematic = body.isKinematic;
			body.isKinematic = true;
		}

		physGrabObject.OverrideDeactivate();
	}

	private static void RefreshLocalSlot(int playerKey, int slot)
	{
		if (playerKey != PlayerKeys.Local() || Inventory.instance == null)
		{
			return;
		}

		InventorySpot? spot = Inventory.instance.GetSpotByIndex(slot);
		if (spot != null)
		{
			spot.UpdateUI();
		}
	}
}
