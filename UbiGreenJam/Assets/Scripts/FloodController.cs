using UnityEngine;
using UnityEngine.Events;

public class FloodController : MonoBehaviour
{
    [Header("Flood Settings")]
    [Tooltip("Maximum flood height above the baseY.")]
    public float maxHeight = 3f;

    [Tooltip("Base rising speed if intensity = 1. Final speed = this * intensityMultiplier.")]
    public float baseRiseSpeed = 0.5f;

    [Tooltip("Units per second that flood lowers.")]
    public float lowerSpeed = 0.5f;

    [Tooltip("Reference to player flood detector.")]
    public PlayerFloodDetector playerFlood;


    // -------------------------------------------------------
    // NEW — FLOOD INTENSITY
    // -------------------------------------------------------
    [Header("Flood Intensity")]
    [Tooltip("Intensity values for RAIN LEVELS. 0 = Light, 1 = Medium, 2 = Heavy.")]
    public float[] intensityMultipliers = new float[] { 0.5f, 1f, 1.8f };

    [Tooltip("Current flood intensity index (0 = light, 1 = medium, 2 = heavy).")]
    [Range(0, 2)]
    public int currentIntensity = 0;

    [Tooltip("Smooth transition speed when switching intensities.")]
    public float intensityLerpSpeed = 2f;

    private float currentRiseSpeed;     // final rise speed after lerp
    private float targetRiseSpeed;      // the speed we lerp toward


    // -------------------------------------------------------
    [Header("Visual Water (Optional)")]
    public Transform waterVisual;
    public float visualOffset = 0.05f;

    [Header("Debug")]
    public bool startRisingOnPlay = false;


    // -------------------------------------------------------
    // GAME OVER EVENT
    // -------------------------------------------------------
    [Header("Game Over Event")]
    public UnityEvent onFloodMaxed;   // called once when water reaches max

    private bool gameOverTriggered = false;


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

        // Set initial rise speed
        currentRiseSpeed = baseRiseSpeed * intensityMultipliers[currentIntensity];
        targetRiseSpeed = currentRiseSpeed;

        isRising = startRisingOnPlay;

        UpdateScaleAndPosition();
        UpdateVisualWater();
    }


    // --------------------------------------------------------
    void Update()
    {
        float dt = Time.deltaTime;

        // Player detector
        if (playerFlood)
            playerFlood.waterLevelY = CurrentWaterSurfaceY();


        // --------------------------------------------
        // Smooth intensity transition (Lerp)
        targetRiseSpeed = baseRiseSpeed * intensityMultipliers[currentIntensity];
        currentRiseSpeed = Mathf.Lerp(currentRiseSpeed, targetRiseSpeed, dt * intensityLerpSpeed);
        // --------------------------------------------


        // RISE
        if (isRising)
        {
            currentHeight += currentRiseSpeed * dt;

            if (currentHeight >= maxHeight)
            {
                currentHeight = maxHeight;
                isRising = false;
                TriggerGameOver();
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
    void TriggerGameOver()
    {
        if (gameOverTriggered) return;

        gameOverTriggered = true;

        Debug.Log("⚠ GAME OVER – Flood reached max height.");

        if (onFloodMaxed != null)
            onFloodMaxed.Invoke();
    }


    // --------------------------------------------------------
    void UpdateScaleAndPosition()
    {
        float height = Mathf.Max(currentHeight, 0.01f);

        Vector3 scale = transform.localScale;
        scale.y = height;
        transform.localScale = scale;

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
    // PUBLIC API
    // --------------------------------------------------------

    public void StartFlood()
    {
        isLowering = false;
        isRising = true;
    }

    public void StopFlood()
    {
        isRising = false;
    }

    public void StartLowering()
    {
        isRising = false;
        isLowering = true;
    }

    public void RemoveWater(float amount)
    {
        currentHeight = Mathf.Max(0f, currentHeight - amount);
        UpdateScaleAndPosition();
        UpdateVisualWater();
    }

    public float GetNormalizedFloodLevel()
    {
        return Mathf.InverseLerp(0f, maxHeight, currentHeight);
    }

    public float CurrentWaterSurfaceY()
    {
        return baseY + currentHeight;
    }


    // --------------------------------------------------------
    // NEW API — SWITCH INTENSITY
    // --------------------------------------------------------
    public void SetFloodIntensity(int index)
    {
        currentIntensity = Mathf.Clamp(index, 0, intensityMultipliers.Length - 1);
        Debug.Log($"Rain intensity set to {currentIntensity} ({intensityMultipliers[currentIntensity]}x)");
    }
}
