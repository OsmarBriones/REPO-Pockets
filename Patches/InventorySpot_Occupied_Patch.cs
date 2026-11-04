using HarmonyLib;
using Pockets.Api;
using Pockets.Net;

namespace Pockets.Patches;

/// <summary>Makes a pocketed slot read as occupied, so vanilla never equips an item into it.</summary>
[HarmonyPatch(typeof(InventorySpot), nameof(InventorySpot.IsOccupied))]
internal static class InventorySpot_Occupied_Patch
{
	[HarmonyPostfix]
	private static void Postfix(InventorySpot __instance, ref bool __result)
	{
		// Not gated by Enabled: a valuable that is still in a pocket keeps its slot until it is taken out.
		if (__result || !SemiFunc.RunIsLevel())
		{
			return;
		}

		int playerKey = PlayerKeys.Local();
		if (playerKey == PlayerKeys.NoPlayer)
		{
			return;
		}

		PlayerPockets? pockets = PocketsApi.Find(playerKey);
		if (pockets != null && pockets.State.IsOccupied(__instance.inventorySpotIndex))
		{
			__result = true;
		}
	}
}
