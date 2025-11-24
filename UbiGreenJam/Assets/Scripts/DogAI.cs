using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DogAI : MonoBehaviour
{
    public enum State { Roam, AvoidWater, Stuck, Held }

    [Header("Roaming")]
    public float roamRadius = 7f;
    public Vector2 waitRange = new Vector2(1.5f, 3.5f);

    [Header("Water Avoidance")]
    public FloodController flood;
    public float waterAvoidMargin = 0.15f;   // start running before feet touch water
    public float safeHeightMargin = 0.4f;    // must be above water by this much
    public List<Transform> safeSpots;        // optional higher areas

    [Header("Rescue / Danger")]
    public float panicChancePerSecond = 0.015f;  // chance to run somewhere risky during storm
    public float maxStuckTime = 8f;
    public bool stormActive = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip barkClip;
    public float barkInterval = 2.5f;

    [Header("Debug")]
    public State state = State.Roam;

    NavMeshAgent agent;
    float waitTimer;
    float stuckTimer;
    float barkTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!flood) flood = FindObjectOfType<FloodController>();
        PickNewRoamPoint();
    }

    void Update()
    {
        if (state == State.Held) return;

        EvaluateWater();

        switch (state)
        {
            case State.Roam:
                UpdateRoam();
                MaybePanic();
                break;

            case State.AvoidWater:
                UpdateAvoid();
                break;

            case State.Stuck:
                UpdateStuck();
                break;
        }
    }

    // ---------------- ROAM ----------------
    void UpdateRoam()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                PickNewRoamPoint();
        }
    }

    void PickNewRoamPoint()
    {
        Vector3 center = transform.position;
        Vector3 randomPos = center + Random.insideUnitSphere * roamRadius;
        randomPos.y = center.y;

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
        {
            if (IsSafe(hit.position))
            {
                agent.SetDestination(hit.position);
                waitTimer = Random.Range(waitRange.x, waitRange.y);
            }
            else
            {
                GoToSafeSpot();
            }
        }
    }

    // ---------------- WATER ----------------
    void EvaluateWater()
    {
        if (!flood) return;

        float waterY = flood.CurrentWaterSurfaceY();
        float feetY = transform.position.y;

        bool nearWater = feetY < waterY + waterAvoidMargin;

        if (nearWater && state != State.Stuck)
        {
            state = State.AvoidWater;
            GoToSafeSpot();
        }
        else if (!nearWater && state == State.AvoidWater)
        {
            state = State.Roam;
            PickNewRoamPoint();
        }
    }

    bool IsSafe(Vector3 pos)
    {
        if (!flood) return true;
        return pos.y > flood.CurrentWaterSurfaceY() + safeHeightMargin;
    }

    void GoToSafeSpot()
    {
        // Prefer manual safe spots if provided
        if (safeSpots != null && safeSpots.Count > 0)
        {
            Transform best = null;
            float bestScore = float.MinValue;

            foreach (var s in safeSpots)
            {
                if (!s) continue;
                float score = s.position.y - Vector3.Distance(transform.position, s.position) * 0.2f;
                if (IsSafe(s.position) && score > bestScore)
                {
                    best = s; bestScore = score;
                }
            }

            if (best)
            {
                agent.SetDestination(best.position);
                return;
            }
        }

        // fallback search upward biased
        for (int i = 0; i < 10; i++)
        {
            Vector3 candidate = transform.position + Random.insideUnitSphere * roamRadius;
            candidate.y += Random.Range(0.2f, 1.2f);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                if (IsSafe(hit.position))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        agent.ResetPath();
    }

    // ---------------- PANIC / STUCK ----------------
    void MaybePanic()
    {
        if (!stormActive || state != State.Roam) return;

        float chance = panicChancePerSecond * Time.deltaTime;
        if (Random.value < chance)
        {
            // dog runs to random point without safety check
            Vector3 risky = transform.position + Random.insideUnitSphere * roamRadius;
            risky.y = transform.position.y;

            if (NavMesh.SamplePosition(risky, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                Invoke(nameof(CheckIfStuckInWater), 3f);
            }
        }
    }

    void CheckIfStuckInWater()
    {
        if (!flood) return;
        float waterY = flood.CurrentWaterSurfaceY();
        if (transform.position.y < waterY + 0.05f)
        {
            state = State.Stuck;
            agent.ResetPath();
            stuckTimer = 0f;
        }
    }

    void UpdateAvoid()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (IsSafe(transform.position))
            {
                state = State.Roam;
                PickNewRoamPoint();
            }
        }
    }

    void UpdateStuck()
    {
        stuckTimer += Time.deltaTime;
        barkTimer -= Time.deltaTime;

        if (barkTimer <= 0f)
        {
            Bark();
            barkTimer = barkInterval;
        }

        if (stuckTimer > maxStuckTime)
        {
            state = State.AvoidWater;
            GoToSafeSpot();
        }
    }

    void Bark()
    {
        if (audioSource && barkClip)
            audioSource.PlayOneShot(barkClip);
    }

    // ---------------- PICKUP HOOK ----------------
    public void SetHeld(bool held)
    {
        if (held)
        {
            state = State.Held;
            agent.enabled = false;
        }
        else
        {
            agent.enabled = true;
            state = State.Roam;
            PickNewRoamPoint();
        }
    }

    public bool IsInDanger() => state == State.Stuck;
}
