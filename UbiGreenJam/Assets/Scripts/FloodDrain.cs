using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FloodDrain : MonoBehaviour
{
    [Header("Refs")]
    public FloodController flood;       // drag FloodVolume here
    public Transform player;            // auto-found if empty
    public PlayerFloodDetector playerFlood; // auto-found if empty

    [Header("Drain Behavior")]
    [Tooltip("Drain only activates when water surface is above this Y (world).")]
    public float activateWaterY = 0.6f;

    [Tooltip("Max drainage rate when drain is clean (height units per second).")]
    public float maxDrainRate = 0.15f;

    [Tooltip("How fast clog increases per second while active (0..1 per sec).")]
    public float clogIncreaseRate = 0.02f;

    [Tooltip("How much clog is removed per unclog interaction.")]
    public float unclogAmount = 0.5f;

    [Header("Interaction")]
    [Tooltip("Player must be within this trigger to unclog.")]
    public bool requirePlayerInTrigger = true;

    [Tooltip("Key to unclog.")]
    public KeyCode unclogKey = KeyCode.E;

    [Header("Clog State (read only)")]
    [Range(0f, 1f)]
    public float clogLevel = 0f;     // 0 clean -> 1 fully clogged

    [Header("Visuals (Optional)")]
    [Tooltip("Renderer for visual feedback (color lerp).")]
    public Renderer clogRenderer;

    [Tooltip("Clean color.")]
    public Color cleanColor = new Color(0.2f, 0.9f, 0.25f, 1f);

    [Tooltip("Clogged color.")]
    public Color cloggedColor = new Color(0.9f, 0.25f, 0.2f, 1f);

    [Tooltip("Optional mesh that rises with clog (like gunk).")]
    public Transform clogFillMesh;

    [Tooltip("Local Y scale when fully clogged (0..1).")]
    public float fillMeshMaxScaleY = 1f;

    bool playerInside = false;

    void Start()
    {
        if (!flood) flood = FindAnyObjectByType<FloodController>();
        if (!playerFlood) playerFlood = FindAnyObjectByType<PlayerFloodDetector>();
        if (!player) player = Camera.main ? Camera.main.transform.root : null;

        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        UpdateVisuals();
    }

    void Update()
    {
        if (!flood) return;

        float waterY = flood.CurrentWaterSurfaceY();

        // Should drain be active?
        bool active = waterY >= activateWaterY && clogLevel < 1f;

        if (active)
        {
            // Drain rate scales down as clog grows.
            float drainMultiplier = Mathf.Lerp(1f, 0f, clogLevel);
            float drainThisFrame = maxDrainRate * drainMultiplier * Time.deltaTime;

            if (drainThisFrame > 0f)
                flood.RemoveWater(drainThisFrame);

            // Increase clog over time while draining
            clogLevel += clogIncreaseRate * Time.deltaTime;
            clogLevel = Mathf.Clamp01(clogLevel);

            UpdateVisuals();
        }

        // Player unclog input
        if (Input.GetKeyDown(unclogKey))
        {
            if (!requirePlayerInTrigger || playerInside)
                Unclog();
        }
    }

    public void Unclog()
    {
        if (clogLevel <= 0f) return;

        clogLevel = Mathf.Max(0f, clogLevel - unclogAmount);
        UpdateVisuals();
        // Optional feedback:
        // Debug.Log("Drain unclogged!");
    }

    void UpdateVisuals()
    {
        // Color lerp on renderer
        if (clogRenderer != null)
        {
            clogRenderer.material.color = Color.Lerp(cleanColor, cloggedColor, clogLevel);
        }

        // Fill mesh scale (gunk rising)
        if (clogFillMesh != null)
        {
            Vector3 s = clogFillMesh.localScale;
            s.y = Mathf.Lerp(0.05f, fillMeshMaxScaleY, clogLevel);
            clogFillMesh.localScale = s;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!requirePlayerInTrigger) return;
        if (other.GetComponent<CharacterController>() != null || other.CompareTag("Player"))
            playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!requirePlayerInTrigger) return;
        if (other.GetComponent<CharacterController>() != null || other.CompareTag("Player"))
            playerInside = false;
    }
}
