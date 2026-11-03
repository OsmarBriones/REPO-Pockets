using HarmonyLib;
using Pockets.Api;
using Pockets.Net;
using UnityEngine;

namespace Pockets.Patches;

/// <summary>
/// Intercepts the slot key. Holding an eligible valuable and pressing a free slot stores it; pressing a
/// pocketed slot takes its valuable out and never reaches vanilla (whose unequip path would fail on a
/// slot without an item).
/// </summary>
[HarmonyPatch(typeof(InventorySpot), "HandleInput")]
internal static class InventorySpot_HandleInput_Patch
{
	private const float RequestCooldownSeconds = 0.2f;

	private static float lastRequestTime = -1f;

	[HarmonyPrefix]
	private static bool Prefix(InventorySpot __instance)
	{
		if (!ConfigurationController.IsEnabled || !SemiFunc.RunIsLevel())
		{
			return true;
		}

		int playerKey = PlayerKeys.Local();
		if (playerKey == PlayerKeys.NoPlayer)
		{
			return true;
		}

		int spot = __instance.inventorySpotIndex;
		PlayerPockets? pockets = PocketsApi.Find(playerKey);
		if (pockets != null && pockets.State.IsOccupied(spot))
		{
			// A pocketed slot never reaches vanilla; the key takes the valuable out.
			if (CanSendRequest())
			{
				ValuableObject? stored = pockets.Valuables[spot];
				PocketedValuable? pocketed = stored != null ? stored.GetComponent<PocketedValuable>() : null;
				if (pocketed != null)
				{
					pocketed.RequestRelease();
				}
			}

			return false;
		}

		if (__instance.IsOccupied())
		{
			return true;
		}

		PocketedValuable? held = HeldValuable();
		if (held == null || !PocketSync.IsSizeAccepted(held.Valuable))
		{
			return true;
		}

		if (CanSendRequest())
		{
			held.RequestStore(spot);
		}

		return false;
	}

	/// <summary>Rate-limits slot-key requests; also consumes the attempt when it passes.</summary>
	private static bool CanSendRequest()
	{
		if (PlayerController.instance.InputDisableTimer > 0f || Time.time - lastRequestTime < RequestCooldownSeconds)
		{
			return false;
		}

		lastRequestTime = Time.time;
		return true;
	}

	private static PocketedValuable? HeldValuable()
	{
		PhysGrabber? grabber = PhysGrabber.instance;
		if (grabber == null || !grabber.grabbed || grabber.grabbedPhysGrabObject == null)
		{
			return null;
		}

		return grabber.grabbedPhysGrabObject.GetComponent<PocketedValuable>();
	}
}
