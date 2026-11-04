using Pockets.Api;

namespace Pockets.Net;

/// <summary>Takes the local player's valuables out of their pockets, for example when the mod is switched off.</summary>
internal static class PocketReleaser
{
	/// <summary>Asks for every valuable in the local player's pockets to be dropped in front of them (not grabbed), side by side.</summary>
	internal static void ReleaseLocalPockets()
	{
		int playerKey = PlayerKeys.Local();
		PlayerPockets? pockets = playerKey == PlayerKeys.NoPlayer ? null : PocketsApi.Find(playerKey);
		if (pockets == null)
		{
			return;
		}

		int released = 0;
		for (int slot = 0; slot < pockets.Valuables.Length; slot++)
		{
			ValuableObject? valuable = pockets.Valuables[slot];
			PocketedValuable? pocketed = valuable != null ? valuable.GetComponent<PocketedValuable>() : null;
			if (pocketed != null)
			{
				pocketed.RequestRelease(autoGrab: false, spread: slot - 1);
				released++;
			}
		}

		if (released > 0)
		{
			PocketsPlugin.Logger.LogInfo($"Pockets switched off: dropping {released} stored valuable(s)");
		}
	}
}
