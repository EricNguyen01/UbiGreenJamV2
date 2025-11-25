using UnityEngine;

[DisallowMultipleComponent]
public class MouseLook : CharacterComponentBase
{
    [Header("Mouse Look Components")]
    [SerializeField] public Camera playerCam { get; private set; }

    [Header("Sensitivity")]
    [SerializeField] private float horizontalSensitivity = 450f;
    [SerializeField] private float verticalSensitivity = 450f;
    [SerializeField] private float sensitivityMultiplier = 1f;

    [Header("Limits")]
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Smoothing")]
    [SerializeField] private bool useSmoothing = false;
    [SerializeField] private float smoothTime = 0.03f;

    [Header("Camera Handling")]
    [SerializeField] private bool forceUsePlayerChildCamera = true;
    [SerializeField] private bool disableOtherMainCameras = true;

    [Header("Crouch Camera")]
    [Tooltip("Camera local Y when standing.")]
    [SerializeField] private float standCamY = 1.0f;
    [Tooltip("Camera local Y when crouched (visual only).")]
    [SerializeField] private float crouchCamY = 0.6f;
    [Tooltip("How fast the camera moves between stand/crouch.")]
    [SerializeField] private float camCrouchSmooth = 10f;

    private Transform characterTransform;
    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;

    private bool isCrouching = false;

    protected override void Start()
    {
        base.Start();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResolveCamera();

        yaw = transform.eulerAngles.y;
        pitch = playerCam.transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
    }

    private void ResolveCamera()
    {
        // 1) Prefer a camera already under/inside the player
        if (playerCam == null && forceUsePlayerChildCamera)
        {
            playerCam = GetComponentInChildren<Camera>(true);
        }

        // 2) If still null, use Camera.main but verify it isn't some menu cam
        if (playerCam == null)
        {
            playerCam = Camera.main;
        }

        // 3) If STILL null, create one
        if (playerCam == null)
        {
            playerCam = new GameObject("PlayerCam").AddComponent<Camera>();
        }

        // 4) Make sure it's parented to player and positioned correctly
        playerCam.transform.SetParent(transform);
        Vector3 startPos = playerCam.transform.localPosition;
        startPos.x = 0f;
        startPos.z = 0f;
        startPos.y = standCamY;
        playerCam.transform.localPosition = startPos;
        playerCam.transform.localRotation = Quaternion.identity;

        // 5) Ensure THIS is the active MainCamera
        if (!playerCam.CompareTag("MainCamera"))
            playerCam.tag = "MainCamera";

        // Optional: disable other main cameras that might persist from menu
        if (disableOtherMainCameras)
        {
            Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCams)
            {
                if (cam != playerCam && cam.CompareTag("MainCamera"))
                {
                    cam.tag = "Untagged";
                    cam.gameObject.SetActive(false);
                }
            }
        }
    }

    private void Update()
    {
        if (!enabled || playerCam == null || characterTransform == null) return;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");

        float targetYaw = yaw + mx * horizontalSensitivity * sensitivityMultiplier * 0.002f;
        float targetPitch = pitch - my * verticalSensitivity * sensitivityMultiplier * 0.002f;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        if (useSmoothing)
        {
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, smoothTime);
            pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVelocity, smoothTime);
        }
        else
        {
            yaw = targetYaw;
            pitch = targetPitch;
        }

        // Rotate player + camera
        characterTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Smooth camera vertical offset for crouch
        float targetY = isCrouching ? crouchCamY : standCamY;
        Vector3 camLocal = playerCam.transform.localPosition;
        camLocal.y = Mathf.Lerp(camLocal.y, targetY, camCrouchSmooth * Time.deltaTime);
        playerCam.transform.localPosition = camLocal;
    }

    public void SetCrouchState(bool crouching)
    {
        isCrouching = crouching;
    }

    public override bool InitCharacterComponentFrom(CharacterBase character)
    {
        if (!base.InitCharacterComponentFrom(character))
            return false;

        characterTransform = character.transform;

        horizontalSensitivity = character.characterSOData.mouseHorizontalSensitivity;
        verticalSensitivity = character.characterSOData.mouseVerticalSensitivity;

        return true;
    }
}
