using GameCore;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractableBase : MonoBehaviour
{
    public InteractableItemData itemData;
    public bool destroyOnPickup = false;

    [Header("Runtime State")]
    public bool isBeingHeld = false;

    [Header("UI")]

    public InteractablePopupUI popupUIPrefabToSpawn;

    public InteractablePopupUI popupUI;

    [Tooltip("World-space offset for popup above the object")]
    public Vector3 popupOffset = new Vector3(0f, 0.6f, 0f);

    public bool promptVisible { get; private set; } = false;
    [Header("Prompt override (runtime)")]
    public string lockedPromptMessage = "";
    public int lockedPromptCost = 0;
    private float promptLockUntil = 0f;

    public DogAI dogAI { get; private set; }

    public FurnitureColliderRigidbodySetup furnitureColliderRigidbodyData { get; private set; }

    public void ShowTemporaryMessage(string message, int cost = 0, float duration = 1.5f)
    {
        if (string.IsNullOrEmpty(message)) return;
        lockedPromptMessage = message;
        lockedPromptCost = cost;
        promptLockUntil = Time.time + Mathf.Max(0.1f, duration);
        //if(GameManager.Instance) GameManager.Instance.OpenPromf(gameObject.name, lockedPromptMessage, lockedPromptCost);
    }

    private void Awake()
    {
        if (!popupUI)
        {
            popupUI = GetComponentInChildren<InteractablePopupUI>();
        }

        if (popupUI) popupUI.transform.position += popupOffset;

        dogAI = GetComponent<DogAI>();
    }

    private void Start()
    {
        furnitureColliderRigidbodyData = GetComponent<FurnitureColliderRigidbodySetup>();
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

            MeshRenderer meshRend = GetComponentInChildren<MeshRenderer>();

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

    public void ShowPrompt()
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

    public void HidePrompt()
    {
        promptVisible = false;
        //if(GameManager.Instance) GameManager.Instance.ClosePromf();

        if(popupUI) popupUI.Hide();
    }
}
