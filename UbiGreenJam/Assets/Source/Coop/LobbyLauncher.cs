using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class LobbyLauncher : MonoBehaviourPunCallbacks
{
    public static LobbyLauncher Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1.0.0"; 
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "asia";
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
        Debug.Log("[LobbyLauncher] Connected to Master. Waiting until ready to join lobby...");

        StartCoroutine(WaitUntilReadyThenJoinLobby());
    }

    private IEnumerator WaitUntilReadyThenJoinLobby()
    {
        while (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMaster || !PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;
        }

        Debug.Log("[LobbyLauncher] Ready. Joining Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[LobbyLauncher] Disconnected: " + cause);
    }
}
