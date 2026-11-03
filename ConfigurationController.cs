using BepInEx.Configuration;

namespace Pockets
{
	internal class ConfigurationController
	{
		private static ConfigFile? ConfigFile { get; set; }
		private static ConfigEntry<bool>? Enabled { get; set; }
		private static ConfigEntry<bool>? EnableSpikes { get; set; }

		internal static bool IsEnabled => Enabled?.Value ?? true;

		/// <summary>Opt-in prototype hotkeys for the technical spikes in plan.md. Disabled by default.</summary>
		internal static bool SpikesEnabled => EnableSpikes?.Value ?? false;

		internal static void Initialize(ConfigFile config)
		{
			ConfigFile = config;

			Enabled = ConfigFile.Bind("General", nameof(Enabled), true, "Enable or disable this mod.");
			EnableSpikes = ConfigFile.Bind("Debug", nameof(EnableSpikes), false,
				"Developer only. Enables prototype hotkeys (F6 hide or show the held valuable, F7 dump its state, F8 sync test, F9 host property test).");

			ConfigFile.Save();
		}

		internal static void Reload()
		{
			ConfigFile?.Reload();
			ConfigFile?.Save();
		}
	}
}
