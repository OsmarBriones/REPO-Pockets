namespace Pockets.Net;

/// <summary>Identifies players for the pocket registry, the way vanilla identifies them in its RPCs.</summary>
internal static class PlayerKeys
{
	/// <summary>The only player in singleplayer, matching the id vanilla uses there.</summary>
	internal const int SingleplayerKey = -1;

	/// <summary>No player available (for example before the avatar exists).</summary>
	internal const int NoPlayer = int.MinValue;

	internal static int Of(PlayerAvatar avatar)
	{
		return SemiFunc.IsMultiplayer() ? avatar.photonView.ViewID : SingleplayerKey;
	}

	internal static int Local()
	{
		PlayerAvatar? avatar = PlayerAvatar.instance;
		return avatar == null ? NoPlayer : Of(avatar);
	}

	internal static PlayerAvatar? Resolve(int playerKey)
	{
		// Vanilla returns null for the singleplayer id (-1), so resolve it ourselves.
		return SemiFunc.IsMultiplayer() ? SemiFunc.PlayerAvatarGetFromPhotonID(playerKey) : PlayerAvatar.instance;
	}
}
