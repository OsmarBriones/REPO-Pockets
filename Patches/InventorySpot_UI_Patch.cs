using HarmonyLib;
using Pockets.Ui;

namespace Pockets.Patches;

/// <summary>After vanilla redraws a slot, draw the pocketed valuable (picture and value) over it, or clear our label.</summary>
[HarmonyPatch(typeof(InventorySpot), nameof(InventorySpot.UpdateUI))]
internal static class InventorySpot_UI_Patch
{
	[HarmonyPostfix]
	private static void Postfix(InventorySpot __instance)
	{
		PocketSlotView.GetOrAdd(__instance).Refresh();
	}
}
