using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace GameCore
{
    public class PlayerPickupDrop : CharacterComponentBase
    {
        [Header("Pickup Settings")]
        public float pickupRange = 3f;
        public LayerMask pickupLayer = ~0;
        public Transform holdPoint;
        public Camera aimCamera;

        Rigidbody heldRb;
        PhotonView heldPhotonView;
        // Anchor rigidbody used to keep a physical joint while preserving colliders
        Rigidbody holdAnchorRb;

        FixedJoint currentJoint;

        CollisionDetectionMode previousCollisionMode;

        private InteractableBase currentInteractableLookAt;

        private InteractableBase interactableHeld;

        private bool isHoldingLMB = false;

        private Dictionary<Rigidbody, InteractableBase> interactableRigidbodyDict = new Dictionary<Rigidbody, InteractableBase>();

        private bool pickUpOriginalKinematicState = false;
        [Header("Drop Tuning")]
        public Vector3 dropImpulse = new Vector3(5f, 20f, 0f); 
        PhotonView ownPV;

        private Vector3 smoothVel;

        protected override void Start()
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
            if (heldRb != null && holdPoint)
            {
                //heldRb.transform.position = Vector3.SmoothDamp(heldRb.transform.position, holdPoint.transform.position, ref smoothVel, Time.fixedDeltaTime, 10.0f);

                //holdAnchorRb.MoveRotation(holdPoint.rotation);
            }
        }*/

        [Photon.Pun.PunRPC]
        void RPC_SetHeldNetwork(int itemViewId, bool held)
        {
            if (itemViewId == 0) return;

            PhotonView itemPV = PhotonView.Find(itemViewId);
            if (itemPV == null) return;

            InteractableBase interactable = itemPV.GetComponent<InteractableBase>();
            if (!interactable) interactable = itemPV.GetComponentInChildren<InteractableBase>();
            if (!interactable) interactable = itemPV.GetComponentInParent<InteractableBase>();
            if (interactable == null) return;

            interactable.isBeingHeld = held;

            if (held)
            {
                if (interactable.furnitureColliderRigidbodyData)
                {
                    interactable.furnitureColliderRigidbodyData.DisableFurnitureColliders(true);
                }

                interactable.HidePrompt();
                interactable.EnableInteractableOutline(false);
            }
            else
            {
                if (interactable.furnitureColliderRigidbodyData)
                {
                    interactable.furnitureColliderRigidbodyData.DisableFurnitureColliders(false);
                }
                var meshR = interactable.GetComponentInChildren<MeshRenderer>();
                if (meshR != null) meshR.enabled = true;

                interactable.HidePrompt();
                interactable.EnableInteractableOutline(false);
            }
        }
        void RPC_RemoveJoint(int itemViewId)
        {
            PhotonView itemPV = PhotonView.Find(itemViewId);
            if (itemPV == null) return;

            Rigidbody rb = itemPV.GetComponent<Rigidbody>();
            if (rb == null) rb = itemPV.GetComponentInChildren<Rigidbody>();
            if (rb == null) return;

            ConfigurableJoint j = rb.GetComponent<ConfigurableJoint>();
            if (j != null) Destroy(j);
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

                ///FMOD PLAY PICKUP SOUND
                if (AudioManager.Instance) AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PickupSFX, transform.position);

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
            heldPhotonView = rb.GetComponent<PhotonView>();

            if (PhotonNetwork.InRoom && heldPhotonView != null)
            {
                heldPhotonView.TransferOwnership(PhotonNetwork.LocalPlayer);
                Debug.Log($"[Pickup] Ownership requested → IsMine: {heldPhotonView.IsMine}, Owner: {heldPhotonView.Owner}");
            }

            if (!rb) return;

            if(!interactable) return;

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

            if(heldRb.transform.parent) heldRb.transform.SetParent(null);

            if (currentJoint != null)
            {
                Destroy(currentJoint);

                currentJoint = null;
            }

            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;

            currentJoint = heldRb.gameObject.AddComponent<FixedJoint>();

            if (holdAnchorRb) currentJoint.connectedBody = holdAnchorRb;

            previousCollisionMode = heldRb.collisionDetectionMode;

            heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            heldRb.interpolation = RigidbodyInterpolation.Interpolate;

            currentJoint.breakForce = Mathf.Infinity;
            currentJoint.breakTorque = Mathf.Infinity;
            currentJoint.enableCollision = false;

            if (characterUsingComponent)
            {
                characterUsingComponent.SetAnimatorBool("Holding", true);

                characterUsingComponent.SetAnimatorLayerWeight("UpperArms", 0.5f);
            }
        }

        void Drop()
        {
            if (!heldRb || !interactableHeld) return;

            /*foreach (var joint in heldRb.GetComponents<FixedJoint>())
            {
                Destroy(joint);
            }*/

            if (heldRb.transform.parent) heldRb.transform.SetParent(null);

            if (currentJoint != null)
            {
                Destroy(currentJoint);

                currentJoint = null;
            }

            if (characterUsingComponent)
            {
                characterUsingComponent.SetAnimatorBool("Holding", false);

                characterUsingComponent.SetAnimatorLayerWeight("UpperArms", 0.0f);
            }

            heldRb.isKinematic = pickUpOriginalKinematicState;

            heldRb.collisionDetectionMode = previousCollisionMode;

            heldRb.interpolation = RigidbodyInterpolation.None;

            if (interactableHeld.furnitureColliderRigidbodyData)
            {
                interactableHeld.furnitureColliderRigidbodyData.DisableFurnitureColliders(false);
            }

            interactableHeld.isBeingHeld = false;

            interactableHeld.HidePrompt();

            interactableHeld.EnableInteractableOutline(false);

            if (interactableHeld.dogAI)
            {
                interactableHeld.dogAI.SetHeld(false);
            }

            if (currentInteractableLookAt && currentInteractableLookAt != interactableHeld)
            {
                currentInteractableLookAt.HidePrompt();
                currentInteractableLookAt.EnableInteractableOutline(false);
                currentInteractableLookAt = null;
            }

            if (GameManager.Instance)
            {
                GameManager.Instance.ClearHeldItem();
            }

            //Vector3 impulse = transform.forward * dropImpulse.x + Vector3.up * dropImpulse.y;

            heldRb.AddForce(transform.forward * dropImpulse.x + Vector3.up * dropImpulse.y, ForceMode.Impulse);

            if (AudioManager.Instance) AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ThrowDropSFX, transform.position);

            if (PhotonNetwork.InRoom && heldPhotonView != null && heldPhotonView.IsMine)
            {
                heldPhotonView.TransferOwnership(PhotonNetwork.MasterClient);
            }

            heldRb = null;
            interactableHeld = null;
            heldPhotonView = null;
            //currentJoint = null;
            pickUpOriginalKinematicState = false;
        }
    }
}