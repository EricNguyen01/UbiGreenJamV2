using UnityEngine;

[DisallowMultipleComponent]
public class InteractableDamageReceiver : MonoBehaviour
{
    private InteractableBase interactableParent;

    private void Start()
    {
        if (!interactableParent)
        {
            interactableParent = GetComponentInParent<InteractableBase>();
        }
    }

    public void InitDamageReceiver(InteractableBase interactableParent)
    {
        if(!interactableParent) return;

        this.interactableParent = interactableParent;
    }

    public void TakeDamage(float damageValue)
    {
        if (!interactableParent) return;

        if (interactableParent.isBeingHeld || interactableParent.isPendingDestroy) return;

        interactableParent.DeductInteractableHealth(damageValue);
    }
}
