using Pockets.Domain;

namespace Pockets.Api;

/// <summary>A player's pocket state together with the valuable objects behind the keys.</summary>
internal sealed class PlayerPockets
{
	internal PocketState State { get; } = new PocketState();

	internal ValuableObject?[] Valuables { get; } = new ValuableObject?[PocketState.SlotCount];

	internal void Clear()
	{
		State.Clear();
		for (int slot = 0; slot < Valuables.Length; slot++)
		{
			Valuables[slot] = null;
		}
	}
}
