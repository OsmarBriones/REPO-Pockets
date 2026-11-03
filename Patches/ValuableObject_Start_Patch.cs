using HarmonyLib;
using Photon.Pun;
using Pockets.Net;

namespace Pockets.Patches;

/// <summary>Adds the pocket component to every valuable on every client, and refreshes PUN's RPC cache so its RPCs are found.</summary>
[HarmonyPatch(typeof(ValuableObject), "Start")]
internal static class ValuableObject_Start_Patch
{
	[HarmonyPostfix]
	private static void Postfix(ValuableObject __instance)
	{
		if (__instance.GetComponent<PocketedValuable>() != null)
		{
			return;
		}

		__instance.gameObject.AddComponent<PocketedValuable>();
		PhotonView? view = __instance.GetComponent<PhotonView>();
		if (view != null)
		{
			view.RefreshRpcMonoBehaviourCache();
		}
	}
}
