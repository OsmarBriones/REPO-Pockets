using HarmonyLib;
using Photon.Pun;

namespace Pockets.Spikes;

/// <summary>Adds the spike component to every valuable on every client (spike S3).</summary>
[HarmonyPatch(typeof(ValuableObject), "Start")]
internal static class Spike_ValuableObject_Start_Patch
{
	[HarmonyPostfix]
	private static void Postfix(ValuableObject __instance)
	{
		if (!ConfigurationController.SpikesEnabled || __instance.GetComponent<SpikePocketed>() != null)
		{
			return;
		}

		__instance.gameObject.AddComponent<SpikePocketed>();
		PhotonView? view = __instance.GetComponent<PhotonView>();
		if (view != null)
		{
			view.RefreshRpcMonoBehaviourCache();
		}
	}
}

/// <summary>Keeps a hidden valuable out of the extraction haul list (spike S5).</summary>
[HarmonyPatch(typeof(ValuableObject), "AddToDollarHaulList")]
internal static class Spike_ValuableObject_AddToDollarHaulList_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(ValuableObject __instance)
	{
		if (ConfigurationController.SpikesEnabled && SpikePocketed.Hidden.Contains(__instance.gameObject))
		{
			PocketsPlugin.Logger.LogInfo($"[Spike S5] blocked haul list add for hidden valuable {__instance.name}");
			return false;
		}

		return true;
	}
}
