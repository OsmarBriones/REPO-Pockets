namespace Pockets.Domain;

/// <summary>Decides which valuables may be stored, given the configured largest size.</summary>
internal static class SizePolicy
{
	/// <summary>True when a valuable of <paramref name="valuable"/> size fits under <paramref name="maximum"/>.</summary>
	internal static bool Accepts(PocketSize valuable, PocketSize maximum)
	{
		return valuable <= maximum;
	}

	/// <summary>Converts the game's volume enum value (by its numeric order) to a <see cref="PocketSize"/>.</summary>
	internal static bool TryFromGameVolume(int gameVolume, out PocketSize size)
	{
		if (gameVolume < (int)PocketSize.Tiny || gameVolume > (int)PocketSize.VeryTall)
		{
			size = PocketSize.Tiny;
			return false;
		}

		size = (PocketSize)gameVolume;
		return true;
	}
}
