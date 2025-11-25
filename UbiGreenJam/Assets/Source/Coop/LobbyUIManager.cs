using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject mainMenuPanel; 
    public GameObject lobbyPanel;   

    [Header("Slot UI")]
    public Button[] slotButtons = new Button[3]; 
    public TextMeshProUGUI[] slotNameTexts = new TextMeshProUGUI[3];  
    public TextMeshProUGUI[] slotDescTexts = new TextMeshProUGUI[3]; 

    [Header("Controls")]
    public Button createButton; 
    public Button joinButton;
    public Button backButton;
    public Button backToLobbyButton;

    [Header("Settings")]
    public string gameSceneToLoad = "GameScene";
    public bool allowMasterStartAlone = true;

    private int selectedSlot = -1;
    private readonly string[] slotRoomNames = new string[3] { "Slot_1", "Slot_2", "Slot_3" };
    private readonly Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private string pendingRoomToCreateOrJoin = null;
    private enum PendingAction { None, JoinOrCreateRoom, JoinRoom }
    private PendingAction pendingAction = PendingAction.None;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    void Start()
    {
        if (mainMenuPanel == null) Debug.LogWarning("[LobbyUIManager] mainMenuPanel not assigned.");
        if (lobbyPanel == null) Debug.LogWarning("[LobbyUIManager] lobbyPanel not assigned.");

        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null)
            {
                Debug.LogWarning($"[LobbyUIManager] slotButtons[{i}] is null in Inspector.");
                continue;
            }
            int idx = i;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(idx));
        }

        if (createButton != null)
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(OnCreateOrStartOrReadyClicked);
        }
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }
        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.onClick.AddListener(OnBackToLobbyButtonClicked);
            backToLobbyButton.gameObject.SetActive(false);
        }
        SetControlsInteractable(false);

        RefreshSlotUIAll();
    }

    void SetControlsInteractable(bool enabled)
    {
        if (createButton != null) createButton.interactable = enabled;
        if (joinButton != null) joinButton.interactable = enabled;
        for (int i = 0; i < slotButtons.Length; i++)
            if (slotButtons[i] != null) slotButtons[i].interactable = enabled;
    }

    public void OpenLobby()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.IsConnectedAndReady)
        {
            LobbyLauncher.Instance.ConnectAndJoinLobby();
        }
        else if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        selectedSlot = -1;
        RefreshSlotUIAll();
    }

    public void OnBackToLobbyButtonClicked()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        selectedSlot = -1;

        for (int i = 0; i < slotButtons.Length; i++)
            slotButtons[i].gameObject.SetActive(true);

        if (joinButton != null) joinButton.gameObject.SetActive(true);

        if (backToLobbyButton != null) backToLobbyButton.gameObject.SetActive(false);

        RefreshSlotUIAll();
    }

    public void CloseLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        selectedSlot = -1;
        RefreshSlotUIAll();
    }

    void OnSlotClicked(int index)
    {
        if (index < 0 || index >= slotButtons.Length) return;
        if (selectedSlot == index) selectedSlot = -1;
        else selectedSlot = index;

        UpdateSelectionVisuals();
        UpdateControlButtons();
    }

    void UpdateSelectionVisuals()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;
            ColorBlock cb = slotButtons[i].colors;
            if (selectedSlot == i)
            {
                cb.normalColor = Color.cyan;
            }
            else
            {
                cb.normalColor = Color.white;
            }
            slotButtons[i].colors = cb;
        }
    }

    void UpdateControlButtons()
    {
        if (createButton == null || joinButton == null) return;

        if (PhotonNetwork.InRoom)
            joinButton.gameObject.SetActive(false);
        else
            joinButton.gameObject.SetActive(true);

        joinButton.interactable = false;
        createButton.interactable = false;

        if (selectedSlot < 0)
        {
            UpdateCreateButtonText(selectedSlot);
            return;
        }

        string rn = slotRoomNames[selectedSlot];
        bool inSameRoom = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == rn;

        if (inSameRoom)
        {
            createButton.interactable = true;
            UpdateCreateButtonText(selectedSlot);
            return;
        }

        bool roomExists = RoomExists(rn);
        joinButton.interactable = roomExists;

        createButton.interactable = !roomExists;

        UpdateCreateButtonText(selectedSlot);
    }


    void OnCreateOrStartOrReadyClicked()
    {
        if (selectedSlot < 0) return;
        string rn = slotRoomNames[selectedSlot];

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[LobbyUI] Not connected/ready. Queuing JoinOrCreate for: " + rn);
            pendingRoomToCreateOrJoin = rn;
            pendingAction = PendingAction.JoinOrCreateRoom;

            LobbyLauncher.Instance.ConnectAndJoinLobby();

            SetControlsInteractable(false);
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            RoomOptions options = new RoomOptions { MaxPlayers = 4 };
            PhotonNetwork.JoinOrCreateRoom(rn, options, TypedLobby.Default);
        }
        else
        {
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == rn)
            {
                bool canStart = AllPlayersReady();

                if (!canStart && allowMasterStartAlone)
                {
                    int pc = PhotonNetwork.CurrentRoom.PlayerCount;
                    if (pc == 1) canStart = true;
                }

                if (canStart)
                {
                    PhotonNetwork.LoadLevel(gameSceneToLoad);
                    if (lobbyPanel != null) lobbyPanel.SetActive(false);
                    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                    return;
                }
                else
                {
                    Debug.Log("[LobbyUI] Not all players are ready.");
                }
            }
            else
            {
                ToggleLocalReady();
            }
        }
    }

    void OnJoinClicked()
    {
        if (selectedSlot < 0) return;
        string rn = slotRoomNames[selectedSlot];

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[LobbyUI] Not connected/ready. Queuing Join for: " + rn);
            pendingRoomToCreateOrJoin = rn;
            pendingAction = PendingAction.JoinRoom;
            LobbyLauncher.Instance.ConnectAndJoinLobby();
            SetControlsInteractable(false);
            return;
        }

        if (RoomExists(rn))
        {
            PhotonNetwork.JoinRoom(rn);
        }
    }

    public void OnBackClicked()
    {
        CloseLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                    cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        RefreshSlotUIAll();
    }

    private bool RoomExists(string roomName)
    {
        return cachedRoomList.ContainsKey(roomName);
    }

    void RefreshSlotUIAll()
    {
        for (int i = 0; i < slotNameTexts.Length; i++)
        {
            if (slotNameTexts[i] != null) slotNameTexts[i].text = $"Lobby {(i + 1)}";
            if (slotDescTexts[i] != null) slotDescTexts[i].text = "Empty";
            if (slotButtons[i] != null) slotButtons[i].interactable = true;
        }

        foreach (var kv in cachedRoomList)
        {
            string rname = kv.Key;
            RoomInfo rinfo = kv.Value;
            for (int i = 0; i < slotRoomNames.Length; i++)
            {
                if (rname == slotRoomNames[i])
                {
                    if (slotDescTexts[i] != null)
                        slotDescTexts[i].text = $"Players: {rinfo.PlayerCount}/{rinfo.MaxPlayers}";
                    break;
                }
            }
        }

        UpdateSelectionVisuals();
        UpdateControlButtons();
    }

    void UpdateCreateButtonText(int slotIndex)
    {
        if (createButton == null) return;

        TextMeshProUGUI t = createButton.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return;

        if (slotIndex < 0)
        {
            t.text = "Create";
            return;
        }

        string rn = slotRoomNames[slotIndex];
        bool exists = RoomExists(rn);
        bool inSameRoom = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == rn;

        if (!inSameRoom)
        {
            t.text = exists ? "Create/Join" : "Create";
        }
        else
        {
            if (PhotonNetwork.IsMasterClient) t.text = "Start";
            else t.text = IsLocalReady() ? "Unready" : "Ready";
        }
    }

    void ToggleLocalReady()
    {
        bool cur = IsLocalReady();
        Hashtable props = new Hashtable();
        props["Ready"] = !cur;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    bool IsLocalReady()
    {
        object val;
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Ready", out val))
            return (bool)val;
        return false;
    }

    bool AllPlayersReady()
    {
        if (!PhotonNetwork.InRoom) return false;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.IsMasterClient) continue;
            object v;
            if (!p.CustomProperties.TryGetValue("Ready", out v) || !(v is bool) || !(bool)v)
                return false;
        }
        return PhotonNetwork.PlayerList.Length > 0;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[LobbyUI] Joined room: " + PhotonNetwork.CurrentRoom?.Name);

        Hashtable props = new Hashtable() { { "Ready", false } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        for (int i = 0; i < slotRoomNames.Length; i++)
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == slotRoomNames[i])
            {
                selectedSlot = i;
                break;
            }
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (i == selectedSlot) continue;
            slotButtons[i].gameObject.SetActive(false);
        }

        if (joinButton != null) joinButton.gameObject.SetActive(false);
        if (backToLobbyButton != null) backToLobbyButton.gameObject.SetActive(true);
        if (backButton != null) backButton.gameObject.SetActive(false);

        SetControlsInteractable(true);

        RefreshPlayersInCurrentRoom();
        UpdateControlButtons();
    }


    public override void OnLeftRoom()
    {
        Debug.Log("[LobbyUI] Left room");

        selectedSlot = -1;
        for (int i = 0; i < slotButtons.Length; i++)
            slotButtons[i].gameObject.SetActive(true);

        if (joinButton != null) joinButton.gameObject.SetActive(true);
        if (backToLobbyButton != null) backToLobbyButton.gameObject.SetActive(false);

        RefreshSlotUIAll();
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayersInCurrentRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayersInCurrentRoom();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        RefreshPlayersInCurrentRoom();
    }

    void RefreshPlayersInCurrentRoom()
    {
        for (int i = 0; i < slotDescTexts.Length; i++)
        {
            if (slotDescTexts[i] != null) slotDescTexts[i].text = "Empty";
        }

        if (!PhotonNetwork.InRoom) return;

        int idx = Array.IndexOf(slotRoomNames, PhotonNetwork.CurrentRoom.Name);
        if (idx < 0) return;

        Player[] players = PhotonNetwork.PlayerList;
        Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        string txt = "";
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            object readyObj = null;
            bool ready = false;
            if (p.CustomProperties != null && p.CustomProperties.TryGetValue("Ready", out readyObj))
                ready = (bool)readyObj;

            string name = string.IsNullOrEmpty(p.NickName) ? $"Player{p.ActorNumber}" : p.NickName;
            txt += $"{name}{(ready ? " (Ready)" : "")}";
            if (i < players.Length - 1) txt += "\n";
        }

        if (slotDescTexts[idx] != null) slotDescTexts[idx].text = txt;

        UpdateCreateButtonText(idx);
        UpdateControlButtons();
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
        RefreshSlotUIAll();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[LobbyUI] OnConnectedToMaster: Joining Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[LobbyUI] OnJoinedLobby: lobby list ready.");
        SetControlsInteractable(true);
        RefreshSlotUIAll();

        if (!string.IsNullOrEmpty(pendingRoomToCreateOrJoin))
        {
            string rn = pendingRoomToCreateOrJoin;
            var action = pendingAction;
            pendingRoomToCreateOrJoin = null;
            pendingAction = PendingAction.None;

            if (action == PendingAction.JoinOrCreateRoom)
            {
                RoomOptions options = new RoomOptions { MaxPlayers = 4 };
                PhotonNetwork.JoinOrCreateRoom(rn, options, TypedLobby.Default);
            }
            else if (action == PendingAction.JoinRoom)
            {
                if (RoomExists(rn))
                    PhotonNetwork.JoinRoom(rn);
                else
                    Debug.LogWarning("[LobbyUI] Pending JoinRoom: room not found in cache: " + rn);
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[LobbyUI] Disconnected: " + cause);
        SetControlsInteractable(false);
    }
}
