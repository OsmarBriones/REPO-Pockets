using System;
using System.Collections.Generic;
using Pockets.Domain;
using Pockets.Net;
using UnityEngine;

namespace Pockets.Api;

/// <summary>
/// Public surface of Pockets for other mods (TalismanCore). Read the pockets of any player and
/// react to valuables being stored or removed. All members are safe to call from the main thread only.
/// </summary>
public static class PocketsApi
{
	private static readonly Dictionary<int, PlayerPockets> Players = new Dictionary<int, PlayerPockets>();

	/// <summary>Raised after a valuable has been stored in a pocket.</summary>
	public static event Action<PocketEvent>? Stored;

	/// <summary>Raised after a valuable has left a pocket (taken out, dropped, or lost).</summary>
	public static event Action<PocketEvent>? Removed;

	/// <summary>Raised before a dying player's valuables are dropped; set <see cref="PlayerDyingEventArgs.Cancel"/> to handle them yourself.</summary>
	public static event Action<PlayerDyingEventArgs>? PlayerDying;

	/// <summary>The valuables currently stored by a player, one entry per slot (null for a free slot or a valuable not loaded here).</summary>
	public static IReadOnlyList<ValuableObject?> GetStored(int playerKey)
	{
		return Players.TryGetValue(playerKey, out PlayerPockets? pockets)
			? (IReadOnlyList<ValuableObject?>)pockets.Valuables.Clone()
			: new ValuableObject?[PocketState.SlotCount];
	}

	/// <summary>True when the valuable is stored in any player's pocket.</summary>
	public static bool IsPocketed(ValuableObject valuable)
	{
		foreach (PlayerPockets pockets in Players.Values)
		{
			foreach (ValuableObject? stored in pockets.Valuables)
			{
				if (stored == valuable)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Takes a valuable out of a player's pocket and drops it at <paramref name="at"/> (default: just above the player).
	/// Call it on the host only; it returns false on clients, or when the slot is empty or the valuable is not loaded.
	/// </summary>
	public static bool TryRemove(int playerKey, int slot, Vector3? at = null)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return false;
		}

		PlayerPockets? pockets = Find(playerKey);
		if (pockets == null || !PocketState.IsValidSlot(slot) || !pockets.State.IsOccupied(slot) || pockets.Valuables[slot] == null)
		{
			return false;
		}

		PocketedValuable? pocketed = pockets.Valuables[slot]!.GetComponent<PocketedValuable>();
		if (pocketed == null)
		{
			return false;
		}

		Vector3 position = at ?? DefaultDropPosition(playerKey, pocketed);
		pocketed.BroadcastRelease(slot, playerKey, position, Quaternion.identity);
		return true;
	}

	private static Vector3 DefaultDropPosition(int playerKey, PocketedValuable pocketed)
	{
		PlayerAvatar? avatar = PlayerKeys.Resolve(playerKey);
		return avatar != null ? avatar.transform.position + Vector3.up * 0.5f : pocketed.transform.position;
	}

	internal static PlayerPockets GetOrCreate(int playerKey)
	{
		if (!Players.TryGetValue(playerKey, out PlayerPockets? pockets))
		{
			pockets = new PlayerPockets();
			Players[playerKey] = pockets;
		}

		return pockets;
	}

	/// <summary>The pockets of a player, or null when nothing has been registered for them.</summary>
	internal static PlayerPockets? Find(int playerKey)
	{
		return Players.TryGetValue(playerKey, out PlayerPockets? pockets) ? pockets : null;
	}

	/// <summary>Drops a registration silently (no events), for example when the valuable object is destroyed.</summary>
	internal static void Forget(int playerKey, int slot)
	{
		PlayerPockets? pockets = Find(playerKey);
		if (pockets != null && pockets.State.TryRelease(slot, out _))
		{
			pockets.Valuables[slot] = null;
		}
	}

	internal static void ClearAll()
	{
		foreach (PlayerPockets pockets in Players.Values)
		{
			pockets.Clear();
		}

		Players.Clear();
	}

	internal static void RaiseStored(PocketEvent pocketEvent)
	{
		Raise(Stored, pocketEvent, nameof(Stored));
	}

	internal static void RaiseRemoved(PocketEvent pocketEvent)
	{
		Raise(Removed, pocketEvent, nameof(Removed));
	}

	/// <summary>Raises <see cref="PlayerDying"/> and returns true when a subscriber cancelled the default drop.</summary>
	internal static bool RaisePlayerDying(int playerKey)
	{
		PlayerDyingEventArgs args = new PlayerDyingEventArgs(playerKey);
		Raise(PlayerDying, args, nameof(PlayerDying));
		return args.Cancel;
	}

	private static void Raise<T>(Action<T>? handlers, T argument, string eventName)
	{
		if (handlers == null)
		{
			return;
		}

		foreach (Delegate handler in handlers.GetInvocationList())
		{
			try
			{
				((Action<T>)handler)(argument);
			}
			catch (Exception exception)
			{
				// One faulty subscriber must not break the others or the game.
				PocketsPlugin.Logger.LogError($"A subscriber of PocketsApi.{eventName} threw: {exception}");
			}
		}
	}
}
