using HarmonyLib;
using Pockets.Net;

namespace Pockets.Patches;

/// <summary>Keeps a pocketed valuable out of the extraction haul list, even if its room check still reports an extraction room.</summary>
[HarmonyPatch(typeof(ValuableObject), "AddToDollarHaulList")]
internal static class ValuableObject_AddToDollarHaulList_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(ValuableObject __instance)
	{
		PocketedValuable? pocketed = __instance.GetComponent<PocketedValuable>();
		return pocketed == null || !pocketed.IsPocketed;
	}
}
