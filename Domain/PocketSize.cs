namespace Pockets.Domain;

/// <summary>
/// Valuable sizes in the same order as the game's <c>ValuableVolume.Type</c>, so the pure domain
/// stays free of game types. A size limit accepts every size up to and including the limit.
/// </summary>
public enum PocketSize
{
	Tiny = 0,
	Small = 1,
	Medium = 2,
	Big = 3,
	Wide = 4,
	Tall = 5,
	VeryTall = 6,
}
