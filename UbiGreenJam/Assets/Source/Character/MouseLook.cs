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

    private Transform characterTransform;
    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;

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
        playerCam.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        playerCam.transform.localRotation = Quaternion.identity;

        // 5) Ensure THIS is the active MainCamera
        if (playerCam.CompareTag("MainCamera") == false)
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

        characterTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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