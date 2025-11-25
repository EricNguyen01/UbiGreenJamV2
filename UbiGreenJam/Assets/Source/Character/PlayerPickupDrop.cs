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

        void FixedUpdate()
        {
            // Move the kinematic anchor to the hold point position using MovePosition/MoveRotation so physics can resolve collisions
            if (holdAnchorRb != null)
            {
                holdAnchorRb.MovePosition(holdPoint.position);
                holdAnchorRb.MoveRotation(holdPoint.rotation);
            }
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

                if(!interactableRigidbodyDict.TryGetValue(hit.rigidbody, out interactable))
                {
                    interactable = hit.collider.GetComponentInParent<InteractableBase>();

                    interactableRigidbodyDict.TryAdd(hit.rigidbody, interactable);
                }

                if (interactable != null)
                {
                    if (!interactable.isBeingHeld && !heldRb)
                    {
                        if (currentInteractableLookAt && interactable != currentInteractableLookAt) currentInteractableLookAt.HidePrompt();

                        currentInteractableLookAt = interactable;

                        interactable.ShowPrompt();
                    }

                    return;
                }
            }

            if (currentInteractableLookAt)
            {
                currentInteractableLookAt.HidePrompt();

                currentInteractableLookAt = null;
            }
        }

        
        /*void HideLastLookedAtInteractablePrompts()
        {
            /*var allInteractables = FindObjectsOfType<InteractableBase>();
            foreach (var item in allInteractables)
            {
                item.HidePrompt();
            }
            if (!currentInteractableLookAt) return;

            currentInteractableLookAt.HidePrompt();
        }*/

        void Pickup(Rigidbody rb, InteractableBase interactable)
        {
            if (!rb || !interactable) return;

            rb.isKinematic = false;

            if (interactable != null)
            {
                //TOO HEAVY -> NOT PICK UP AND RETURN

                if (interactable.itemData != null && interactable.itemData.weight > 1f)
                {
                    interactable.ShowTemporaryMessage("Too heavy for one to carry!", interactable.itemData.cost, 1.5f);

                    return;    
                }

                //NOT TOO HEAVY -> PICKUP-ABLE CODE BELOW

                if (interactable.furnitureColliderRigidbodyData)
                {
                    interactable.furnitureColliderRigidbodyData.DisableFurnitureColliders(true);
                }

                if (interactable.dogAI)
                {
                    interactable.dogAI.SetHeld(true);
                }

                interactable.isBeingHeld = true;

                interactableHeld = interactable;

                interactable.HidePrompt();

                if (GameManager.Instance) GameManager.Instance.SetHeldItem(interactable);
            }

            if (currentJoint != null)
            {
                Destroy(currentJoint);

                currentJoint = null;
            }

            heldRb = rb;

            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;

            previousCollisionMode = heldRb.collisionDetectionMode;
            heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            heldRb.transform.SetParent(null);

            currentJoint = heldRb.gameObject.AddComponent<FixedJoint>();
            currentJoint.connectedBody = holdAnchorRb;
            currentJoint.breakForce = Mathf.Infinity;
            currentJoint.breakTorque = Mathf.Infinity;
        }

        void Drop()
        {
            if (!interactableHeld || !heldRb) return;

            heldRb.isKinematic = pickUpOriginalKinematicState;

            pickUpOriginalKinematicState = false;

            if (!interactableHeld && heldRb)
            {
                if (!interactableRigidbodyDict.TryGetValue(heldRb, out interactableHeld)) return;
            }

            if (interactableHeld != null)
            {
                if (interactableHeld.furnitureColliderRigidbodyData)
                {
                    interactableHeld.furnitureColliderRigidbodyData.ProcessFurnitureDropped();
                }

                if (interactableHeld.dogAI)
                {
                    interactableHeld.dogAI.SetHeld(false);
                }

                interactableHeld.isBeingHeld = false;

                interactableHeld.HidePrompt();

                if (currentInteractableLookAt)
                {
                    currentInteractableLookAt.HidePrompt();

                    currentInteractableLookAt = null;
                }

                if(GameManager.Instance) GameManager.Instance.ClearHeldItem();
            }

            if (currentJoint != null)
            {
                Destroy(currentJoint);

                currentJoint = null;
            }

            if(heldRb) heldRb.collisionDetectionMode = previousCollisionMode;

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