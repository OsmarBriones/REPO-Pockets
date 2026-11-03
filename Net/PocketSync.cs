using Pockets.Api;
using Pockets.Domain;
using UnityEngine;

namespace Pockets.Net;

/// <summary>Host-side rules for storing a valuable. Used by the master client (or singleplayer) before it broadcasts the result.</summary>
internal static class PocketSync
{
	/// <summary>Whether the valuable's size is allowed by the configuration.</summary>
	internal static bool IsSizeAccepted(ValuableObject valuable)
	{
		return SizePolicy.TryFromGameVolume((int)valuable.volumeType, out PocketSize size)
			&& SizePolicy.Accepts(size, ConfigurationController.MaxValuableSize);
	}

	/// <summary>Validates a release request from a player: it must be theirs, and the drop spot must be near them.</summary>
	internal static bool CanRelease(PocketedValuable pocketed, int slot, int playerKey, Vector3 position, PlayerAvatar? avatar, float maxDistanceFromPlayer, out string reason)
	{
		if (avatar == null)
		{
			reason = "unknown player";
			return false;
		}

		if (avatar.isDisabled || avatar.deadSet)
		{
			reason = "the player is dead or disabled";
			return false;
		}

		if (!pocketed.IsPocketed || pocketed.PlayerKey != playerKey || pocketed.Slot != slot)
		{
			reason = "the valuable is not in that player's slot";
			return false;
		}

		bool finite = !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z)
			&& !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z);
		if (!finite || Vector3.Distance(position, avatar.transform.position) > maxDistanceFromPlayer)
		{
			reason = "the drop position is invalid or too far from the player";
			return false;
		}

		reason = string.Empty;
		return true;
	}

	/// <summary>Validates a store request. <paramref name="reason"/> explains a refusal, for logging.</summary>
	internal static bool CanStore(PocketedValuable pocketed, int slot, int playerKey, out string reason)
	{
		ValuableObject valuable = pocketed.Valuable;

		if (!ConfigurationController.IsEnabled)
		{
			reason = "the mod is disabled";
			return false;
		}

		if (!SemiFunc.RunIsLevel())
		{
			reason = "not inside a level";
			return false;
		}

		PlayerAvatar? avatar = PlayerKeys.Resolve(playerKey);
		if (avatar == null)
		{
			reason = "unknown player";
			return false;
		}

		if (avatar.isDisabled || avatar.deadSet)
		{
			reason = "the player is dead or disabled";
			return false;
		}

		if (!PocketState.IsValidSlot(slot))
		{
			reason = $"invalid slot {slot}";
			return false;
		}

		if (pocketed.IsPocketed)
		{
			reason = "the valuable is already pocketed";
			return false;
		}

		if (valuable.committedToExtraction)
		{
			reason = "the valuable is committed to an extraction";
			return false;
		}

		if (!IsSizeAccepted(valuable))
		{
			reason = $"size {valuable.volumeType} is over the limit {ConfigurationController.MaxValuableSize}";
			return false;
		}

		PlayerPockets? pockets = PocketsApi.Find(playerKey);
		if (pockets != null && pockets.State.IsOccupied(slot))
		{
			reason = $"slot {slot} is already occupied";
			return false;
		}

		PhysGrabObject physGrabObject = pocketed.PhysGrabObject;
		if (avatar.physGrabber == null || !physGrabObject.playerGrabbing.Contains(avatar.physGrabber) || physGrabObject.playerGrabbing.Count != 1)
		{
			reason = "the valuable is not held only by the requesting player";
			return false;
		}

		reason = string.Empty;
		return true;
	}
}
