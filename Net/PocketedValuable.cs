using System.Collections;
using System.Linq;
using Photon.Pun;
using Pockets.Api;
using Pockets.Ui;
using UnityEngine;

namespace Pockets.Net;

/// <summary>
/// Runtime component added to every valuable on every client. It carries the store and release RPCs and
/// hides the valuable the way <c>ItemEquippable</c> hides an equipped item (shrunk, colliders off, grab
/// released), since the vanilla inventory only accepts items.
/// Store: the owner asks the master (<see cref="RequestStoreRpc"/>), the master validates with
/// <see cref="PocketSync.CanStore"/> and broadcasts <see cref="StoredRpc"/>, every client applies it.
/// Release: the owner picks a spot in front of the camera and asks the master (<see cref="RequestReleaseRpc"/>),
/// which broadcasts <see cref="ReleasedRpc"/>.
/// </summary>
internal sealed class PocketedValuable : MonoBehaviourPun
{
	private const float HiddenScaleFactor = 0.01f;
	private const float ReleaseDistance = 1.5f;
	private const float MaxReleaseDistanceFromPlayer = 10f;
	private const float AutoGrabSeconds = 1.5f;

	private PhysGrabObject physGrabObject = null!;
	private ValuableObject valuable = null!;
	private Rigidbody? body;
	private Collider[] colliders = new Collider[0];
	private bool[] colliderStates = new bool[0];
	private Vector3 originalScale = Vector3.one;
	private bool bodyWasKinematic;
	private float releaseOffset;

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
		if (!IsPocketed)
		{
			return;
		}

