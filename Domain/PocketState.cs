namespace Pockets.Domain;

/// <summary>
/// The three pocket slots of one player. Pure logic with no game types.
/// A valuable is identified by an opaque integer key chosen by the integration layer
/// (the PhotonView id in multiplayer; the Unity instance id in singleplayer, where every
/// valuable's view id is 0).
/// </summary>
internal sealed class PocketState
{
	internal const int SlotCount = 3;

	/// <summary>Marks an empty slot; never a valid valuable key.</summary>
	internal const int NoValuable = int.MinValue;

	private readonly int[] slots = new int[SlotCount];

	internal PocketState()
	{
		Clear();
	}

	internal int OccupiedCount
	{
		get
		{
			int count = 0;
			foreach (int slot in slots)
			{
				if (slot != NoValuable)
				{
					count++;
				}
			}

			return count;
		}
	}

	internal static bool IsValidSlot(int slot)
	{
		return slot >= 0 && slot < SlotCount;
	}

	internal bool IsOccupied(int slot)
	{
		return IsValidSlot(slot) && slots[slot] != NoValuable;
	}

	/// <summary>The key stored in <paramref name="slot"/>, or <see cref="NoValuable"/>.</summary>
	internal int Get(int slot)
	{
		return IsValidSlot(slot) ? slots[slot] : NoValuable;
	}

	/// <summary>The slot holding <paramref name="valuableKey"/>, or -1 when it is not stored.</summary>
	internal int FindSlot(int valuableKey)
	{
		if (valuableKey == NoValuable)
		{
			return -1;
		}

		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (slots[slot] == valuableKey)
			{
				return slot;
			}
		}

		return -1;
	}

	/// <summary>Stores a valuable in a free slot. Fails for a bad slot, an occupied slot, a missing key, or a valuable already stored.</summary>
	internal bool TryStore(int slot, int valuableKey)
	{
		if (!IsValidSlot(slot) || valuableKey == NoValuable || slots[slot] != NoValuable || FindSlot(valuableKey) != -1)
		{
			return false;
		}

		slots[slot] = valuableKey;
		return true;
	}

	/// <summary>Frees a slot and returns the key it held.</summary>
	internal bool TryRelease(int slot, out int valuableKey)
	{
		if (!IsOccupied(slot))
		{
			valuableKey = NoValuable;
			return false;
		}

		valuableKey = slots[slot];
		slots[slot] = NoValuable;
		return true;
	}

	/// <summary>The index of the first free slot, or -1 when all are occupied.</summary>
	internal int FirstFreeSlot()
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (slots[slot] == NoValuable)
			{
				return slot;
			}
		}

		return -1;
	}

	internal void Clear()
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			slots[slot] = NoValuable;
		}
	}

	/// <summary>A copy of the slots, for syncing or logging.</summary>
	internal int[] Snapshot()
	{
		return (int[])slots.Clone();
	}
}
