using BepInEx.Configuration;
using Pockets.Api;
using Pockets.Domain;
using Pockets.Net;

namespace Pockets
{
	internal class ConfigurationController
	{
		private static ConfigFile? ConfigFile { get; set; }
		private static ConfigEntry<bool>? Enabled { get; set; }
		private static ConfigEntry<PocketSize>? MaxValuableSizeEntry { get; set; }
		private static ConfigEntry<bool>? DropOnDeathEntry { get; set; }
		private static ConfigEntry<bool>? ShowSlotValueEntry { get; set; }
		private static ConfigEntry<bool>? EnableIconBake { get; set; }

		internal static bool IsEnabled => Enabled?.Value ?? true;

		/// <summary>This machine's own setting for the largest storable size (what the host publishes when hosting).</summary>
		internal static PocketSize LocalMaxValuableSize => MaxValuableSizeEntry?.Value ?? PocketSize.Tiny;

		/// <summary>This machine's own setting for dropping on death (what the host publishes when hosting).</summary>
		internal static bool LocalDropOnDeath => DropOnDeathEntry?.Value ?? true;

		/// <summary>Largest valuable size that can be stored. Clients use the host's value.</summary>
		internal static PocketSize MaxValuableSize => HostSettings.TryGetMaxSize(out PocketSize hostSize) ? hostSize : LocalMaxValuableSize;

		/// <summary>Whether stored valuables drop where the player dies. Clients use the host's value.</summary>
		internal static bool DropOnDeath => HostSettings.TryGetDropOnDeath(out bool hostDrop) ? hostDrop : LocalDropOnDeath;

		/// <summary>Whether the slot shows the stored valuable's value: the player's setting, forced on while a mod such as CharmCore requires it. Local only.</summary>
		internal static bool ShowSlotValue => (ShowSlotValueEntry?.Value ?? true) || PocketsApi.SlotValueRequired;

		/// <summary>Opt-in developer tool: F10 renders every valuable prefab to a PNG in BepInEx/PocketIcons. Disabled by default.</summary>
		internal static bool IconBakeEnabled => EnableIconBake?.Value ?? false;

		internal static void Initialize(ConfigFile config)
		{
			ConfigFile = config;

			Enabled = ConfigFile.Bind("General", nameof(Enabled), true, "Enable or disable this mod. Turning it off drops whatever is in your pockets.");
			Enabled.SettingChanged += (_, _) => OnEnabledChanged();
			MaxValuableSizeEntry = ConfigFile.Bind("General", "MaxValuableSize", PocketSize.Tiny,
				"Largest valuable that can be stored in a pocket. Every size up to this one is accepted (Tiny, Small, Medium, Big, Wide, Tall, VeryTall). The host's value is used.");
			DropOnDeathEntry = ConfigFile.Bind("General", "DropOnDeath", true,
				"Drop the valuables stored in your pockets where you die. The host's value is used.");
			ShowSlotValueEntry = ConfigFile.Bind("Display", "ShowSlotValue", true,
				"Show the stored valuable's current value on its inventory slot. Other mods that depend on the value turn it on regardless of this setting.");
			EnableIconBake = ConfigFile.Bind("Debug", nameof(EnableIconBake), false,
				"Developer only. Press F10 inside a level to render a picture of every valuable to BepInEx/PocketIcons.");

			ConfigFile.Save();
		}

		private static void OnEnabledChanged()
		{
			// Switching the mod off must not strand valuables in hidden pockets.
			if (!IsEnabled)
			{
				PocketReleaser.ReleaseLocalPockets();
			}
		}

		internal static void Reload()
		{
			ConfigFile?.Reload();
			ConfigFile?.Save();
		}
	}
}
