using BepInEx.Configuration;
using Pockets.Domain;

namespace Pockets
{
	internal class ConfigurationController
	{
		private static ConfigFile? ConfigFile { get; set; }
		private static ConfigEntry<bool>? Enabled { get; set; }
		private static ConfigEntry<PocketSize>? MaxValuableSizeEntry { get; set; }
		private static ConfigEntry<bool>? DropOnDeathEntry { get; set; }
		private static ConfigEntry<bool>? ShowSlotValueEntry { get; set; }
		private static ConfigEntry<bool>? EnableSpikes { get; set; }
		private static ConfigEntry<bool>? EnableIconBake { get; set; }

		internal static bool IsEnabled => Enabled?.Value ?? true;

		/// <summary>Largest valuable size that can be stored. The host's value is authoritative.</summary>
		internal static PocketSize MaxValuableSize => MaxValuableSizeEntry?.Value ?? PocketSize.Tiny;

		/// <summary>Whether stored valuables drop where the player dies. The host's value is authoritative.</summary>
		internal static bool DropOnDeath => DropOnDeathEntry?.Value ?? true;

		/// <summary>Whether the slot shows the stored valuable's value. Local only.</summary>
		internal static bool ShowSlotValue => ShowSlotValueEntry?.Value ?? true;

		/// <summary>Opt-in prototype hotkeys for the technical spikes in plan.md. Disabled by default.</summary>
		internal static bool SpikesEnabled => EnableSpikes?.Value ?? false;

		/// <summary>Opt-in developer tool: F10 renders every valuable prefab to a PNG in BepInEx/PocketIcons. Disabled by default.</summary>
		internal static bool IconBakeEnabled => EnableIconBake?.Value ?? false;

		internal static void Initialize(ConfigFile config)
		{
			ConfigFile = config;

			Enabled = ConfigFile.Bind("General", nameof(Enabled), true, "Enable or disable this mod.");
			MaxValuableSizeEntry = ConfigFile.Bind("General", "MaxValuableSize", PocketSize.Tiny,
				"Largest valuable that can be stored in a pocket. Every size up to this one is accepted (Tiny, Small, Medium, Big, Wide, Tall, VeryTall). The host's value is used.");
			DropOnDeathEntry = ConfigFile.Bind("General", "DropOnDeath", true,
				"Drop the valuables stored in your pockets where you die. The host's value is used.");
			ShowSlotValueEntry = ConfigFile.Bind("Display", "ShowSlotValue", true,
				"Show the stored valuable's current value on its inventory slot.");
			EnableSpikes = ConfigFile.Bind("Debug", nameof(EnableSpikes), false,
				"Developer only. Enables prototype hotkeys (F6 hide or show the held valuable, F7 dump its state, F8 sync test, F9 host property test).");

			EnableIconBake = ConfigFile.Bind("Debug", nameof(EnableIconBake), false,
				"Developer only. Press F10 inside a level to render a picture of every valuable to BepInEx/PocketIcons.");

			ConfigFile.Save();
		}

		internal static void Reload()
		{
			ConfigFile?.Reload();
			ConfigFile?.Save();
		}
	}
}
