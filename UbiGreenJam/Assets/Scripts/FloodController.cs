using UnityEngine;

public class FloodController : MonoBehaviour
{
    [Header("Flood Settings")]
    [Tooltip("Maximum flood height above the baseY.")]
    public float maxHeight = 3f;

    [Tooltip("Units per second that flood rises.")]
    public float riseSpeed = 0.5f;

    [Tooltip("Units per second that flood lowers.")]
    public float lowerSpeed = 0.5f;

    [Tooltip("Reference to player flood detector.")]
    public PlayerFloodDetector playerFlood;

    [Header("Visual Water (Optional)")]
    [Tooltip("Prefab/plane that visually represents the water surface.")]
    public Transform waterVisual;

    [Tooltip("Offset above the calculated water plane.")]
    public float visualOffset = 0.05f;

    [Header("Debug")]
    public bool startRisingOnPlay = false;

    // Internal
    private float baseY;
    private float currentHeight = 0f;
    private bool isRising = false;
    private bool isLowering = false;


    // --------------------------------------------------------
    void Start()
    {
        if (!playerFlood)
            playerFlood = FindAnyObjectByType<PlayerFloodDetector>();

        baseY = transform.position.y;

        isRising = startRisingOnPlay;

        UpdateScaleAndPosition();
        UpdateVisualWater();
    }


    // --------------------------------------------------------
    void Update()
    {
        float dt = Time.deltaTime;

        // Update player detector
        if (playerFlood)
            playerFlood.waterLevelY = CurrentWaterSurfaceY();

        // RISE
        if (isRising)
        {
            currentHeight += riseSpeed * dt;
            if (currentHeight >= maxHeight)
            {
                currentHeight = maxHeight;
                isRising = false;
            }

            UpdateScaleAndPosition();
            UpdateVisualWater();
        }
        // LOWER
        else if (isLowering)
        {
            currentHeight -= lowerSpeed * dt;
            if (currentHeight <= 0f)
            {
                currentHeight = 0f;
                isLowering = false;
            }

            UpdateScaleAndPosition();
            UpdateVisualWater();
        }
    }


    // --------------------------------------------------------
    void UpdateScaleAndPosition()
    {
        // Height must never hit 0 (Unity breaks scaling)
        float height = Mathf.Max(currentHeight, 0.01f);

        // Scale cube
        Vector3 scale = transform.localScale;
        scale.y = height;
        transform.localScale = scale;

        // Position cube so bottom stays fixed at baseY
        transform.position = new Vector3(
            transform.position.x,
            baseY + height * 0.5f,
            transform.position.z
        );
    }


    // --------------------------------------------------------
    void UpdateVisualWater()
    {
        if (!waterVisual) return;

        Vector3 p = waterVisual.position;
        p.y = CurrentWaterSurfaceY() + visualOffset;
        waterVisual.position = p;
    }


    // --------------------------------------------------------
    // PUBLIC API (for drains, storm manager, scripts)
    // --------------------------------------------------------

    /// <summary>
    /// Add flood water (start rising).
    /// </summary>
    public void StartFlood(float speedMultiplier = 1f)
    {
        isLowering = false;
        isRising = true;
        // Optionally apply multiplier externally
        // riseSpeed *= speedMultiplier;
    }

    /// <summary>
    /// Stop rising but keep level as is.
    /// </summary>
    public void StopFlood()
    {
        isRising = false;
    }

    /// <summary>
    /// Begin actively lowering water (drain or drying).
    /// </summary>
    public void StartLowering()
    {
        isRising = false;
        isLowering = true;
    }

    /// <summary>
    /// Used by drains — removes water instantly.
    /// </summary>
    public void RemoveWater(float amount)
    {
        currentHeight = Mathf.Max(0f, currentHeight - amount);
        UpdateScaleAndPosition();
        UpdateVisualWater();
    }

    /// <summary>
    /// 0 to 1 normalized flood percentage.
    /// </summary>
    public float GetNormalizedFloodLevel()
    {
        return Mathf.InverseLerp(0f, maxHeight, currentHeight);
    }

    /// <summary>
    /// Returns current water surface height in world coordinates.
    /// </summary>
    public float CurrentWaterSurfaceY()
    {
        return baseY + currentHeight;
    }
}
