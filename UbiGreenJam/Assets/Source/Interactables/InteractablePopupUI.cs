using TMPro;
using UnityEngine;

public class InteractablePopupUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas interactWorldUICanvas;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;   // <- add this text in your prefab
    public GameObject popup;

    /*
    [Header("Follow Settings")]
    public float followSmooth = 15f;
    public bool billboardToCamera = true;

    private Transform followTarget;
    private Vector3 followOffset;*/
    //private Camera cam;

    void Awake()
    {
        if (!interactWorldUICanvas)
        {
            TryGetComponent<Canvas>(out interactWorldUICanvas);
        }

        if(!interactWorldUICanvas) interactWorldUICanvas = gameObject.AddComponent<Canvas>();

        if(interactWorldUICanvas.renderMode != RenderMode.WorldSpace) interactWorldUICanvas.renderMode = RenderMode.WorldSpace;

        popup.SetActive(false);
    }

    /*
    void LateUpdate()
    {
        if (!popup.activeSelf || followTarget == null) return;

        // Smooth follow
        Vector3 targetPos = followTarget.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSmooth * Time.deltaTime);

        // Billboard
        if (billboardToCamera && cam != null)
        {
            Vector3 lookDir = transform.position - cam.transform.position;
            lookDir.y = 0f; // keep upright
            if (lookDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    public void SetFollowTarget(Transform target, Vector3 offset)
    {
        followTarget = target;
        followOffset = offset;
    }*/

    public void Show(string promptText, int cost)
    {
        if (this.promptText)
        {
            this.promptText.text = promptText;
        }

        if (!costText)
        {
            if (nameText)
            {
                costText = nameText;
            }
        }

        if(costText) costText.text = $"{cost}";

        popup.SetActive(true);

        /*
        // snap once on show so we don't lerp from old spot
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;*/
    }

    public void Hide()
    {
        popup.SetActive(false);
        //followTarget = null;
    }
}
