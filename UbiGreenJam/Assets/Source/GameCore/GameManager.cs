using System;
using System.Collections.Generic;
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

        public void StartGame()
        {
            SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single)
                .completed += (op) =>
                {
                    _uiManager?.ShowUI(false);
                    StartPreparePhase();
                    Debug.Log("Game Scene Loaded, UI hidden");
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

            ChangeState(new StormPhaseState(this));
        }
        public void StartStormEndPhase()
        {
            ChangeState(new StormEndPhaseState(this));
        }
        public void SetHeldItem(InteractableBase item)
        {
            currentHeldItem = item;
            Debug.Log("Player is holding: " + item.itemData.itemName);
        }

        /*
        public void OpenPromf(string name, string promf, int code)
        {
            _uiManager?.Show(name, promf, code);
        }
        public void ClosePromf()
        {
            _uiManager?.Hide();
        }*/

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
