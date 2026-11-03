namespace Pockets.Api;

/// <summary>
/// Raised before Pockets drops a dying player's valuables. A subscriber that takes care of them
/// itself (for example by moving them elsewhere) sets <see cref="Cancel"/> so Pockets leaves them alone.
/// </summary>
public sealed class PlayerDyingEventArgs
{
	public PlayerDyingEventArgs(int playerKey)
	{
		PlayerKey = playerKey;
	}

	public int PlayerKey { get; }

	/// <summary>Set to true to stop Pockets from dropping the valuables.</summary>
	public bool Cancel { get; set; }
}
