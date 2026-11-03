using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace Pockets.Spikes;

/// <summary>
/// Throwaway prototype for spikes S1, S3, S4 and S5 (see specs/001-pockets-storage/plan.md).
/// Hides a valuable the way <c>ItemEquippable</c> hides an equipped item and restores it later,
/// over a runtime-added PUN RPC component. Removed once the outcomes are recorded in research.md.
/// </summary>
internal sealed class SpikePocketed : MonoBehaviourPun
{
	private const float HiddenScaleFactor = 0.01f;

	internal static readonly HashSet<GameObject> Hidden = new HashSet<GameObject>();

	private PhysGrabObject? physGrabObject;
	private Rigidbody? body;
	private Collider[] colliders = new Collider[0];
	private bool[] colliderStates = new bool[0];
	private Vector3 originalScale = Vector3.one;
	private bool bodyWasKinematic;

	internal static SpikePocketed? LastHidden { get; private set; }

	internal bool IsHidden { get; private set; }

	private void Awake()
	{
		physGrabObject = GetComponent<PhysGrabObject>();
		body = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		// Vanilla re-asserts the deactivation every frame while an item is equipped.
		if (IsHidden && physGrabObject != null)
		{
			physGrabObject.OverrideDeactivate();
		}
	}

	private void OnDestroy()
	{
		Hidden.Remove(gameObject);
	}

	internal void RequestHide()
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC(nameof(SpikeHideRpc), RpcTarget.AllBuffered);
		}
		else
		{
			SpikeHideRpc();
		}
	}

	/// <summary>Distance from the center to the farthest bound, measured before hiding; used to keep the shown object out of walls.</summary>
	internal float ShowOffset { get; private set; }

	internal void RequestShow(Vector3 position, Quaternion rotation, int grabberViewId)
	{
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC(nameof(SpikeShowRpc), RpcTarget.AllBuffered, position, rotation, grabberViewId);
		}
		else
		{
			SpikeShowRpc(position, rotation, grabberViewId);
		}
	}

	private IEnumerator ForceGrab()
	{
		// Same idea as ItemEquippable.ForceGrab: keep grabbing the object for a moment after taking it out.
		float timer = 1.5f;
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

	[PunRPC]
	private void SpikeHideRpc(PhotonMessageInfo info = default)
	{
		PocketsPlugin.Logger.LogInfo($"[Spike S3] hide RPC received on {Role()} for view {photonView.ViewID} from {info.Sender?.NickName ?? "local"}");
		if (IsHidden || physGrabObject == null)
		{
			return;
		}

		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			foreach (PhysGrabber grabber in physGrabObject.playerGrabbing.ToList())
			{
				grabber.OverrideGrabRelease(photonView.ViewID);
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

		ShowOffset = bounds.extents.magnitude;

		// Keep a hidden valuable out of the haul list (plan: remove on store). Only the master owns the list.
		ValuableObject? valuable = GetComponent<ValuableObject>();
		if (valuable != null && SemiFunc.IsMasterClientOrSingleplayer() && !valuable.committedToExtraction)
		{
			valuable.RemoveFromDollarHaulList();
		}

		transform.localScale = originalScale * HiddenScaleFactor;

		if (body != null)
		{
			bodyWasKinematic = body.isKinematic;
			body.isKinematic = true;
		}

		IsHidden = true;
		Hidden.Add(gameObject);
		LastHidden = this;
		physGrabObject.OverrideDeactivate();
	}

	[PunRPC]
	private void SpikeShowRpc(Vector3 position, Quaternion rotation, int grabberViewId, PhotonMessageInfo info = default)
	{
		PocketsPlugin.Logger.LogInfo($"[Spike S3] show RPC received on {Role()} for view {photonView.ViewID} from {info.Sender?.NickName ?? "local"}");
		if (!IsHidden || physGrabObject == null)
		{
			return;
		}

		transform.localScale = originalScale;
		for (int index = 0; index < colliders.Length; index++)
		{
			if (colliders[index] != null)
			{
				colliders[index].enabled = colliderStates[index];
			}
		}

		if (body != null)
		{
			body.isKinematic = bodyWasKinematic;
		}

		IsHidden = false;
		Hidden.Remove(gameObject);
		physGrabObject.OverrideDeactivateReset();

		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.Teleport(position, rotation);

			// ValuableObject.Update only adds to the haul list when it enters an extraction room; forget the old flag
			// so a valuable shown inside the room is added again.
			ValuableObject? valuable = GetComponent<ValuableObject>();
			if (valuable != null)
			{
				valuable.inStartRoom = false;
			}
		}

		bool isGrabber = PhysGrabber.instance != null
			&& (!SemiFunc.IsMultiplayer() || PhysGrabber.instance.photonView.ViewID == grabberViewId);
		if (isGrabber)
		{
			StartCoroutine(ForceGrab());
		}
	}

	internal string DumpState()
	{
		ValuableObject? valuable = GetComponent<ValuableObject>();
		bool inHaul = RoundDirector.instance != null && RoundDirector.instance.dollarHaulList.Contains(gameObject);
		bool inExtractionRoom = false;
		if (physGrabObject != null && physGrabObject.roomVolumeCheck != null)
		{
			foreach (RoomVolume room in physGrabObject.roomVolumeCheck.CurrentRooms)
			{
				inExtractionRoom |= room.Extraction;
			}
		}

		int enabledColliders = GetComponentsInChildren<Collider>().Count(collider => collider.enabled);
		return $"[Spike dump] {Role()} view={photonView.ViewID} name={name} hidden={IsHidden} scale={transform.localScale.x:F3} "
			+ $"colliders={enabledColliders}/{colliders.Length} kinematic={(body != null ? body.isKinematic.ToString() : "n/a")} "
			+ $"clientNonKinematic={(physGrabObject != null ? physGrabObject.clientNonKinematic.ToString() : "n/a")} "
			+ $"volume={(valuable != null ? valuable.volumeType.ToString() : "n/a")} value={(valuable != null ? valuable.dollarValueCurrent.ToString("F0") : "n/a")} "
			+ $"inHaulList={inHaul} inExtractionRoom={inExtractionRoom} grabbers={(physGrabObject != null ? physGrabObject.playerGrabbing.Count : 0)}";
	}

	private static string Role()
	{
		return SemiFunc.IsMasterClientOrSingleplayer() ? "host" : "client";
	}
}
