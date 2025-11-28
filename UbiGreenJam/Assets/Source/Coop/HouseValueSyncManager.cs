using Photon.Pun;
using UnityEngine;
using GameCore;
using TMPro;
using static FloodController;

public class HouseValueSyncManager : MonoBehaviourPun, IPunObservable
{
    public static HouseValueSyncManager Instance { get; private set; }

    public int syncedHouseValue { get; private set; } = 0;
    public int syncedMaxHouseValue { get; private set; } = 0;

    [Header("Flood HUD Settings")]
    public float prepareDuration = 10f;          
    private double floodStartTimePhoton = 0.0; 
    private bool floodActive = false;            
    public float stormDuration = 120f;
    private UIManager ui;
    public RainLevel[] syncedRainForecast = new RainLevel[5];
    private int startHouseValue = 0;
    private int endHouseValue = 0;
    private bool endPopupShown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private bool IsUILoading()
    {
        var ui = GameManager.Instance.GetUIManager();
        return ui != null && ui.isLoading;
    }
    void Start()
    {
        var ui = GameManager.Instance.GetUIManager();
        endPopupShown = false;

        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            floodStartTimePhoton = PhotonNetwork.InRoom ? PhotonNetwork.Time + prepareDuration
                                                        : Time.time + prepareDuration;
            floodActive = false;

            if (FloodController.FloodControllerInstance)
            {
                FloodController.FloodControllerInstance.StopFlood();
                FloodController.FloodControllerInstance.StartLowering();
            }
        }
    }


    private void Update()
    {
        if (IsUILoading()) return;
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            CalculateAndSyncHouseValue();
            ControlFlood();
            SyncRainForecast();
        }
        UpdateFloodHUD();
    }
    void SyncRainForecast()
    {
        var ui = GameManager.Instance.GetUIManager();
        var flood = FloodController.FloodControllerInstance;
        if (flood == null) return;

        syncedRainForecast = flood.rainForecast;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SyncRainForecast), RpcTarget.AllBuffered, (int)syncedRainForecast[0], (int)syncedRainForecast[1], (int)syncedRainForecast[2], (int)syncedRainForecast[3], (int)syncedRainForecast[4]);
        }
        else
        {
            ui.UpdateRainForecastUI(syncedRainForecast);
        }
    }

    public void CalculateAndSyncHouseValue()
    {
        int total = 0;
        foreach (var item in GameManager.Instance.interactablesInSceneRuntime)
        {
            if (item != null && item.itemData != null)
            {
                total += item.itemData.cost;
            }
        }

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (syncedMaxHouseValue <= 0) syncedMaxHouseValue = total;
                photonView.RPC(nameof(RPC_SyncHouseValue), RpcTarget.AllBuffered, total, syncedMaxHouseValue);
            }
        }
        else
        {
            syncedHouseValue = total;
            if (syncedMaxHouseValue <= 0) syncedMaxHouseValue = total;
            GameManager.Instance.UpdateHouseValue();
        }
    }

    [PunRPC]
    void RPC_SyncHouseValue(int value, int maxValue)
    {
        syncedHouseValue = value;
        syncedMaxHouseValue = maxValue;
        GameManager.Instance.UpdateHouseValue();
    }

    private void UpdateFloodHUD()
    {
        var ui = GameManager.Instance.GetUIManager();
        if (ui == null || ui.stormPhaseText == null) return;
        var flood = FloodController.FloodControllerInstance;
        if (flood == null) return;

        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.time;

        if (!floodActive)
        {
            double remaining = floodStartTimePhoton - now;
            if (remaining < 0) remaining = 0;

            ui.stormPhaseText.text =
                $"Storm starts in: <color=#EDBE24>{GameManager.FormatTime((float)remaining)}</color>";
        }
        else
        {
            double elapsed = now - floodStartTimePhoton;
            double remaining = stormDuration - elapsed;
            if (remaining < 0) remaining = 0;

            ui.stormPhaseText.text =
                $"Survive the storm: <color=#EE6148>{GameManager.FormatTime((float)remaining)}</color>";

            if (remaining <= 0 || flood.GetNormalizedFloodLevel() >= 1f)
            {
                floodActive = false;
                flood.StopFlood();
                EndFloodAndShowReport();
            }
        }
    }
    private void EndFloodAndShowReport()
    {
        var flood = FloodController.FloodControllerInstance;
        if (flood == null) return;

        floodActive = false;
        flood.StopFlood();

        endHouseValue = syncedHouseValue;
        int loss = Mathf.Max(0, startHouseValue - endHouseValue);

        if (endPopupShown) return; 
        endPopupShown = true;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_ShowEndReport), RpcTarget.AllBuffered, loss);
        }
        else if (!PhotonNetwork.InRoom)
        {
            GameManager.Instance.GetUIManager()?.ShowEndReport(loss);
        }

        Debug.Log($"[Flood] Ended. start={startHouseValue}, end={endHouseValue}, loss={loss}");
    }

    private void ControlFlood()
    {
        var flood = FloodController.FloodControllerInstance;
        if (flood == null) return;

        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.time;

        if (!floodActive && now >= floodStartTimePhoton)
        {
            floodActive = true;
            flood.StartFlood();
            startHouseValue = syncedHouseValue;
            endPopupShown = false;
        }

        if (floodActive && flood.GetNormalizedFloodLevel() >= 1f)
        {
            floodActive = false;
            flood.StopFlood();
        }
    }

    // ---------- PHOTON SYNC ----------
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(floodActive);
            stream.SendNext(floodStartTimePhoton);
            stream.SendNext(prepareDuration);
            stream.SendNext(stormDuration);
        }
        else
        {
            floodActive = (bool)stream.ReceiveNext();
            floodStartTimePhoton = (double)stream.ReceiveNext();
            prepareDuration = (float)stream.ReceiveNext();
            stormDuration = (float)stream.ReceiveNext();
        }
    }
    [PunRPC]
    void RPC_SyncRainForecast(int r0, int r1, int r2, int r3, int r4)
    {
        syncedRainForecast[0] = (RainLevel)r0;
        syncedRainForecast[1] = (RainLevel)r1;
        syncedRainForecast[2] = (RainLevel)r2;
        syncedRainForecast[3] = (RainLevel)r3;
        syncedRainForecast[4] = (RainLevel)r4;

        GameManager.Instance.GetUIManager()?.UpdateRainForecastUI(syncedRainForecast);
    }
    [PunRPC]
    void RPC_ShowEndReport(int loss)
    {
        GameManager.Instance.GetUIManager()?.ShowEndReport(loss);
    }
}
