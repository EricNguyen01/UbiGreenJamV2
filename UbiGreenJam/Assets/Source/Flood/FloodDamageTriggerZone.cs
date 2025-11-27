using GameCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FloodDamageTriggerZone : MonoBehaviour
{
    private FloodController floodParent;

    private Collider triggerCollider;

    private List<InteractableDamageReceiver> alreadyDamagedInteractables = new List<InteractableDamageReceiver>();

    private bool isProcessingDamageTick = false;

    private void Start()
    {
        if (!triggerCollider)
        {
            foreach(Collider col in GetComponentsInParent<Collider>())
            {
                if(col.enabled && col.isTrigger)
                {
                    triggerCollider = col;

                    break;
                }
            }
        }
    }

    public void InitFloodDamageComponent(FloodController floodParent, Collider floodTriggerCol)
    {
        if (!floodParent) return;

        this.floodParent = floodParent;

        if (!floodTriggerCol) floodTriggerCol = GetComponent<Collider>();

        triggerCollider = floodTriggerCol;
    }

    public void DamageInteractablesInFloodTrigger()
    {
        if (!floodParent || !triggerCollider)
        {
            Debug.LogWarning("[FLOOD] No floodParent or triggerCollider");
            return;
        }
        Bounds b = triggerCollider.bounds;
        Vector3 center = b.center;
        Vector3 halfExtents = b.extents;

        float damageToDeal = GameManager.Instance?.CurrentStorm?.currentStormDamage ?? 1f;
        if (GameManager.Instance && GameManager.Instance.CurrentStorm != null)
            damageToDeal = GameManager.Instance.CurrentStorm.currentStormDamage;

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.identity,
            ~0, 
            QueryTriggerInteraction.Collide
        );
        Debug.Log($"[FLOOD] OverlapBox found {hits.Length} colliders");
        alreadyDamagedInteractables.Clear();

        foreach (var col in hits)
        {
            if (!col) continue;

            if (col.TryGetComponent(out InteractableDamageReceiver receiver))
            {
                Debug.Log($"[FLOOD] Damaging {receiver.name} with {damageToDeal}");
                if (alreadyDamagedInteractables.Contains(receiver)) continue;
                receiver.TakeDamage(damageToDeal);

                alreadyDamagedInteractables.Add(receiver);
            }
        }
    }

    private IEnumerator EnableFloodDamageTickCoroutine()
    {
        if (!floodParent || !triggerCollider) yield break;

        if (isProcessingDamageTick) yield break;

        isProcessingDamageTick = true;

        alreadyDamagedInteractables.Clear();

        triggerCollider.enabled = true;

        yield return new WaitForFixedUpdate();

        triggerCollider.enabled = false;

        isProcessingDamageTick = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessFloodTriggerEventDamageTick(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessFloodTriggerEventDamageTick(other);
    }

    private void ProcessFloodTriggerEventDamageTick(Collider other)
    {
        if (!floodParent || !triggerCollider) return;

        if (!isProcessingDamageTick) return;

        if (!other) return;

        InteractableDamageReceiver interactableDamageReceiver = null;

        other.TryGetComponent<InteractableDamageReceiver>(out interactableDamageReceiver);

        if (!interactableDamageReceiver) return;

        if (alreadyDamagedInteractables.Contains(interactableDamageReceiver)) return;

        float damageToDeal = 1.0f;

        if (GameManager.Instance && GameManager.Instance.CurrentStorm != null)
            damageToDeal = GameManager.Instance.CurrentStorm.currentStormDamage;

        interactableDamageReceiver.TakeDamage(damageToDeal);

        alreadyDamagedInteractables.Add(interactableDamageReceiver);
    }
}
