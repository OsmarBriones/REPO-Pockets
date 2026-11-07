using BepInEx;
using HarmonyLib;
using Pockets.Net;
using UnityEngine;

namespace Pockets
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class PocketsPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "com.osmar.Pockets";
		public const string PluginName = "Pockets";
		public const string PluginVersion = "1.0.0";

		internal Harmony? Harmony { get; set; }
		internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; } = null!;

		private void Awake()
		{
			Logger = base.Logger;

			// Prevent the plugin from being deleted
			this.gameObject.transform.parent = null;
			this.gameObject.hideFlags = HideFlags.HideAndDontSave;

			ConfigurationController.Initialize(this.Config);

			this.gameObject.AddComponent<PocketsRoomEvents>();

			if (ConfigurationController.IconBakeEnabled)
			{
				this.gameObject.AddComponent<Ui.IconBaker>();
				Logger.LogWarning("Icon baking is enabled (Debug.EnableIconBake): press F10 inside a level. Developer use only.");
			}

			Harmony = new Harmony(Info.Metadata.GUID);
			Harmony.PatchAll();

			Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");
		}

		internal void Unpatch()
		{
			Harmony?.UnpatchSelf();
		}
	}
}