		// The valuable was destroyed while pocketed (worn out, broken by another mod). Free the slot, and tell everybody
		// who needs to know: the slot must stop drawing the old picture and listeners must see the valuable leave.
		int playerKey = PlayerKey;
		int slot = Slot;
		int valuableKey = Key;
		PocketsApi.Forget(playerKey, slot);
		try
		{
			PocketsApi.RaiseRemoved(new PocketEvent(playerKey, slot, valuableKey, null));
			RefreshLocalSlot(playerKey, slot);
		}
		catch (System.Exception exception)
		{
			// Destruction also happens while a scene unloads, when the inventory may already be gone.
			PocketsPlugin.Logger.LogWarning($"Could not refresh slot {slot} after {name} was destroyed: {exception.Message}");
		}
	}

	// ---- Store ----

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

		// The slot picture is only needed on the owner's machine, and must be taken while the valuable is still full size.
		if (playerKey == PlayerKeys.Local())
		{
			ValuableIconRenderer.Ensure(gameObject);
		}

		Hide();

		PocketsPlugin.Logger.LogInfo($"Stored {name} (value {valuable.dollarValueCurrent:F0}) in slot {slot} of player {playerKey}");
		PocketsApi.RaiseStored(new PocketEvent(playerKey, slot, Key, valuable));
		RefreshLocalSlot(playerKey, slot);
	}

	/// <summary>Master only: tells a player who just joined that this valuable is in a pocket, so they hide it too.</summary>
	internal void SendStateTo(Photon.Realtime.Player target, int playerKey, int slot)
	{
		if (SemiFunc.IsMultiplayer() && IsPocketed)
		{
			photonView.RPC(nameof(StoredRpc), target, playerKey, slot);
		}
	}

	// ---- Release ----

	/// <summary>Called by the local player's slot key. Picks a spot in front of the camera and asks the master to release this valuable.</summary>
	/// <param name="autoGrab">Put the valuable in the player's hands after taking it out (the normal key press).</param>
	/// <param name="spread">Sideways offset in steps, so several valuables released together do not pile up.</param>
	internal void RequestRelease(bool autoGrab = true, int spread = 0)
	{
		PlayerAvatar? avatar = PlayerAvatar.instance;
		if (avatar == null || !IsPocketed || PlayerKey != PlayerKeys.Of(avatar))
		{
			return;
		}

		ComputeReleasePose(spread, out Vector3 position, out Quaternion rotation);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC(nameof(RequestReleaseRpc), RpcTarget.MasterClient, Slot, PlayerKey, position, rotation, autoGrab);
		}
		else
		{
			ProcessReleaseRequest(Slot, PlayerKey, position, rotation, autoGrab);
		}
	}

	[PunRPC]
	private void RequestReleaseRpc(int slot, int playerKey, Vector3 position, Quaternion rotation, bool autoGrab, PhotonMessageInfo info = default)
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

		ProcessReleaseRequest(slot, playerKey, position, rotation, autoGrab);
	}

	private void ProcessReleaseRequest(int slot, int playerKey, Vector3 position, Quaternion rotation, bool autoGrab)
	{
		PlayerAvatar? avatar = PlayerKeys.Resolve(playerKey);
		if (!PocketSync.CanRelease(this, slot, playerKey, position, avatar, MaxReleaseDistanceFromPlayer, out string reason))
		{
			PocketsPlugin.Logger.LogInfo($"Release refused for {name}: {reason}");
			return;
		}

		BroadcastRelease(slot, playerKey, position, rotation, autoGrab);
	}

	/// <summary>Master only: releases this valuable at the given pose on every client.</summary>
	internal void BroadcastRelease(int slot, int playerKey, Vector3 position, Quaternion rotation, bool autoGrab)
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC(nameof(ReleasedRpc), RpcTarget.All, playerKey, slot, position, rotation, autoGrab);
		}
		else
		{
			ApplyReleased(playerKey, slot, position, rotation, autoGrab);
		}
	}

	[PunRPC]
	private void ReleasedRpc(int playerKey, int slot, Vector3 position, Quaternion rotation, bool autoGrab, PhotonMessageInfo info = default)
	{
		if (!SemiFunc.MasterOnlyRPC(info))
		{
			return;
		}

		ApplyReleased(playerKey, slot, position, rotation, autoGrab);
	}

	private void ApplyReleased(int playerKey, int slot, Vector3 position, Quaternion rotation, bool autoGrab)
	{
		PlayerPockets? pockets = PocketsApi.Find(playerKey);
		if (!IsPocketed || PlayerKey != playerKey || Slot != slot || pockets == null || !pockets.State.TryRelease(slot, out _))
		{
			PocketsPlugin.Logger.LogWarning($"Could not release {name} from slot {slot} of player {playerKey}; ignoring the release.");
			return;
		}

		int valuableKey = Key;
		pockets.Valuables[slot] = null;
		IsPocketed = false;
		PlayerKey = PlayerKeys.NoPlayer;
		Slot = -1;

		Show();

		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.Teleport(position, rotation);
		}

		PocketsPlugin.Logger.LogInfo($"Released {name} (value {valuable.dollarValueCurrent:F0}) from slot {slot} of player {playerKey}");
		PocketsApi.RaiseRemoved(new PocketEvent(playerKey, slot, valuableKey, valuable));
		RefreshLocalSlot(playerKey, slot);

		PlayerAvatar? localAvatar = PlayerAvatar.instance;
		if (autoGrab && playerKey == PlayerKeys.Local() && localAvatar != null && !localAvatar.deadSet)
		{
			StartCoroutine(AutoGrab());
		}
	}

	/// <summary>A spot in front of the camera that stays out of walls, like <c>ItemEquippable.RayHitTestNew</c>.</summary>
	private void ComputeReleasePose(int spread, out Vector3 position, out Quaternion rotation)
	{
		Camera? camera = Camera.main;
		if (camera == null)
		{
			position = transform.position;
			rotation = transform.rotation;
			return;
		}

		Transform view = camera.transform;
		rotation = Quaternion.LookRotation(view.forward, Vector3.up);
		int mask = (int)SemiFunc.LayerMaskGetVisionObstruct() & ~LayerMask.GetMask("Ignore Raycast", "CollisionCheck");
		position = Physics.Raycast(view.position, view.forward, out RaycastHit hit, ReleaseDistance, mask)
			? hit.point + hit.normal * releaseOffset
			: view.position + view.forward * ReleaseDistance;
		position += view.right * (spread * 0.25f);
	}

	/// <summary>Keeps grabbing the valuable for a moment after taking it out, like <c>ItemEquippable.ForceGrab</c>, so it ends up in the player's hands.</summary>
	private IEnumerator AutoGrab()
	{
		float timer = AutoGrabSeconds;
		while (timer > 0f && physGrabObject != null && PhysGrabber.instance != null)
		{
			PhysGrabber.instance.OverrideGrab(physGrabObject);
			if (SemiFunc.InputUp(InputKey.Grab))
			{
				break;
			}

			timer -= Time.deltaTime;
			yield return null;
		}
	}

	// ---- Hide and show ----

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
		Bounds bounds = new Bounds(transform.position, Vector3.zero);
		foreach (Collider collider in colliders)
		{
			bounds.Encapsulate(collider.bounds);
			collider.enabled = false;
		}

		releaseOffset = bounds.extents.magnitude;
		transform.localScale = originalScale * HiddenScaleFactor;

		if (body != null)
		{
			bodyWasKinematic = body.isKinematic;
			body.isKinematic = true;
		}

		physGrabObject.OverrideDeactivate();
	}

	private void Show()
	{
		transform.localScale = originalScale;
		for (int index = 0; index < colliders.Length; index++)
		{
			if (colliders[index] != null && index < colliderStates.Length)
			{
				colliders[index].enabled = colliderStates[index];
			}
		}

		if (body != null)
		{
			body.isKinematic = bodyWasKinematic;
		}

		physGrabObject.OverrideDeactivateReset();
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
