using Photon.Pun;
using Photon.Realtime;
using Pockets.Api;

namespace Pockets.Net;

/// <summary>
/// Reacts to players joining and leaving. The master publishes its settings, tells a new player what is already
/// in pockets, and drops the pockets of a player who disconnects where their valuables were hidden.
/// </summary>
internal sealed class PocketsRoomEvents : MonoBehaviourPunCallbacks
{
	public override void OnJoinedRoom()
	{
		HostSettings.Publish();
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		HostSettings.Publish();
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}

		HostSettings.Publish();
		foreach (System.Collections.Generic.KeyValuePair<int, PlayerPockets> entry in PocketsApi.Snapshot())
		{
			for (int slot = 0; slot < entry.Value.Valuables.Length; slot++)
			{
				ValuableObject? valuable = entry.Value.Valuables[slot];
				PocketedValuable? pocketed = valuable != null ? valuable.GetComponent<PocketedValuable>() : null;
				if (pocketed != null)
				{
					pocketed.SendStateTo(newPlayer, entry.Key, slot);
				}
			}
		}
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}

		foreach (System.Collections.Generic.KeyValuePair<int, PlayerPockets> entry in PocketsApi.Snapshot())
		{
			PlayerAvatar? avatar = PlayerKeys.Resolve(entry.Key);
			bool belongsToLeaver = avatar == null || avatar.photonView.Owner == null || avatar.photonView.Owner.ActorNumber == otherPlayer.ActorNumber;
			if (!belongsToLeaver)
			{
				continue;
			}

			for (int slot = 0; slot < entry.Value.Valuables.Length; slot++)
			{
				ValuableObject? valuable = entry.Value.Valuables[slot];
				if (valuable != null)
				{
					// Drop it where it was hidden: the leaving player's avatar may already be gone.
					PocketsApi.TryRemove(entry.Key, slot, valuable.transform.position);
				}
			}
		}
	}
}
