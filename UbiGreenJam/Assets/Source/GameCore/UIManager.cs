using GameCore;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.EventSystems;
using static FloodController;
public class UIManager : MonoBehaviour
{
    [Header("UI Promf")]
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public GameObject InfoPopup; 
    public GameObject MainMenu;
    public GameObject Tutorial;
    public GameObject Lobby;
    public GameObject Credits;
    public CanvasGroup m_MainMenu;
    public CanvasGroup m_Tutorial;
    public CanvasGroup m_InfoPopup;
    public CanvasGroup m_Lobby;
    public CanvasGroup m_credits;
    public LobbyUIManager lobbyUIManager;
    public EventSystem eventSystem;
    
    [Header("Pause Menu")]
    public GameObject PauseMenu;
    public CanvasGroup m_PauseMenu;
    public Button[] pauseButtons; 
    public TextMeshProUGUI[] pauseButtonTexts; 
    [Header("House Value HUD")]
    public TextMeshProUGUI houseValueText;
    public Image houseValueBar;
    public GameObject houseValueHUD;
    public TextMeshProUGUI stormPhaseText;
    public TextMeshProUGUI timerText;
    private float maxHouseValue = 0f;
    private float currentHouseValue = 0f;

    public float fadeDuration = 0.3f;
    private bool isPaused = false;
    [Header("Rain Forecast UI")]
    public Image nowRainImage;
    public Image[] slotRainImages = new Image[4]; 
    public Sprite rainSprite;
    public Sprite rainLightSprite;
    public Sprite rainMediumSprite;
    public Sprite rainHeavySprite;
    public Sprite rainExtremeSprite;
    [Header("End Game Popup")]
    public GameObject endGamePopup;            
    public TextMeshProUGUI endReportText;
    public GameObject RSBtn;
    public GameObject BackBtn;
    public GameObject RSMultiBtn;
    private bool isInGameplay = false;
    [Header("Loading Popup")]
    public GameObject loadingPopup;
    public TextMeshProUGUI loadingText;
    public bool isLoading = false;
    [Header("Pre-storm indicator")]
    public GameObject preStormGO;
    public Image preStormImage;
    public Sprite[] preStormSprites;  
    [Header("Info Screen")]
    public GameObject infoScreen;
    public CanvasGroup m_InfoScreen;
    public TextMeshProUGUI infoText; 
    private bool isGameEnded = false;
    public void Start()
    {
        StartCoroutine(ShowInfoScreen(5f));
    }
    public void SetGameplayMode(bool active)
    {
        isInGameplay = active;
    }
    void Update()
    {
        if (isInGameplay && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        if (houseValueHUD != null && houseValueHUD.activeSelf)
        {
            UpdateHouseValueHUD();
        }
    }
    public void SetGameEnded(bool ended)
    {
        isGameEnded = ended;
    }
    public IEnumerator ShowInfoScreen(float duration)
    {
        if (infoScreen == null || m_InfoScreen == null) yield break;

        infoScreen.SetActive(true);
        m_InfoScreen.alpha = 1;
        m_InfoScreen.interactable = true;
        m_InfoScreen.blocksRaycasts = true;

        float elapsed = 0f;
        int dotCount = 0;
        while (elapsed < duration)
        {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);
            if (infoText != null) infoText.text = $"Loading{dots}";

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            m_InfoScreen.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
        m_InfoScreen.alpha = 0;
        infoScreen.SetActive(false);

        if (MainMenu != null)
        {
            MainMenu.SetActive(true);
            m_MainMenu.alpha = 1;
            m_MainMenu.interactable = true;
            m_MainMenu.blocksRaycasts = true;
        }
    }

    void TogglePauseMenu()
    {
        if (PauseMenu.activeSelf)
        {
            HidePauseMenu();
        }
        else
        {
            ShowPauseMenu();
        }
    }
    public void ShowPreStormIndicator(bool show)
    {
        if (!preStormGO || !preStormImage) return;
        preStormGO.SetActive(show);
    }
    public IEnumerator PlayPreStormSequence(float durationSeconds)
    {
        if (!preStormGO || !preStormImage || preStormSprites == null || preStormSprites.Length < 3)
            yield break;

        preStormGO.SetActive(true);
        float perSprite = durationSeconds / 3f;
        for (int i = 0; i < 3; i++)
        {
            preStormImage.sprite = preStormSprites[i];
            yield return new WaitForSeconds(perSprite);
        }

        preStormGO.SetActive(false);
        if (!isGameEnded && AudioManager.Instance)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StormStart, transform.position);
        }
    }
    public IEnumerator ShowLoadingPopup(float duration)
    {
        if (loadingPopup == null || loadingText == null) yield break;

        loadingPopup.SetActive(true);
        isLoading = true;

        float elapsed = 0f;
        int dotCount = 0;

        while (elapsed < duration)
        {
            dotCount = (dotCount + 1) % 4; 
            string dots = new string('.', dotCount);
            loadingText.text = $"Loading{dots}";

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        loadingPopup.SetActive(false);
        isLoading = false;
    }
    public void ShowEndReport(int lossVND)
    {
        SetGameEnded(true);
        if (!endGamePopup || !endReportText) return;

        string formattedLoss = HelperFunction.FormatCostWithDots(lossVND.ToString());
        endReportText.text =
            "STORM REPORT\n\n" +
            $"You lost <color=#EDBE24>{formattedLoss} VND</color> worth of belongings.\n" +
            "For many families, real flood damage is far greater and life-changing.\n" +
            "If you'd like to help, please consider donating:\n\n" +
            "LINK : https://www.peacetreesvietnam.org/news-events/central-vietnam-flood-relief.html";

        endGamePopup.SetActive(true);
        bool isMultiplayer = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        var mouseLook = GameSceneManager.GameSceneManagerInstance.localPlayerChar.GetComponent<MouseLook>();
        if (mouseLook) mouseLook.SetMouseEnabled(true);
        if(PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            RSBtn.SetActive(false);
            BackBtn.SetActive(false);
            RSMultiBtn.SetActive(true);
        }
        else
        {
            RSBtn.SetActive(true);
            BackBtn.SetActive(true);
            RSMultiBtn.SetActive(false);
        }
    }
    public void HideEndReport()
    {
        if (endGamePopup) endGamePopup.SetActive(false);
    }
    public void UpdateHouseValueHUD()
    {
        int current = HouseValueSyncManager.Instance.syncedHouseValue;
        int max = HouseValueSyncManager.Instance.syncedMaxHouseValue;

        if (max <= 0) max = current;

        if (houseValueText != null)
            houseValueText.text = $"{HelperFunction.FormatCostWithDots(Mathf.RoundToInt(current).ToString())}";

        if (houseValueBar != null)
            houseValueBar.fillAmount = Mathf.Clamp01((float)current / max);
    }
    public void UpdateStormPhaseText(string message, string hexColor)
    {
        if (stormPhaseText == null) return;

        Color color;
        if (ColorUtility.TryParseHtmlString(hexColor, out color))
            stormPhaseText.color = color;

        stormPhaseText.text = message;
    }
    public void ShowHouseValueHUD(bool show)
    {
        if (houseValueHUD != null)
            houseValueHUD.SetActive(show);
    }
    public void UpdateRainForecastUI(RainLevel[] forecast)
    {
        if (forecast == null || forecast.Length < 5) return;

        nowRainImage.sprite = GetRainSprite(forecast[0]);

        for (int i = 0; i < 4; i++)
        {
            if (slotRainImages[i] != null)
                slotRainImages[i].sprite = GetRainSprite(forecast[i + 1]);
        }
    }

    private Sprite GetRainSprite(RainLevel level)
    {
        switch (level)
        {
            case RainLevel.VeryLight: return rainSprite;
            case RainLevel.Light: return rainLightSprite;
            case RainLevel.Medium: return rainMediumSprite;
            case RainLevel.Heavy: return rainHeavySprite;
            case RainLevel.Extreme: return rainExtremeSprite;
            default: return null;
        }
    }

    void ShowPauseMenu()
    {
        StopAllCoroutines();
        PauseMenu.SetActive(true);
        pauseButtonTexts[0].gameObject.SetActive(true);
        pauseButtonTexts[1].gameObject.SetActive(true);
        pauseButtonTexts[2].gameObject.SetActive(true);
        bool isMultiplayer = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        var mouseLook = GameSceneManager.GameSceneManagerInstance.localPlayerChar.GetComponent<MouseLook>();
        if (mouseLook) mouseLook.SetMouseEnabled(true);
        if (!isMultiplayer)
        {
            Time.timeScale = 0f;
            isPaused = true;

            pauseButtonTexts[0].text = "Resume";
            pauseButtonTexts[1].text = "Restart";
            pauseButtonTexts[2].text = "Back To Main Menu";

            pauseButtons[0].onClick.RemoveAllListeners();
            pauseButtons[0].onClick.AddListener(ResumeGame);

            pauseButtons[1].onClick.RemoveAllListeners();
            pauseButtons[1].onClick.AddListener(RestartGame);

            pauseButtons[2].onClick.RemoveAllListeners();
            pauseButtons[2].onClick.AddListener(BackToMainMenu);
        }
        else
        {
            Time.timeScale = 1f;
            isPaused = false;

            pauseButtonTexts[0].text = "Resume";
            pauseButtonTexts[1].text = "Back To Main Menu";

            pauseButtons[0].onClick.RemoveAllListeners();
            pauseButtons[0].onClick.AddListener(HidePauseMenu);

            pauseButtons[1].onClick.RemoveAllListeners();
            pauseButtons[1].onClick.AddListener(BackToLobby);

            pauseButtons[2].gameObject.SetActive(false);
        }
    }

    void HidePauseMenu()
    {
        StopAllCoroutines();
        PauseMenu.SetActive(false);

        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
        var mouseLook = GameSceneManager.GameSceneManagerInstance.localPlayerChar.GetComponent<MouseLook>();
        if (mouseLook) mouseLook.SetMouseEnabled(false);
    }
    void ResumeGame()
    {
        HidePauseMenu();
    }

    public void RestartGame()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
        SetGameEnded(false);
        GameManager.Instance.RestartGame(); 
    }

    public void BackToMainMenu()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
        SetGameEnded(false);
        GameManager.Instance.ReturnToMainMenu(); 
    }

    void BackToLobby()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
        {
            SetGameEnded(false);
            StartCoroutine(WaitForLeftRoomThenReturnToMainMenu());
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SetGameEnded(false);
            GameManager.Instance.ReturnToMainMenu();
        }
    }
    private IEnumerator WaitForLeftRoomThenReturnToMainMenu()
    {
        while (PhotonNetwork.InRoom)
            yield return null;

        GameManager.Instance.ReturnToMainMenu();
    }
    public void OnStartBtnClicks()
    {
        GameManager.Instance.StartGame();
    }
    public void ShowUI(bool show)
    {
        MainMenu.SetActive(show);
    }
    public void OnTutorialBtnClicks()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut(m_MainMenu));
        StartCoroutine(FadeIn(m_Tutorial));
    }
    public void OnCreditsBtnClicks()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut(m_MainMenu));
        StartCoroutine(FadeIn(m_credits));
    }
    public void OnLobbyBtnClicks()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut(m_MainMenu));
        StartCoroutine(FadeIn(m_Lobby));
        lobbyUIManager.OpenLobby();
    }
    public void OnBackLobbyBtn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut(m_Lobby));
        StartCoroutine(FadeIn(m_MainMenu));
    }
    public void OnBackBtn()
    {
        StopAllCoroutines();
        if(Tutorial.activeSelf && !Credits.activeSelf)
        {
            StartCoroutine(FadeOut(m_Tutorial));
            StartCoroutine(FadeIn(m_MainMenu));
        }
        else if(!Tutorial.activeSelf && Credits.activeSelf)
        {
            StartCoroutine(FadeOut(m_credits));
            StartCoroutine(FadeIn(m_MainMenu));
        }
    }
    public void Show(string name, string message, int cost)
    {
        nameText.text = name;
        promptText.text = message;
        if (costText) costText.text = $"₫ {cost}";
        StartCoroutine(FadeIn(m_InfoPopup));
    }

    public void Hide()
    {
        StartCoroutine(FadeOut(m_InfoPopup));
    }
    IEnumerator FadeIn(CanvasGroup cg)
    {
        cg.gameObject.SetActive(true);
        cg.interactable = true;
        cg.blocksRaycasts = true;

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1;
    }

    IEnumerator FadeOut(CanvasGroup cg)
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 0;
        cg.gameObject.SetActive(false);
    }
}
