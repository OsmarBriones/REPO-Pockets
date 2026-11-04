using Photon.Pun;
using Pockets.Domain;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Pockets.Net;

/// <summary>
/// Makes the host's <c>MaxValuableSize</c> and <c>DropOnDeath</c> the ones everybody uses. The master writes them as room
/// properties (Photon delivers them to every client, including late joiners) and clients read them on demand.
/// </summary>
internal static class HostSettings
{
	private const string MaxSizeKey = "pockets.maxSize";
	private const string DropOnDeathKey = "pockets.dropOnDeath";

	/// <summary>Master only: publishes this machine's settings to the room.</summary>
	internal static void Publish()
	{
		if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
		{
			return;
		}

		PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
		{
			{ MaxSizeKey, (int)ConfigurationController.LocalMaxValuableSize },
			{ DropOnDeathKey, ConfigurationController.LocalDropOnDeath },
		});
	}

	/// <summary>The host's size limit, when this machine is a client and the host has published it.</summary>
	internal static bool TryGetMaxSize(out PocketSize size)
	{
		size = PocketSize.Tiny;
		if (!UseHostValues || !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MaxSizeKey, out object? value) || value is not int number)
		{
			return false;
		}

		return SizePolicy.TryFromGameVolume(number, out size);
	}

	/// <summary>The host's drop-on-death setting, when this machine is a client and the host has published it.</summary>
	internal static bool TryGetDropOnDeath(out bool drop)
	{
		drop = true;
		if (!UseHostValues || !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(DropOnDeathKey, out object? value) || value is not bool flag)
		{
			return false;
		}

		drop = flag;
		return true;
	}

	private static bool UseHostValues => PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient;
}
