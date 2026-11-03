using Pockets.Api;
using Pockets.Domain;

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
