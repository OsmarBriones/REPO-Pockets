using HarmonyLib;
using Pockets.Api;
using Pockets.Net;

namespace Pockets.Patches;

/// <summary>
/// When a player dies (accepted by vanilla), the master gives subscribers a chance to take over the stored valuables
/// (the Phone talisman moves them to a cart), and otherwise drops them where the player fell if <c>DropOnDeath</c> is on.
/// </summary>
[HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeathRPC))]
internal static class PlayerAvatar_PlayerDeathRPC_Patch
{
	[HarmonyPrefix]
	private static void Prefix(PlayerAvatar __instance, out bool __state)
	{
		__state = __instance.deadSet;
	}

	[HarmonyPostfix]
	private static void Postfix(PlayerAvatar __instance, bool __state)
	{
		// Only the first death, and only once vanilla accepted it (it sets deadSet).
		if (__state || !__instance.deadSet || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}

		int playerKey = PlayerKeys.Of(__instance);
		PlayerPockets? pockets = PocketsApi.Find(playerKey);
		if (pockets == null || pockets.State.OccupiedCount == 0)
		{
			return;
		}

		if (PocketsApi.RaisePlayerDying(playerKey) || !ConfigurationController.DropOnDeath)
		{
			return;
		}

		for (int slot = 0; slot < pockets.Valuables.Length; slot++)
		{
			PocketsApi.TryRemove(playerKey, slot);
		}
	}
}
