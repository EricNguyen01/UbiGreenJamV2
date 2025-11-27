using GameCore;
using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractableDamageReceiver : MonoBehaviourPun
{
    private InteractableBase interactableParent;

    void Start()
    {
        if (!interactableParent)
            interactableParent = GetComponentInParent<InteractableBase>();
    }

    public void InitDamageReceiver(InteractableBase parent)
    {
        if (!parent) return;
        interactableParent = parent;
    }

    public void TakeDamage(float damageValue)
    {
        Debug.Log($"[DMG] TakeDamage called on {name}, dmg={damageValue}, InRoom={PhotonNetwork.InRoom}");
        if (!interactableParent) return;
        if (interactableParent.isBeingHeld || interactableParent.isPendingDestroy) return;
        if (PhotonNetwork.InRoom)
        {
            PhotonView pv = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
            if (pv != null && pv.ViewID != 0)
            {
                Debug.Log($"[DMG] Sending RPC_ApplyDamage via PV {pv.ViewID} on {name}");
                pv.RPC(nameof(RPC_ApplyDamage), RpcTarget.AllBuffered, damageValue);
                return;
            }
        }
        ApplyDamageLocal(damageValue);
    }

    [PunRPC]
    void RPC_ApplyDamage(float damageValue)
    {
        Debug.Log($"[DMG] RPC_ApplyDamage received on {name}, dmg={damageValue}");
        ApplyDamageLocal(damageValue);
    }

    void ApplyDamageLocal(float damageValue)
    {
        Debug.Log($"[DMG] ApplyDamageLocal on {name}, dmg={damageValue}");
        if (!interactableParent) return;
        if (interactableParent.isBeingHeld || interactableParent.isPendingDestroy) return;

        interactableParent.DeductInteractableHealth(damageValue);
    }
}
