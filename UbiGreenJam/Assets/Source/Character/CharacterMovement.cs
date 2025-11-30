using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : CharacterComponentBase
{
    [Header("Components")]
    [SerializeField] public CharacterController characterController;
    private PlayerFloodDetector flood;
    private MouseLook mouseLook;   // for camera crouch

    [Header("General Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;   // slightly reduced from 6f
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 12f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float gravity = -16f;
    [SerializeField] private float jumpForce = 6f;

    private bool isWalking;

    [Header("Ground Check Settings")]
    [Tooltip("Which layers count as ground")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Radius of the ground check sphere")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [Tooltip("How far below feet to check")]
    [SerializeField] private float groundCheckDistance = 0.15f;
    [Tooltip("Optional debug log")]
    [SerializeField] private bool debugGrounded = false;

    private bool isGrounded;

    private Vector3 velocity;
    private Vector3 moveVelocity;

    // -------- WATER VALUES (Tweakable) --------
    [Header("Water Movement Settings")]
    [SerializeField] private float waterMinSpeedMultiplier = 0.45f;  // used for shallow wading
    [SerializeField] private float waterCurveExponent = 0.8f;
    [SerializeField] private float waterAirDrag = 0.25f;
    [SerializeField] private float submergedGravityMultiplier = 0.35f;
    [SerializeField] private Vector2 buoyancyJump = new Vector2(2.8f, 4.2f); // base magnitudes for water jumps

    [Header("Water Jump Anti-Spam")]
    [Tooltip("Seconds before another buoyancy jump is allowed.")]
    [SerializeField] private float underwaterJumpCooldown = 0.45f;

    [Tooltip("Prevents boosting again if already moving upward faster than this.")]
    [SerializeField] private float maxUpwardSpeedToAllowBoost = 1.0f;

    [Tooltip("Optional hard cap for upward speed in water to stop crazy launches.")]
    [SerializeField] private float maxUnderwaterUpSpeed = 4.5f;

    private float underwaterJumpTimer = 0f;

    private bool inWater = false;
    private float submergedAmount = 0f;

    [Header("Swimming / Buoyancy Settings")]
    [Tooltip("Submerged amount at which we switch to floating/swimming mode. (Belly-ish)")]
    [SerializeField] private float swimEnterSubmerge = 0.35f;

    [Tooltip("Submerged amount below which we drop back to wading mode (hysteresis).")]
    [SerializeField] private float swimExitSubmerge = 0.25f;

    [Tooltip("Multiplier to walk speed while swimming.")]
    [SerializeField] private float swimSpeedMultiplier = 1.15f;

    [Tooltip("How strongly we push the player toward their target height relative to the surface.")]
    [SerializeField] private float buoyancyStrength = 10f;   // was 8f – holds you closer to target

    [Tooltip("Desired camera/head offset ABOVE the water surface in meters.")]
    [SerializeField] private float neutralDepthFromSurface = 0.35f;  // was 0.16f – push camera clearly above

    [Tooltip("Damping factor for vertical speed while swimming (reduces bobbing).")]
    [SerializeField] private float swimDamping = 5f;  // slightly more damping to avoid visible bob

    [Tooltip("Optional override for which layers count as 'ceiling'. Leave 0 to reuse groundMask.")]
    [SerializeField] private LayerMask ceilingMask;

    private bool isSwimming = false;
    private float waterSurfaceY = float.NegativeInfinity;

    private float playSwimmingSoundTime = 2.5f;

    private float currentSwimmingTime = 0.0f;
    // ------------------------------------------

    // -------- CROUCH VALUES --------
    [Header("Crouch Settings")]
    [Tooltip("CharacterController height while crouched. (Must be >= 2 * radius!)")]
    [SerializeField] private float crouchHeight = 1.2f;   // radius is 0.5, so min legal is 1.0
    [Tooltip("Movement speed multiplier while crouched.")]
    [SerializeField] private float crouchSpeedMultiplier = 0.6f;

    [Space(5.0f)]

    [SerializeField] private KeyCode crouchKey = KeyCode.C;

    private bool isCrouching = false;
    private float originalHeight;
    private Vector3 originalCenter;
    // --------------------------------

    private void Awake()
    {
        if (!characterController)
            characterController = GetComponent<CharacterController>();

        mouseLook = GetComponent<MouseLook>();

        // Cache standing collider
        originalHeight = characterController.height;
        originalCenter = characterController.center;
    }

    private void Update()
    {
        // Photon ownership check
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            if (!TryGetComponent<Photon.Pun.PhotonView>(out var pv) || !pv.IsMine)
                return;
        }

        FloodCheck();

        // Swimming state based on submersion (with hysteresis)
        bool wasSwimming = isSwimming;

        if (inWater)
        {
            if (isSwimming)
            {
                if (submergedAmount < swimExitSubmerge)
                {
                    currentSwimmingTime = 0f;

                    isSwimming = false;
                }
            }
            else
            {
                if (!isSwimming && submergedAmount > swimEnterSubmerge)
                {
                    if (AudioManager.Instance) AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SwimmingSFX, transform.position);

                    isSwimming = true;
                }
            }
        }
        else
        {
            isSwimming = false;

            currentSwimmingTime = 0f;
        }

        // When we newly enter swimming (belly+ water), calm vertical velocity
        if (!wasSwimming && isSwimming)
        {
            velocity.y = 0f;
            underwaterJumpTimer = 0f;
        }

        if (isSwimming)
        {
            if(currentSwimmingTime < playSwimmingSoundTime)
            {
                currentSwimmingTime += Time.deltaTime;

                if(currentSwimmingTime >= playSwimmingSoundTime)
                {
                    currentSwimmingTime = 0f;

                    if (AudioManager.Instance) AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SwimmingSFX, transform.position);
                }
            }
        }

        // Timers
        if (underwaterJumpTimer > 0f)
            underwaterJumpTimer -= Time.deltaTime;

        GroundCheck();          // know if we're grounded this frame
        HandleCrouchInput();    // update isCrouching
        ApplyCrouchCapsule();   // snap collider to crouch/stand

        // -------- INPUT --------
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 inputDir = transform.right * input.x + transform.forward * input.y;
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        // -------- SPEED --------
        float targetSpeed = walkSpeed;

        if (isSwimming)
        {
            if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("Swimming", true);
        }
        else
        {
            if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("Swimming", false);

            if (Mathf.Approximately(input.x, 0.0f) && Mathf.Approximately(input.y, 0.0f))
            {
                if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("Walking", false);
            }
            else
            {
                if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("Walking", true);

                if (inWater)
                {
                    if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("SlowWalk", true);
                }
                else
                {
                    if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("SlowWalk", false);
                }
            }
        }

        if (inWater)
        {
            if (isSwimming)
            {
                // Swim: faster and not heavily slowed by water
                targetSpeed = walkSpeed * swimSpeedMultiplier;
            }
            else
            {
                // Shallow wading: gradually slow down as you get deeper, until swim threshold
                float t = Mathf.Clamp01(submergedAmount / Mathf.Max(swimEnterSubmerge, 0.0001f));
                float curve = Mathf.Pow(t, waterCurveExponent);
                targetSpeed *= Mathf.Lerp(1f, waterMinSpeedMultiplier, curve);
            }
        }

        // Crouch slowdown
        if (isCrouching)
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        // Accel / Decel smoothing
        if (inputDir.magnitude > 0.1f)
        {
            moveVelocity = Vector3.Lerp(
                moveVelocity,
                inputDir * targetSpeed,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            moveVelocity = Vector3.Lerp(
                moveVelocity,
                Vector3.zero,
                deceleration * Time.deltaTime
            );
        }

        // -------- JUMP / BUOYANCY JUMP --------
        if (Input.GetButtonDown("Jump") && !isCrouching)
        {
            if (inWater)
            {
                if (isSwimming)
                {
                    // Deep-ish water: consistent soft bob, regardless of exact depth
                    bool cooldownReady = underwaterJumpTimer <= 0f;
                    bool notAlreadyRocketingUp = velocity.y <= maxUpwardSpeedToAllowBoost;

                    if (cooldownReady && notAlreadyRocketingUp)
                    {
                        // Use a single, tuned swim jump value for all swimming depths
                        float swimJump = buoyancyJump.x; // ~2.8f with current values
                        velocity.y = swimJump;

                        if (velocity.y > maxUnderwaterUpSpeed)
                            velocity.y = maxUnderwaterUpSpeed;

                        underwaterJumpTimer = underwaterJumpCooldown;
                    }
                }
                else
                {
                    // Shallow water (knees/hips): smoother, slightly higher hop while grounded
                    if (isGrounded)
                    {
                        float shallowFactor = Mathf.Clamp01(submergedAmount * 1.5f);
                        float shallowJump = Mathf.Lerp(buoyancyJump.x, buoyancyJump.y, shallowFactor);
                        velocity.y = shallowJump;
                    }
                }
            }
            else if (isGrounded)
            {
                // Normal land jump
                velocity.y = jumpForce;
            }

            if (!isSwimming)
            {
                if (characterUsingComponent) characterUsingComponent.SetAnimatorTrigger("Jump");
            }
        }

        // -------- GRAVITY + BUOYANCY --------
        if (inWater)
        {
            if (isSwimming && waterSurfaceY > -9999f)
            {
                // Head/camera should stay just ABOVE the water surface (no underwater view).

                float radius = characterController.radius;
                float headY = transform.position.y
                              + characterController.center.y
                              + (characterController.height * 0.5f - radius);

                // Target: a small offset above the water surface
                float targetY = waterSurfaceY + neutralDepthFromSurface;

                // Error positive if we are BELOW desired height => accelerate up
                float error = targetY - headY;

                // Spring toward target height
                float buoyancyAccel = error * buoyancyStrength;

                // Damping against current vertical speed to avoid oscillation
                float damping = -velocity.y * swimDamping;

                float netAccel = buoyancyAccel + damping;
                velocity.y += netAccel * Time.deltaTime;
            }
            else
            {
                // Shallow water: softened gravity (still lets you fall off ledges etc.)
                float waterG = gravity * submergedGravityMultiplier;
                velocity.y += waterG * Time.deltaTime;
            }

            // Clamp upward speed in water
            if (velocity.y > maxUnderwaterUpSpeed)
                velocity.y = maxUnderwaterUpSpeed;
        }
        else
        {
            // Normal gravity on land
            velocity.y += gravity * Time.deltaTime;
        }

        // -------- AIR / SWIM CONTROL --------
        if (!isGrounded)
        {
            float control = airControl;

            if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("IsGrounded", false);

            // In water, especially when deeper, give more control so it feels like swimming
            if (inWater)
                control = Mathf.Lerp(airControl, 1f, submergedAmount);

            Vector3 airMove = inputDir * targetSpeed * control;

            moveVelocity = Vector3.Lerp(
                moveVelocity,
                airMove,
                (acceleration / 2f) * Time.deltaTime
            );

            if (inWater)
                moveVelocity *= (1f - waterAirDrag);
        }
        else
        {
            if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("IsGrounded", true);
        }

        // -------- FINAL MOVE --------
        Vector3 finalMovement = moveVelocity + Vector3.up * velocity.y;
        characterController.Move(finalMovement * Time.deltaTime);
    }

    // --------------------------------------------------------
    private void GroundCheck()
    {
        Vector3 feetPos = transform.position + characterController.center
                        - Vector3.up * (characterController.height * 0.5f);

        Vector3 checkPos = feetPos + Vector3.down * groundCheckDistance;

        bool sphereHit = Physics.CheckSphere(
            checkPos,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        isGrounded = sphereHit || characterController.isGrounded;

        // If we're in swim mode, we don't want to be "grounded" – we want the floaty behavior
        if (inWater && isSwimming)
        {
            isGrounded = false;
        }

        // Snap to ground only when truly grounded on land / shallow water
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (debugGrounded)
            Debug.Log($"Grounded? {isGrounded} | inWater={inWater} | isSwimming={isSwimming} | submerged={submergedAmount:F2}");
    }

    // --------------------------------------------------------
    private void FloodCheck()
    {
        if (!flood) flood = GetComponent<PlayerFloodDetector>();
        if (!flood)
        {
            inWater = false;
            submergedAmount = 0f;
            waterSurfaceY = float.NegativeInfinity;
            return;
        }

        inWater = flood.isInWater;
        submergedAmount = flood.submergedAmount;
        waterSurfaceY = flood.waterLevelY;   // surface height from FloodController
    }

    // --------------------------------------------------------
    // CROUCH LOGIC
    private void HandleCrouchInput()
    {
        bool crouchHeld = Input.GetKey(crouchKey);

        if (crouchHeld && isGrounded)
        {
            isCrouching = true;

            if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("Crouch", true);
        }
        else if (!crouchHeld)
        {
            if (CanStandUp())
            {
                isCrouching = false;

                if (characterUsingComponent) characterUsingComponent.SetAnimatorBool("Crouch", false);
            }
        }

        if (mouseLook != null)
            mouseLook.SetCrouchState(isCrouching);
    }

    private void ApplyCrouchCapsule()
    {
        float targetHeight = isCrouching ? crouchHeight : originalHeight;

        characterController.height = targetHeight;

        // keep feet in place
        float heightDiff = originalHeight - targetHeight;
        float targetCenterY = originalCenter.y - (heightDiff * 0.5f);

        Vector3 center = characterController.center;
        center.y = targetCenterY;
        characterController.center = center;
    }

    private bool CanStandUp()
    {
        float radius = characterController.radius * 0.95f;
        float halfHeight = originalHeight * 0.5f;

        Vector3 worldCenter = transform.position + originalCenter;
        Vector3 bottom = worldCenter + Vector3.down * (halfHeight - radius);
        Vector3 top = worldCenter + Vector3.up * (halfHeight - radius);

        // ignore our own layer
        int mask = ~(1 << gameObject.layer);

        return !Physics.CheckCapsule(bottom, top, radius, mask, QueryTriggerInteraction.Ignore);
    }

    // Kept in case you want to use it later (currently unused in buoyancy)
    private bool IsCeilingVeryClose()
    {
        if (!inWater) return false;

        float radius = characterController.radius * 0.95f;
        float halfHeight = characterController.height * 0.5f;

        // Approximate head position
        Vector3 worldCenter = transform.position + characterController.center;
        Vector3 headPos = worldCenter + Vector3.up * (halfHeight - radius);

        // Short sphere cast upward to see if we are about to bonk into something
        float checkDist = 0.35f;

        // If no specific ceilingMask set, reuse groundMask
        int mask = (ceilingMask.value == 0) ? groundMask : ceilingMask;

        return Physics.SphereCast(
            headPos,
            radius * 0.9f,
            Vector3.up,
            out _,
            checkDist,
            mask,
            QueryTriggerInteraction.Ignore
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!characterController) return;

        Vector3 feetPos = transform.position + characterController.center
                        - Vector3.up * (characterController.height * 0.5f);
        Vector3 checkPos = feetPos + Vector3.down * groundCheckDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
#endif
}
