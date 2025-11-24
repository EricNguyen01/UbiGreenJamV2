using UnityEngine;
using UnityEngine.AI;

public class DogAnimationDriver : MonoBehaviour
{
    [Header("Refs (assign in Inspector)")]
    public NavMeshAgent agent;   // root agent
    public Animator animator;    // animator on rig

    [Header("Speed Mapping")]
    [Tooltip("World speed that should equal Speed=1 in blend tree. If agent exists we use agent.speed.")]
    public float maxWorldSpeed = 3.5f;

    [Header("Smoothing")]
    [Tooltip("Higher = snappier response.")]
    public float speedDamp = 10f;

    Vector3 lastPos;
    float smoothed01;
    int speedHash;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        speedHash = Animator.StringToHash("Speed");
        lastPos = transform.position;

        // If agent not assigned, try to find on parent (root)
        if (!agent) agent = GetComponentInParent<NavMeshAgent>();

        if (agent && maxWorldSpeed <= 0.01f)
            maxWorldSpeed = agent.speed;
    }

    void Update()
    {
        if (!animator) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) Try agent desired velocity
        float rawSpeed = 0f;
        bool agentValid = agent && agent.enabled && agent.isOnNavMesh;

        if (agentValid)
        {
            rawSpeed = agent.desiredVelocity.magnitude;

            // Sometimes desiredVelocity is 0 even while moving,
            // so we fallback if it's too small.
            if (rawSpeed < 0.02f)
                rawSpeed = agent.velocity.magnitude;
        }

        // 2) Fallback: real transform movement
        Vector3 delta = transform.position - lastPos;
        float transformSpeed = delta.magnitude / dt;
        lastPos = transform.position;

        if (!agentValid || rawSpeed < 0.02f)
            rawSpeed = transformSpeed;

        // Normalize to 0..1 for blend tree
        float denom = (agentValid ? agent.speed : maxWorldSpeed);
        if (denom < 0.01f) denom = 1f;

        float target01 = Mathf.Clamp01(rawSpeed / denom);

        // Smooth it
        smoothed01 = Mathf.Lerp(smoothed01, target01, 1f - Mathf.Exp(-speedDamp * dt));

        animator.SetFloat(speedHash, smoothed01);
    }
}
