using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pockets.Spikes;

/// <summary>
/// Developer-only hotkeys for the technical spikes. Only added when Debug.EnableSpikes is on.
/// F6 hide the held valuable or show the last hidden one, F7 dump its state, F8 publish the
/// hidden ids as a player property and list every player's property, F9 host tries to write
/// another player's property.
/// </summary>
internal sealed class SpikeHotkeys : MonoBehaviour
{
	private const string PropertyKey = "pockets.spike";

	private void Update()
	{
		Keyboard? keyboard = Keyboard.current;
		if (keyboard == null || !ConfigurationController.SpikesEnabled)
		{
			return;
		}

		if (keyboard.f6Key.wasPressedThisFrame)
		{
			ToggleHeld();
		}

		if (keyboard.f7Key.wasPressedThisFrame)
		{
			Dump();
		}

		if (keyboard.f8Key.wasPressedThisFrame)
		{
			PublishAndList();
		}

		if (keyboard.f9Key.wasPressedThisFrame)
		{
			HostWritesOtherPlayers();
		}
	}

	private static SpikePocketed? HeldValuable()
	{
		PhysGrabber? grabber = PhysGrabber.instance;
		if (grabber == null || !grabber.grabbed || grabber.grabbedPhysGrabObject == null)
		{
			return null;
		}

		return grabber.grabbedPhysGrabObject.GetComponent<SpikePocketed>();
	}

	private static void ToggleHeld()
	{
		SpikePocketed? held = HeldValuable();
		if (held != null && !held.IsHidden)
		{
			PocketsPlugin.Logger.LogInfo($"[Spike S1] hiding held valuable {held.name}");
			held.RequestHide();
			return;
		}

		SpikePocketed? last = SpikePocketed.LastHidden;
		Camera? camera = Camera.main;
		if (last != null && last.IsHidden && camera != null)
		{
			Transform view = camera.transform;
			PocketsPlugin.Logger.LogInfo($"[Spike S1] showing hidden valuable {last.name} in front of the camera");
			last.RequestShow(view.position + view.forward * 1.5f, Quaternion.LookRotation(view.forward, Vector3.up));
			return;
		}

		PocketsPlugin.Logger.LogInfo("[Spike S1] nothing to hide or show: hold a valuable first");
	}

	private static void Dump()
	{
		SpikePocketed? target = HeldValuable() ?? SpikePocketed.LastHidden;
		if (target == null)
		{
			PocketsPlugin.Logger.LogInfo("[Spike dump] no target: hold a valuable or hide one first");
			return;
		}

		PocketsPlugin.Logger.LogInfo(target.DumpState());
	}

	private static void PublishAndList()
	{
		if (!PhotonNetwork.InRoom)
		{
			PocketsPlugin.Logger.LogInfo("[Spike S2] not in a room");
			return;
		}

		string ids = string.Join(",", SpikePocketed.Hidden
			.Where(hidden => hidden != null)
			.Select(hidden => hidden.GetComponent<PhotonView>().ViewID.ToString()));
		PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { PropertyKey, ids } });
		PocketsPlugin.Logger.LogInfo($"[Spike S2] published '{ids}' as my player property");

		foreach (Player player in PhotonNetwork.PlayerList)
		{
			player.CustomProperties.TryGetValue(PropertyKey, out object? value);
			PocketsPlugin.Logger.LogInfo($"[Spike S2] player {player.NickName} (actor {player.ActorNumber}) {PropertyKey}={value ?? "<none>"}");
		}
	}

	private static void HostWritesOtherPlayers()
	{
		if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
		{
			PocketsPlugin.Logger.LogInfo("[Spike S2] only the master client can run this test, inside a room");
			return;
		}

		foreach (Player player in PhotonNetwork.PlayerList.Where(player => !player.IsLocal))
		{
			bool accepted = player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { PropertyKey, "written-by-host" } });
			PocketsPlugin.Logger.LogInfo($"[Spike S2] host wrote to {player.NickName}: SetCustomProperties returned {accepted}");
		}
	}
}
