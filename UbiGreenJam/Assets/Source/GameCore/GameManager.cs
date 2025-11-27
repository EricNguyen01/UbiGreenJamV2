using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scenes")]
        public string uiSceneName = "UIManager";
        public string gameSceneName = "GameScene";

        private bool _uiSceneLoaded = false;
        private UIManager _uiManager;

        public GameStats gameStats;
        public ScriptableStormData stormDataAsset;
        public InteractableItemDatabase interactableDatabase;
        public InteractableBase currentHeldItem;

        private GameStateBase _currentState;

        public StormBase CurrentStorm { get; private set; }

        public List<InteractableBase> interactablesInSceneRuntime = new List<InteractableBase>();

        #region Events
        public event Action<GameStateBase> OnStateChanged;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameEnded;
        #endregion
        public double phaseStartTime;        
        public double localPhaseStartTime;   
        public float prepareDuration;
        public float stormWrapDuration = 15f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (gameStats == null)
                gameStats = new GameStats();

            if (stormDataAsset != null)
                CurrentStorm = new StormBase(stormDataAsset);
        }

        private void Start()
        {
            LoadUIScene();
            ChangeState(new GameStartMenuState(this));
        }

        private void Update()
        {
            _currentState?.OnUpdate();
        }
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == gameSceneName)
            {
                Debug.Log("[GAME] Multiplayer scene loaded → starting prepare phase");
                turnOffEV(false);
                StartPreparePhase();
            }
        }

        // ------------------------------------------------------------
        // SCENE LOADING
        // ------------------------------------------------------------

        private void LoadUIScene()
        {
            if (_uiSceneLoaded) return;

            SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive)
                .completed += (op) =>
                {
                    _uiSceneLoaded = true;

                    Scene uiScene = SceneManager.GetSceneByName(uiSceneName);
                    foreach (var obj in uiScene.GetRootGameObjects())
                    {
                        DontDestroyOnLoad(obj);
                        _uiManager = obj.GetComponentInChildren<UIManager>(true);
                        if (_uiManager != null) break;
                    }

                    Debug.Log("UI Scene Loaded");
                };
        }
        public void UpdateStormHUD(string msg, string hexColor)
        {
            _uiManager?.UpdateStormPhaseText(msg,hexColor);
        }
        
        public void StartGame()
        {
            SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single)
                .completed += (op) =>
                {
                    _uiManager?.ShowUI(false);
                    _uiManager?.ShowHouseValueHUD(true);
                    turnOffEV(false);
                    StartPreparePhase();
                    Debug.Log("Game Scene Loaded, UI hidden");
                };
        }
        public void OpenHUD(bool enable)
        {
            _uiManager?.ShowHouseValueHUD(enable);
        }
        public void RestartGame()
        {
            SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single)
                .completed += (op) =>
                {
                    _uiManager?.ShowUI(false);

                    currentHeldItem = null;
                    interactablesInSceneRuntime.Clear();
                    StartPreparePhase();

                    Debug.Log("Game restarted");
                };
        }
        public void UpdateHouseValue()
        {
            _uiManager?.UpdateHouseValueHUD();
        }
        
        public void turnOffEV(bool enabled)
        {
            _uiManager?.eventSystem.gameObject.SetActive(enabled);
        }
        public void ReturnToMainMenu()
        {
            var mouseLook = GameSceneManager.GameSceneManagerInstance.localPlayerChar.GetComponent<MouseLook>();
            if (mouseLook) mouseLook.SetMouseEnabled(true);
            SceneManager.LoadSceneAsync("SceneManager", LoadSceneMode.Single)
            .completed += (op) =>
            {
                if (_uiManager != null)
                {
                    _uiManager.ShowUI(true);
                    _uiManager.MainMenu.SetActive(true);
                    _uiManager.m_MainMenu.alpha = 1;
                    _uiManager.m_MainMenu.interactable = true;
                    _uiManager.m_MainMenu.blocksRaycasts = true;
                    _uiManager?.ShowHouseValueHUD(false);
                    _uiManager.Tutorial.SetActive(false);
                    _uiManager.Credits.SetActive(false);
                    _uiManager.Lobby.SetActive(false);
                    _uiManager.PauseMenu.SetActive(false);
                    turnOffEV(true);
                }

                ChangeState(new GameStartMenuState(this));
            };
        }
        public void ChangeState(GameStateBase newState)
        {
            _currentState?.OnExit();
            _currentState = newState;
            _currentState?.OnEnter();

            OnStateChanged?.Invoke(_currentState);
        }

        public void StartPreparePhase()
        {
            phaseStartTime = PhotonNetwork.InRoom ? PhotonNetwork.Time : 0.0;
            localPhaseStartTime = Time.time;
            ChangeState(new PreparePhaseState(this));
        }
        public void ForceCloseLobby()
        {
            _uiManager.Lobby.SetActive(false);
        }
        public void StartStormPhase()
        {
            if (CurrentStorm == null && stormDataAsset != null)
                CurrentStorm = new StormBase(stormDataAsset);
            phaseStartTime = PhotonNetwork.InRoom ? PhotonNetwork.Time : 0.0;
            localPhaseStartTime = Time.time;
            ChangeState(new StormPhaseState(this));
            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(StormTickCoroutine());
            }
            else if (!PhotonNetwork.InRoom)
            {
                StartCoroutine(StormTickCoroutine());
            }
        }

        private IEnumerator StormTickCoroutine()
        {
            CurrentStorm.StartStorm();

            while (CurrentStorm != null && !CurrentStorm.IsFinished)
            {
                CurrentStorm.Tick(Time.deltaTime);
                yield return null;
            }

            StartStormEndPhase();
        }
        public void StartStormEndPhase()
        {
            phaseStartTime = PhotonNetwork.InRoom ? PhotonNetwork.Time : 0.0;
            localPhaseStartTime = Time.time;
            ChangeState(new StormEndPhaseState(this));
        }
        public void SetHeldItem(InteractableBase item)
        {
            currentHeldItem = item;
            //Debug.Log("Player is holding: " + item.itemData.itemName);
        }
        public float GetPhaseElapsed()
        {
            if (PhotonNetwork.InRoom)
                return (float)(PhotonNetwork.Time - phaseStartTime);
            return (float)(Time.time - localPhaseStartTime);
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int min = Mathf.FloorToInt(seconds / 60f);
            int sec = Mathf.FloorToInt(seconds % 60f);
            return $"{min}:{sec:D2}";
        }
        public void ClearHeldItem()
        {
            Debug.Log("Player dropped item");
            currentHeldItem = null;
        }

        public bool IsHolding(InteractableItemType type)
        {
            if (currentHeldItem == null) return false;
            return currentHeldItem.itemData.itemType == type;
        }

        public void RegisterInteractableRuntime(InteractableBase interactable)
        {
            if (!interactable) return;

            if(interactablesInSceneRuntime.Contains(interactable)) return;

            interactablesInSceneRuntime.Add(interactable);
        }

        public void DeRegisterInteractableRuntime(InteractableBase interactable)
        {
            if (!interactable) return;

            if (!interactablesInSceneRuntime.Contains(interactable)) return;

            interactablesInSceneRuntime.Remove(interactable);
        }
    }
}
