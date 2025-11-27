using GameCore;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.EventSystems;
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
    private float maxHouseValue = 0f;
    private float currentHouseValue = 0f;

    public float fadeDuration = 0.3f;
    private bool isPaused = false;

    public void Start()
    {
        m_MainMenu.alpha = 1;
        MainMenu.gameObject.SetActive(true);

        m_Tutorial.alpha = 0;
        Tutorial.gameObject.SetActive(false);

        m_credits.alpha = 0;
        Credits.gameObject.SetActive(false);

        m_Lobby.alpha = 0;
        Lobby.gameObject.SetActive(false);

        PauseMenu.gameObject.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        if (houseValueHUD != null && houseValueHUD.activeSelf)
        {
            UpdateHouseValueHUD();
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
    public void UpdateHouseValueHUD()
    {
        currentHouseValue = 0f;
        foreach (var item in GameManager.Instance.interactablesInSceneRuntime)
        {
            if (item != null && item.itemData != null)
            {
                currentHouseValue += item.itemData.cost;
            }
        }

        if (maxHouseValue <= 0f)
        {
            maxHouseValue = currentHouseValue;
        }

        if (houseValueText != null)
            houseValueText.text = $"₫ {Mathf.RoundToInt(currentHouseValue)}";

        if (houseValueBar != null)
            houseValueBar.fillAmount = Mathf.Clamp01(currentHouseValue / maxHouseValue);
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

    void RestartGame()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
        GameManager.Instance.RestartGame(); 
    }

    void BackToMainMenu()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
        GameManager.Instance.ReturnToMainMenu(); 
    }

    void BackToLobby()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
        {
            StartCoroutine(WaitForLeftRoomThenReturnToMainMenu());
            PhotonNetwork.LeaveRoom();
        }
        else
        {
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
