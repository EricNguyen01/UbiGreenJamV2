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
            if (Input.GetKeyDown(KeyCode.E) && heldRb == null)
            {
                TryPickupInFront();
            }

            // If the left mouse button is released, drop
            if (Input.GetMouseButtonUp(0) && heldRb != null)
            {
                Drop();
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

            if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.rigidbody;

                if (rb != null)
                {
                    pickUpOriginalKinematicState = rb.isKinematic;

                    Pickup(rb);

                    return;
                }

                pickUpOriginalKinematicState = false;
            }
        }
        void CheckForInteractablePrompt()
        {
            if (heldRb) return;

            Ray ray = aimCamera != null
                ? new Ray(aimCamera.transform.position, aimCamera.transform.forward)
                : new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Collide))
            {
                InteractableBase interactable = hit.collider.GetComponentInParent<InteractableBase>();

                if (interactable != null && !interactable.isBeingHeld)
                {
                    if (currentInteractableLookAt && interactable != currentInteractableLookAt) currentInteractableLookAt.HidePrompt();

                    currentInteractableLookAt = interactable;

                    interactable.ShowPrompt();

                    return;
                }
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

        void Pickup(Rigidbody rb)
        {
            if (rb == null) return;

            rb.isKinematic = false;

            InteractableBase interactable = rb.GetComponentInParent<InteractableBase>();
            if (interactable != null)
            {
                if (interactable.furnitureColliderRigidbodyData)
                {
                    interactable.furnitureColliderRigidbodyData.DisableFurnitureColliders(true);
                }

                if (interactable.dogAI)
                {
                    interactable.dogAI.SetHeld(true);
                }

                if (interactable.itemData != null && interactable.itemData.weight > 1f)
                {
                    interactable.ShowTemporaryMessage("Too heavy for one to carry!", 0, 1.5f);
                    return;    
                }
                interactable.isBeingHeld = true;
                interactable.HidePrompt();
                if(GameManager.Instance) GameManager.Instance.SetHeldItem(interactable);
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
            if (heldRb == null) return;

            heldRb.isKinematic = pickUpOriginalKinematicState;

            pickUpOriginalKinematicState = false;

            var interactable = heldRb.GetComponent<InteractableBase>();

            if (interactable != null)
            {
                if (interactable.furnitureColliderRigidbodyData)
                {
                    interactable.furnitureColliderRigidbodyData.DisableFurnitureColliders(false);
                }

                if (interactable.dogAI)
                {
                    interactable.dogAI.SetHeld(false);
                }

                interactable.isBeingHeld = false;

                if(GameManager.Instance) GameManager.Instance.ClearHeldItem();
            }

            if (currentJoint != null)
            {
                Destroy(currentJoint);
                currentJoint = null;
            }

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