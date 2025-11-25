using UnityEngine;
using Photon.Pun;
using GameCore;

public class GameSceneManager : MonoBehaviour
{
    public Transform[] spawnPoints; 
    public string playerPrefabResourcePath = "Player"; 

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    void Start()
    {
        SpawnLocalPlayer();
        GameManager.Instance.ForceCloseLobby();
    }

    void SpawnLocalPlayer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            int index = Mathf.Clamp(PhotonNetwork.LocalPlayer.ActorNumber - 1, 0, spawnPoints.Length - 1);
            Vector3 pos = spawnPoints[index].position;
            Quaternion rot = spawnPoints[index].rotation;

            GameObject playerGO = PhotonNetwork.Instantiate(playerPrefabResourcePath, pos, rot, 0);
        }
        else
        {
            Instantiate(Resources.Load<GameObject>(playerPrefabResourcePath), spawnPoints[0].position, spawnPoints[0].rotation);
        }
    }
}
