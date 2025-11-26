using GameCore;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractableBase : MonoBehaviour
{
    public InteractableItemData itemData;

    private float itemMaxHealth = 100.0f;

    private float itemCurrentHealth = 100.0f;

    public bool isPendingDestroy { get; private set; } = false;

    public bool destroyOnPickup = false;

    [Header("Runtime State")]

    [ReadOnlyInspector]
    public bool allowPickUp { get; protected set; } = true;

    [ReadOnlyInspector]
    public bool damageable { get; protected set; } = true;

    [ReadOnlyInspector]
    public bool isBeingHeld = false;

    [Header("UI")]

    public InteractablePopupUI popupUIPrefabToSpawn;

    public InteractablePopupUI popupUI;

    [Tooltip("World-space offset for popup above the object")]
    public Vector3 popupOffset = new Vector3(0f, 0.6f, 0f);

    public bool promptVisible { get; protected set; } = false;

    [Header("Destroy FX")]

    public ParticleSystem destroyFXPrefabToSpawn;

    [Header("Prompt override (runtime)")]
    public string lockedPromptMessage = "";
    public int lockedPromptCost = 0;
    private float promptLockUntil = 0f;

    public DogAI dogAI { get; private set; }

    public FurnitureRequiredComponentsSetup furnitureColliderRigidbodyData { get; private set; }

    private void Awake()
    {
        if (!popupUI)
        {
            popupUI = GetComponentInChildren<InteractablePopupUI>();
        }

        if (popupUI) popupUI.transform.position += popupOffset;

        dogAI = GetComponent<DogAI>();

        if (!itemData)
        {
            Debug.LogError($"Interactable: {name} is missing its item data scriptable object. Disabling interactable game object...");

            enabled = false;

            gameObject.SetActive(false);

            return;
        }

        if (itemData.useCostAsHealth) itemMaxHealth = itemData.cost;

        else itemMaxHealth = itemData.health;

        itemCurrentHealth = itemMaxHealth;

        allowPickUp = itemData.isCarryable;

        damageable = itemData.isDamageable;
    }

    private void OnEnable()
    {
        if(GameManager.Instance) GameManager.Instance.RegisterInteractableRuntime(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance) GameManager.Instance.DeRegisterInteractableRuntime(this);
    }

    private void Start()
    {
        furnitureColliderRigidbodyData = GetComponent<FurnitureRequiredComponentsSetup>();
    }

#if UNITY_EDITOR
    [ContextMenu("InstantiatePopupUI_Editor")]
    private void InstantiatePopupUI_Editor()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            if (!popupUIPrefabToSpawn) return;

            foreach(InteractablePopupUI childPopupUI in GetComponentsInChildren<InteractablePopupUI>())
            {
                if (childPopupUI)
                {
                    DestroyImmediate(childPopupUI.gameObject);
                }
            }

            GameObject popupUIGO = Instantiate(popupUIPrefabToSpawn.gameObject);

            MeshRenderer meshRend = GetComponent<MeshRenderer>();

            if(!meshRend) meshRend = GetComponentInChildren<MeshRenderer>();

            if (meshRend)
            {
                popupUIGO.transform.SetParent(meshRend.transform);
            }
            else
            {
                popupUIGO.transform.SetParent(transform);
            }

            popupUIGO.transform.localPosition = new Vector3(0.0f, 
                                                            popupUIPrefabToSpawn.transform.localPosition.y, 
                                                            popupUIPrefabToSpawn.transform.localPosition.z);

            popupUIGO.transform.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }
#endif

    public virtual void ShowPrompt()
    {
        if (isBeingHeld || itemData == null) return;

        if (!string.IsNullOrEmpty(lockedPromptMessage) && Time.time < promptLockUntil)
        {
            //GameManager.Instance.OpenPromf(itemData.itemName, lockedPromptMessage, lockedPromptCost);

            if (popupUI) popupUI.Show(lockedPromptMessage, lockedPromptCost);

            return;
        }

        if (Time.time >= promptLockUntil)
        {
            lockedPromptMessage = "";
            lockedPromptCost = 0;
            promptLockUntil = 0f;
        }

        string name = itemData.itemName;

        string prompt = itemData.isCarryable ? "Pick Up" : string.Empty;

        int cost = itemData.cost;

        promptVisible = true;

        //GameManager.Instance.OpenPromf(name, prompt, cost);

        if (popupUI) popupUI.Show(prompt, cost);
    }

    public void ShowTemporaryMessage(string message, int cost = 0, float duration = 1.5f)
    {
        if (string.IsNullOrEmpty(message)) return;
        lockedPromptMessage = message;
        lockedPromptCost = cost;
        promptLockUntil = Time.time + Mathf.Max(0.1f, duration);
        //if(GameManager.Instance) GameManager.Instance.OpenPromf(gameObject.name, lockedPromptMessage, lockedPromptCost);
    }

    public virtual void HidePrompt()
    {
        promptVisible = false;
        //if(GameManager.Instance) GameManager.Instance.ClosePromf();

        if(popupUI) popupUI.Hide();
    }

    public void DeductInteractableHealth(float healthToDeduct)
    {
        if (!itemData) return;

        if (!itemData.isDamageable || !damageable) return;

        if(healthToDeduct < 0.0f) healthToDeduct = 0.0f;

        itemCurrentHealth -= healthToDeduct * (itemData ? itemData.floodDamageMitigation : 1.0f);

        if(itemCurrentHealth <= 0.0f)
        {
            if(!isPendingDestroy) DestroyInteractable();
        }
    }

    private void DestroyInteractable()
    {
        StopAllCoroutines();

        isPendingDestroy = true;

        if (destroyFXPrefabToSpawn)
        {
            if (furnitureColliderRigidbodyData && furnitureColliderRigidbodyData.meshRend)
            {
                MeshRenderer meshRend = furnitureColliderRigidbodyData.meshRend;

                Instantiate(destroyFXPrefabToSpawn.gameObject, meshRend.bounds.center, Quaternion.identity);
            }
            else
            {
                Instantiate(destroyFXPrefabToSpawn.gameObject, transform.position, Quaternion.identity);
            }
        }

        StartCoroutine(DestroyInteractableCoroutineDelay());
    }

    private IEnumerator DestroyInteractableCoroutineDelay(float delay = 0.0f)
    {
        if(delay < 0.0f) delay = 0.0f;

        if(delay > 1.0f) delay = 1.0f;

        isPendingDestroy = true;

        if (popupUI) popupUI.Hide();

        EnableInteractableOutline(false);

        if (GameManager.Instance) GameManager.Instance.DeRegisterInteractableRuntime(this);

        if (furnitureColliderRigidbodyData)
        {
            furnitureColliderRigidbodyData.DisableFurnitureColliders(true);

            if(delay > 0.0f) yield return new WaitForSeconds(delay);

            Destroy(furnitureColliderRigidbodyData.meshRend.gameObject);

            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, delay);
        }
    }

    public void EnableInteractableOutline(bool enabled)
    {
        if (!furnitureColliderRigidbodyData) return;

        furnitureColliderRigidbodyData.EnableOutline(enabled);
    }
}
