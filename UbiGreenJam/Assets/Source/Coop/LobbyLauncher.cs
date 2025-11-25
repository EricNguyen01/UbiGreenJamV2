using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LobbyLauncher : MonoBehaviourPunCallbacks
{
    public static LobbyLauncher Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
    }
    public void ConnectAndJoinLobby()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
            return;
        }

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[LobbyLauncher] Connected to Master. Joining Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[LobbyLauncher] Disconnected: " + cause);
    }
}
