using HarmonyLib;

namespace Pockets.Patches;

/// <summary>
/// Reloads configuration and resets per-level state when a playable level loads.
/// 
/// LIFECYCLE GUIDANCE:
/// - EnemyDirector.Start runs at Frame 0 of scene load, BEFORE procedural generation completes.
///   Appropriate for: reloading config, resetting in-memory per-level counters.
///   Do NOT use for: spawning items, checking dungeon geometry, or querying TruckSafetySpawnPoint.
/// - If your mod interacts with the generated level or truck, hook RoundDirector.StartRoundLogic
///   or SemiFunc.OnLevelGenDone instead (executes after LevelGenerator.Instance.Generated is true).
/// </summary>
[HarmonyPatch(typeof(EnemyDirector), "Start")]
internal static class EnemyDirector_Start_Patch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (!SemiFunc.RunIsLevel())
			return;

		ConfigurationController.Reload();
	}
}
