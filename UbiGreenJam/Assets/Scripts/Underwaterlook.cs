using UnityEngine;
using UnityEngine.UI;

public class UnderwaterEffects : MonoBehaviour
{
    [Header("Refs")]
    public FloodController flood;           // FloodVolume controller
    public Transform cam;                   // FPS camera
    public Image underwaterOverlay;         // UI full-screen overlay
    public PlayerFloodDetector playerFlood; // detector on player

    [Header("Surface Thresholds (for true underwater)")]
    public float enterOffset = 0.08f;       // below surface to ENTER underwater
    public float exitOffset = 0.15f;        // above surface to EXIT underwater

    [Header("Partial Submerge Tint")]
    [Tooltip("At what submergedAmount (0-1) the overlay starts appearing.")]
    public float partialStart = 0.65f;      // legs deep
    [Tooltip("At what submergedAmount (0-1) the overlay reaches full strength BEFORE true underwater.")]
    public float partialFull = 0.9f;       // almost head deep
    [Tooltip("Max alpha used for partial submerge (before camera underwater).")]
    public float partialMaxAlpha = 0.45f;   // mild tint while waist/chest deep

    [Header("Fade Speeds")]
    public float overlayFadeSpeed = 4f;     // higher = snappier
    public float fogFadeSpeed = 2f;

    [Header("Fog Settings")]
    public bool enableFog = true;
    public Color underwaterFogColor = new Color(0.1f, 0.35f, 0.4f, 1f);
    public float underwaterFogDensity = 0.05f;

    bool underwater = false;

    // Defaults
    Color defaultFogColor;
    float defaultFogDensity;
    bool defaultFogEnabled;

    float overlayAlpha = 0f;
    float fogBlend = 0f; // 0 = normal, 1 = underwater

    void Start()
    {
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            if (!TryGetComponent<Photon.Pun.PhotonView>(out var pv) || !pv.IsMine)
            {
                if (underwaterOverlay) underwaterOverlay.gameObject.SetActive(false);
                return;
            }
        }
        if (!flood) flood = FindAnyObjectByType<FloodController>();
        if (!cam) cam = Camera.main.transform;
        if (!playerFlood) playerFlood = FindAnyObjectByType<PlayerFloodDetector>();

        defaultFogEnabled = RenderSettings.fog;
        defaultFogColor = RenderSettings.fogColor;
        defaultFogDensity = RenderSettings.fogDensity;

        if (underwaterOverlay)
        {
            // Keep it enabled; just fade alpha.
            underwaterOverlay.gameObject.SetActive(true);
            Color c = underwaterOverlay.color;
            c.a = 0f;
            underwaterOverlay.color = c;
        }
    }

    void Update()
    {
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            if (!TryGetComponent<Photon.Pun.PhotonView>(out var pv) || !pv.IsMine)
                return; 
        }
        if (!flood || !cam || !playerFlood) return;

        float waterY = flood.CurrentWaterSurfaceY();
        float camY = cam.position.y;

        // -----------------------------
        // 1) TRUE underwater state (camera-based)
        // -----------------------------
        if (!underwater && camY < waterY - enterOffset)
            underwater = true;

        if (underwater && camY > waterY + exitOffset)
            underwater = false;

        // -----------------------------
        // 2) PARTIAL submerge intensity (body-based)
        // -----------------------------
        float sub = playerFlood.submergedAmount; // 0-1 from feet to head

        // remap partial range
        float partialT = Mathf.InverseLerp(partialStart, partialFull, sub);
        partialT = Mathf.Clamp01(partialT);

        float targetPartialAlpha = partialT * partialMaxAlpha;

        // If camera is fully underwater, we force strong overlay
        float targetAlpha = underwater ? 1f : targetPartialAlpha;

        // Smooth fade
        overlayAlpha = Mathf.MoveTowards(
            overlayAlpha,
            targetAlpha,
            overlayFadeSpeed * Time.deltaTime
        );

        if (underwaterOverlay)
        {
            Color c = underwaterOverlay.color;
            c.a = overlayAlpha;
            underwaterOverlay.color = c;
        }

        // -----------------------------
        // 3) Fog blending
        // -----------------------------
        if (enableFog)
        {
            float targetFog = underwater ? 1f : 0f;
            fogBlend = Mathf.MoveTowards(fogBlend, targetFog, fogFadeSpeed * Time.deltaTime);

            if (fogBlend > 0.001f)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(defaultFogColor, underwaterFogColor, fogBlend);
                RenderSettings.fogDensity = Mathf.Lerp(defaultFogDensity, underwaterFogDensity, fogBlend);
            }
            else
            {
                RenderSettings.fog = defaultFogEnabled;
                RenderSettings.fogColor = defaultFogColor;
                RenderSettings.fogDensity = defaultFogDensity;
            }
        }
    }
}
