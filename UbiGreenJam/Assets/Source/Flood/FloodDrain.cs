using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FloodDrain : InteractableBase
{
    [Header("Refs")]
    public FloodController flood;
    public Transform player;
    public PlayerFloodDetector playerFlood;

    [Header("Drain Behavior")]
    [Tooltip("If TRUE, activateWaterY is auto-set from this drain's world Y.")]
    public bool autoActivateFromPosition = true;

    [Tooltip("Activation height = drain Y + this offset. (e.g. 0.05 = activate when water slightly above drain)")]
    public float activateOffsetAboveDrain = 0.05f;

    [Tooltip("Drain only activates when water surface is above this Y (world). Used if autoActivateFromPosition = false.")]
    public float activateWaterY = 0.6f;

    [Tooltip("Max drainage rate when drain is clean (height units per second).")]
    public float maxDrainRate = 0.15f;

    [Tooltip("How fast clog increases per second while active (0..1 per sec).")]
    public float clogIncreaseRate = 0.02f;

    [Tooltip("How much clog is removed per unclog interaction.")]
    public float unclogAmount = 0.5f;

    [Header("Interaction")]
    public bool requirePlayerInTrigger = true;
    public KeyCode unclogKey = KeyCode.E;

    [Header("Clog State (read only)")]
    [Range(0f, 1f)]
    public float clogLevel = 0f;

    // ---------------- VISUALS -----------------
    [Header("Visuals - Color / Emission (Optional)")]
    public Renderer clogRenderer;

    public Color cleanColor = new Color(0.2f, 0.9f, 0.25f, 1f);
    public Color cloggedColor = new Color(0.9f, 0.25f, 0.2f, 1f);

    public bool useEmission = true;
    public float emissionAtClean = 0f;
    public float emissionAtClogged = 2f;
    public bool pulseWhenNearlyClogged = true;
    [Range(0.5f, 5f)] public float pulseSpeed = 2f;
    [Range(0.5f, 1f)] public float pulseStartAt = 0.8f;

    [Header("Visuals - Stage Meshes (Optional)")]
    public GameObject[] clogStageMeshes;
    public float[] stageThresholds = new float[] { 0.25f, 0.6f, 0.9f };

    [Header("Visuals - Fill Mesh (Optional)")]
    public Transform clogFillMesh;
    public float fillMeshMaxScaleXZ = 1f;
    public float fillMeshMinScaleXZ = 0.05f;
    public float fillMeshMaxPushZ = 0.08f;
    Vector3 fillMeshBaseLocalPos;
    Vector3 fillMeshBaseLocalScale;

    [Header("Visuals - Swirl While Draining (Optional)")]
    public Transform swirlVisual;
    public float swirlRotationSpeed = 180f;
    public bool scaleSwirlWithClog = true;
    public float swirlScaleClean = 1f;
    public float swirlScaleClogged = 0.4f;

    [Header("Visuals - Particles (Optional)")]
    public ParticleSystem drainParticles;
    public float particlesAtClean = 20f;
    public float particlesAtClogged = 3f;
    public ParticleSystem unclogBurstParticles;

    [Header("Visuals - Unclog Pop (Optional)")]
    public bool popOnUnclog = true;
    public float popScaleMultiplier = 1.1f;
    public float popDuration = 0.12f;

    // -----------------------------------------
    bool playerInside = false;
    MaterialPropertyBlock mpb;
    int currentStageIndex = -1;

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private float updateDrainPopupTextTime = 0.2f;

    private float currentDrainPopupTextUpdateTime = 0.0f;

    void Start()
    {
        if (!flood) flood = FindAnyObjectByType<FloodController>();
        if (!playerFlood) playerFlood = FindAnyObjectByType<PlayerFloodDetector>();
        if (!player) player = Camera.main ? Camera.main.transform.root : null;

        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (autoActivateFromPosition)
        {
            activateWaterY = transform.position.y + activateOffsetAboveDrain;
        }

        if (clogFillMesh)
        {
            fillMeshBaseLocalPos = clogFillMesh.localPosition;
            fillMeshBaseLocalScale = clogFillMesh.localScale;
        }

        mpb = new MaterialPropertyBlock();
        UpdateVisuals(activeDraining: false);
    }

    void Update()
    {
        if (!flood) return;

        float waterY = flood.CurrentWaterSurfaceY();
        bool active = waterY >= activateWaterY && clogLevel < 1f;

        if (active)
        {
            float drainMultiplier = Mathf.Lerp(1f, 0f, clogLevel);
            float drainThisFrame = maxDrainRate * drainMultiplier * Time.deltaTime;

            if (drainThisFrame > 0f)
                flood.RemoveWater(drainThisFrame);

            clogLevel += clogIncreaseRate * Time.deltaTime;
            clogLevel = Mathf.Clamp01(clogLevel);

            UpdateVisuals(activeDraining: true);
        }
        else
        {
            UpdateVisuals(activeDraining: false);
        }

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

        if (unclogBurstParticles)
            unclogBurstParticles.Play();

        //Commented out this if because it's causing drain scaling and UI bugs!!!
        /*if (popOnUnclog)
            StartCoroutine(PopRoutine());*/

        UpdateVisuals(activeDraining: false);
    }

    void UpdateVisuals(bool activeDraining)
    {
        UpdateStageMeshes();

        if (clogRenderer != null)
        {
            Color baseCol = Color.Lerp(cleanColor, cloggedColor, clogLevel);

            clogRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorID, baseCol);

            if (useEmission)
            {
                clogRenderer.material.EnableKeyword("_EMISSION");

                float emission = Mathf.Lerp(emissionAtClean, emissionAtClogged, clogLevel);

                if (pulseWhenNearlyClogged && clogLevel >= pulseStartAt)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
                    emission *= Mathf.Lerp(1f, 1.6f, pulse);
                }

                mpb.SetColor(EmissionColorID, baseCol * emission);
            }

            clogRenderer.SetPropertyBlock(mpb);
        }

        // Fill mesh grows uniformly in XZ and pushes forward slightly on Z
        if (clogFillMesh != null)
        {
            float xz = Mathf.Lerp(fillMeshMinScaleXZ, fillMeshMaxScaleXZ, clogLevel);

            Vector3 s = fillMeshBaseLocalScale;
            s.x = xz;
            s.z = xz;
            clogFillMesh.localScale = s;

            Vector3 lp = fillMeshBaseLocalPos;
            lp.z += Mathf.Lerp(0f, fillMeshMaxPushZ, clogLevel);
            clogFillMesh.localPosition = lp;
        }

        if (swirlVisual != null)
        {
            if (activeDraining)
            {
                float speed = swirlRotationSpeed * Mathf.Lerp(1f, 0.2f, clogLevel);
                swirlVisual.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);

                if (scaleSwirlWithClog)
                {
                    float sc = Mathf.Lerp(swirlScaleClean, swirlScaleClogged, clogLevel);
                    swirlVisual.localScale = Vector3.one * sc;
                }

                if (!swirlVisual.gameObject.activeSelf)
                    swirlVisual.gameObject.SetActive(true);
            }
            else
            {
                if (swirlVisual.gameObject.activeSelf)
                    swirlVisual.gameObject.SetActive(false);
            }
        }

        if (drainParticles != null)
        {
            var emission = drainParticles.emission;

            if (activeDraining && clogLevel < 1f)
            {
                float rate = Mathf.Lerp(particlesAtClean, particlesAtClogged, clogLevel);
                emission.rateOverTime = rate;

                if (!drainParticles.isPlaying) drainParticles.Play();
            }
            else
            {
                emission.rateOverTime = 0f;
                if (drainParticles.isPlaying) drainParticles.Stop();
            }
        }
    }

    void UpdateStageMeshes()
    {
        if (clogStageMeshes == null || clogStageMeshes.Length == 0) return;

        int stage = 0;
        for (int i = 0; i < stageThresholds.Length; i++)
        {
            if (clogLevel >= stageThresholds[i]) stage = i + 1;
        }

        stage = Mathf.Clamp(stage, 0, clogStageMeshes.Length - 1);
        if (stage == currentStageIndex) return;

        currentStageIndex = stage;

        for (int i = 0; i < clogStageMeshes.Length; i++)
        {
            if (clogStageMeshes[i])
                clogStageMeshes[i].SetActive(i == stage);
        }
    }

    IEnumerator PopRoutine()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 upScale = baseScale * popScaleMultiplier;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = t / popDuration;
            transform.localScale = Vector3.Lerp(baseScale, upScale, k);
            yield return null;
        }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = t / popDuration;
            transform.localScale = Vector3.Lerp(upScale, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    public override void ShowPrompt()
    {
        promptVisible = true;

        string unclogStr = $"{unclogKey} - Unclog";

        if (clogLevel <= 0.0f) unclogStr = "No Clog";

        if(currentDrainPopupTextUpdateTime > 0.0f && currentDrainPopupTextUpdateTime <= updateDrainPopupTextTime)
        {
            currentDrainPopupTextUpdateTime += Time.deltaTime;
        }

        if(currentDrainPopupTextUpdateTime <= 0.0f || currentDrainPopupTextUpdateTime > updateDrainPopupTextTime)
        {
            currentDrainPopupTextUpdateTime = 0.0f;

            if (popupUI) popupUI.Show(unclogStr, 0);

            currentDrainPopupTextUpdateTime += Time.deltaTime;
        }

    }

    public override void HidePrompt()
    {
        currentDrainPopupTextUpdateTime = 0.0f;

        base.HidePrompt();
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float y = autoActivateFromPosition
            ? transform.position.y + activateOffsetAboveDrain
            : activateWaterY;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.left * 0.3f + Vector3.up * (y - transform.position.y),
                        transform.position + Vector3.right * 0.3f + Vector3.up * (y - transform.position.y));
    }
#endif
}
