using GameCore;
using UnityEngine;
using TMPro;
using System.Collections;
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
    public CanvasGroup m_MainMenu;
    public CanvasGroup m_Tutorial;
    public CanvasGroup m_InfoPopup;
    public CanvasGroup m_Lobby;
    public LobbyUIManager lobbyUIManager;

    public float fadeDuration = 0.3f;

    public void Start()
    {
        m_MainMenu.alpha = 1;
        MainMenu.gameObject.SetActive(true);

        m_Tutorial.alpha = 0;
        Tutorial.gameObject.SetActive(false);
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
        StartCoroutine(FadeOut(m_Tutorial));
        StartCoroutine(FadeIn(m_MainMenu));
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
