using HarmonyLib;
using Pockets.Api;
using Pockets.Net;

namespace Pockets.Patches;

/// <summary>
/// Reloads the configuration and clears every pocket when a playable level loads, so nothing carries over
/// between levels. EnemyDirector.Start runs at the start of the scene load, before generation completes.
/// </summary>
[HarmonyPatch(typeof(EnemyDirector), "Start")]
internal static class EnemyDirector_Start_Patch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (!SemiFunc.RunIsLevel())
		{
			return;
		}

		ConfigurationController.Reload();
		PocketsApi.ClearAll();
		HostSettings.Publish();
	}
}
