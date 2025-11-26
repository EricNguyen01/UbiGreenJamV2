using UnityEngine;
using Photon.Pun;
using GameCore;

public class GameSceneManager : MonoBehaviour
{
    public Transform[] spawnPoints;

    public PlayerCharacter characterPrefabToSpawnLocal;

    public string playerPrefabResourcePath = "Assets/Prefabs/SystemPrefabs/Player.prefab";

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
        localPlayerChar = SpawnLocalPlayer();

        if(GameManager.Instance) GameManager.Instance.ForceCloseLobby();
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
