using GameCore;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [Header("Player Spawn Points")]

    public Transform[] spawnPoints;

    [Header("Character Skins")]

    public List<Material> characterHeadSkinMats = new List<Material>();

    public List<Material> characterBodySkinMats = new List<Material>();

    [Header("Player Prefab Path")]

    public string playerPrefabResourcePath = "Player";

    public PlayerCharacter localPlayerChar { get; private set; }

    public static GameSceneManager GameSceneManagerInstance;

    void Awake()
    {
        if(GameSceneManagerInstance && GameSceneManagerInstance != this)
        {
            Destroy(gameObject);

            return;
        }

        GameSceneManagerInstance = this;    

        PhotonNetwork.AutomaticallySyncScene = true;
    }
    void Start()
    {
        var ui = GameManager.Instance.GetUIManager();
        localPlayerChar = SpawnLocalPlayer();
        if (PhotonNetwork.InRoom)
        {
            ui.SetGameplayMode(true);
            PhotonNetwork.SendRate = 30;
            PhotonNetwork.SerializationRate = 30;
            GameManager.Instance.OpenHUD(true);
        }
        if (GameManager.Instance)
        {
            GameManager.Instance.ForceCloseLobby();
            GameManager.Instance.turnOffEV(false);
        }
    }
    private PlayerCharacter SpawnLocalPlayer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return null;
        }

        GameObject playerGO = null;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            int index = Mathf.Clamp(PhotonNetwork.LocalPlayer.ActorNumber - 1, 0, spawnPoints.Length - 1);
            Vector3 pos = spawnPoints[index].position;
            Quaternion rot = spawnPoints[index].rotation;

            playerGO = PhotonNetwork.Instantiate(playerPrefabResourcePath, pos, rot, 0);
        }
        else
        {
            playerGO = Instantiate(Resources.Load<GameObject>(playerPrefabResourcePath), spawnPoints[0].position, spawnPoints[0].rotation);
        }

        return playerGO.GetComponent<PlayerCharacter>();
    }
}
