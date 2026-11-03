namespace Pockets.Api;

/// <summary>Describes a valuable that was stored in, or removed from, a player's pocket.</summary>
public readonly struct PocketEvent
{
	public PocketEvent(int playerKey, int slot, int valuableKey, ValuableObject? valuable)
	{
		PlayerKey = playerKey;
		Slot = slot;
		ValuableKey = valuableKey;
		Valuable = valuable;
	}

	/// <summary>Identifies the player: the PhotonView id of their avatar in multiplayer, the local key in singleplayer.</summary>
	public int PlayerKey { get; }

	/// <summary>The inventory slot index, 0 to 2.</summary>
	public int Slot { get; }

	/// <summary>Opaque key of the valuable (view id in multiplayer, instance id in singleplayer).</summary>
	public int ValuableKey { get; }

	/// <summary>The valuable, when it is loaded on this machine.</summary>
	public ValuableObject? Valuable { get; }
}
