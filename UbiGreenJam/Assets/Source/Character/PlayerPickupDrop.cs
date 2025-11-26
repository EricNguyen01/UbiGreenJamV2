using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class PlayerPickupDrop : MonoBehaviour
    {
        [Header("Pickup Settings")]
        public float pickupRange = 3f;
        public LayerMask pickupLayer = ~0;
        public Transform holdPoint;
        public Camera aimCamera;

        Rigidbody heldRb;

        // Anchor rigidbody used to keep a physical joint while preserving colliders
        Rigidbody holdAnchorRb;

        FixedJoint currentJoint;

        CollisionDetectionMode previousCollisionMode;

        private InteractableBase currentInteractableLookAt;

        private InteractableBase interactableHeld;

        private bool isHoldingLMB = false;

        private Dictionary<Rigidbody, InteractableBase> interactableRigidbodyDict = new Dictionary<Rigidbody, InteractableBase>();

        private bool pickUpOriginalKinematicState = false;

        void Start()
        {
            if (holdPoint == null)
            {
                GameObject hp = new GameObject("HoldPoint");
                hp.transform.SetParent(transform);
                hp.transform.localPosition = new Vector3(0f, 0.5f, 1f);
                hp.transform.localRotation = Quaternion.identity;
                holdPoint = hp.transform;
            }

            // Ensure the holdPoint has a kinematic Rigidbody to connect joints to
            holdAnchorRb = holdPoint.GetComponent<Rigidbody>();
            if (holdAnchorRb == null)
            {
                holdAnchorRb = holdPoint.gameObject.AddComponent<Rigidbody>();
                holdAnchorRb.isKinematic = true;
                // Keep default collision detection for the anchor (anchor will be moved with MovePosition)
            }
        }

        void Update()
        {
            if (Photon.Pun.PhotonNetwork.InRoom)
            {
                if (!TryGetComponent<Photon.Pun.PhotonView>(out var pv) || !pv.IsMine)
                    return; 
            }

            CheckForInteractablePrompt();

            // Pick on left mouse button press
            if (Input.GetMouseButtonDown(0) && !isHoldingLMB)
            {
                isHoldingLMB = true;

                if(!heldRb) TryPickupInFront();
            }

            // If the left mouse button is released, drop
            if (Input.GetMouseButtonUp(0) && isHoldingLMB)
            {
                isHoldingLMB = false;

                if(heldRb) Drop();
            }
        }

        /*void FixedUpdate()
        {
            if (Photon.Pun.PhotonNetwork.InRoom)
            {
                if (!TryGetComponent<Photon.Pun.PhotonView>(out var pv) || !pv.IsMine)
                    return;
            }
            // Move the kinematic anchor to the hold point position using MovePosition/MoveRotation so physics can resolve collisions
            if (holdAnchorRb != null)
            {
                holdAnchorRb.MovePosition(holdPoint.position);

                holdAnchorRb.MoveRotation(holdPoint.rotation);
            }
        }*/

        [Photon.Pun.PunRPC]
        void RPC_SetHeld(bool held)
        {
            var interactable = heldRb.GetComponent<InteractableBase>();
            if (interactable != null)
                interactable.isBeingHeld = held;
        }
        void TryPickupInFront()
        {
            Ray ray;

            if (aimCamera != null)
            {
                ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
            }
            else
            {
                ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Collide))
            {
                Rigidbody rb = hit.rigidbody;

                if (!rb) return;

                InteractableBase interactable = null;

                if (!interactableRigidbodyDict.TryGetValue(rb, out interactable))
                {
                    return;
                }

                if (interactable.isPendingDestroy) return;

                if (!interactable.allowPickUp) return;

                pickUpOriginalKinematicState = rb.isKinematic;

                Pickup(rb, interactable);
            }
        }
        void CheckForInteractablePrompt()
        {
            if (heldRb) return;

            Ray ray = aimCamera != null
                ? new Ray(aimCamera.transform.position, aimCamera.transform.forward)
                : new Ray(transform.position + Vector3.up * 0.8f, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Collide))
            {
                InteractableBase interactable = null;

                if (hit.rigidbody)
                {
                    if (!interactableRigidbodyDict.TryGetValue(hit.rigidbody, out interactable))
                    {
                        interactable = hit.collider.GetComponentInParent<InteractableBase>();

                        interactableRigidbodyDict.TryAdd(hit.rigidbody, interactable);
                    }
                }
                else
                {
                    interactable = hit.collider.GetComponentInParent<InteractableBase>();
                }

                if (interactable != null && !interactable.isPendingDestroy)
                {
                    if (currentInteractableLookAt && interactable != currentInteractableLookAt)
                    {
                        currentInteractableLookAt.HidePrompt();

                        currentInteractableLookAt.EnableInteractableOutline(false);
                    }

                    if (interactable.GetType() == typeof(FloodDrain))
                    {
                        FloodDrain floodDrain = interactable as FloodDrain;

                        currentInteractableLookAt = interactable;

                        floodDrain.ShowPrompt();

                        floodDrain.EnableInteractableOutline(true);

                        return;
                    }

                    if (!interactable.isBeingHeld && !heldRb)
                    {
                        currentInteractableLookAt = interactable;

                        interactable.ShowPrompt();

                        interactable.EnableInteractableOutline(true);
                    }

                    return;
                }
            }

            //If not looking at any interactable ---------------------------------------------------------------------------

            if (currentInteractableLookAt)
            {
                currentInteractableLookAt.HidePrompt();

                currentInteractableLookAt.EnableInteractableOutline(false);

                currentInteractableLookAt = null;
            }
        }

        void Pickup(Rigidbody rb, InteractableBase interactable)
        {
            if (!rb) return;

            if(!interactable) return;

            //TOO HEAVY -> NOT PICK UP AND RETURN

            if (interactable.itemData != null && interactable.itemData.weight > 1f)
            {
                interactable.ShowTemporaryMessage("Too heavy for one to carry!", interactable.itemData.cost, 1.5f);

                return;
            }

            //NOT TOO HEAVY -> PICKUP-ABLE CODE BELOW

            heldRb = rb;

            interactable.isBeingHeld = true;

            interactableHeld = interactable;

            heldRb.isKinematic = false;

            if (interactable.furnitureColliderRigidbodyData)
            {
                interactable.furnitureColliderRigidbodyData.DisableFurnitureColliders(true);
            }

            if (interactable.dogAI)
            {
                interactable.dogAI.SetHeld(true);
            }

            interactable.HidePrompt();

            interactable.EnableInteractableOutline(false);

            if (currentInteractableLookAt && currentInteractableLookAt != interactable)
            {
                currentInteractableLookAt.HidePrompt();

                currentInteractableLookAt.EnableInteractableOutline(false);
            }

            if (GameManager.Instance) GameManager.Instance.SetHeldItem(interactable);

            if (currentJoint != null)
            {
                Destroy(currentJoint);

                currentJoint = null;
            }

            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;

            if (holdPoint)
            {
                heldRb.transform.SetParent(holdPoint);
            }
            else
            {
                heldRb.transform.SetParent(null);
            }

            previousCollisionMode = heldRb.collisionDetectionMode;

            heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            currentJoint = heldRb.gameObject.AddComponent<FixedJoint>();
            currentJoint.connectedBody = holdAnchorRb;
            currentJoint.breakForce = Mathf.Infinity;
            currentJoint.breakTorque = Mathf.Infinity;
            currentJoint.enableCollision = false;
        }

        void Drop()
        {
            if (!heldRb) return;

            if (!interactableHeld) return;

            InteractableBase interactable;

            if (!interactableRigidbodyDict.TryGetValue(heldRb, out interactable)) return;

            if (interactable != interactableHeld) return;

            if (currentJoint) currentJoint.connectedBody = null;

            if(heldRb.transform.parent) heldRb.transform.SetParent(null);

            heldRb.isKinematic = false;

            heldRb.AddForce(transform.forward * 50.0f + Vector3.up * 200.0f, ForceMode.Impulse);

            if (interactableHeld.furnitureColliderRigidbodyData)
            {
                interactableHeld.furnitureColliderRigidbodyData.DisableFurnitureColliders(false, 0.1f);
            }

            if (interactableHeld.dogAI)
            {
                interactableHeld.dogAI.SetHeld(false);
            }

            interactableHeld.isBeingHeld = false;

            interactableHeld.HidePrompt();

            interactableHeld.EnableInteractableOutline(false);

            if (currentInteractableLookAt && currentInteractableLookAt != interactableHeld)
            {
                currentInteractableLookAt.HidePrompt();

                currentInteractableLookAt.EnableInteractableOutline(false);

                currentInteractableLookAt = null;
            }

            if (GameManager.Instance) GameManager.Instance.ClearHeldItem();

            if (currentJoint != null)
            {
                Destroy(currentJoint);

                currentJoint = null;
            }

            heldRb.isKinematic = pickUpOriginalKinematicState;

            pickUpOriginalKinematicState = false;

            heldRb.collisionDetectionMode = previousCollisionMode;

            heldRb = null;
        }


        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 origin;
            Vector3 dir;
            if (aimCamera != null)
            {
                origin = aimCamera.transform.position;
                dir = aimCamera.transform.forward;
            }
            else
            {
                origin = transform.position + Vector3.up * 0.5f;
                dir = transform.forward;
            }
            Gizmos.DrawLine(origin, origin + dir * pickupRange);
            Gizmos.DrawWireSphere(origin + dir * pickupRange, 0.05f);
        }
    }
}