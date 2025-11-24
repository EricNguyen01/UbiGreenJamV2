using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class FurnitureRigidbodyCheck : MonoBehaviour
{
    [SerializeField]
    [Min(20.0f)]
    private float rigidbodyMass = 25.0f;

    private Rigidbody rb;

    private MeshRenderer meshRend;

    private Dictionary<Collider, CharacterController> charControllersDict = new Dictionary<Collider, CharacterController>();

    private void Awake()
    {
        meshRend = GetComponent<MeshRenderer>();

        if(!meshRend) meshRend = GetComponentInChildren<MeshRenderer>();

        if (!meshRend)
        {
            enabled = false;

            return;
        }

        meshRend.gameObject.isStatic = false;

        int childColliderCount = 0;

        foreach (Collider childColliders in GetComponentsInChildren<Collider>())
        {
            if (!childColliders) continue;

            childColliders.gameObject.isStatic = false;

            if (childColliders.transform.parent != meshRend.transform)
            {
                childColliders.transform.SetParent(meshRend.transform);
            }

            childColliderCount++;
        }

        Collider meshRendCollider = meshRend.GetComponent<Collider>();

        if (meshRendCollider && childColliderCount > 1) Destroy(meshRendCollider);

        Rigidbody meshRendRb = meshRend.GetComponent<Rigidbody>();

        if(!meshRendRb) meshRendRb = meshRend.AddComponent<Rigidbody>();

        foreach(Rigidbody childRb in GetComponentsInChildren<Rigidbody>())
        {
            if(childRb && childRb != meshRendRb) Destroy(childRb);
        }

        Rigidbody thisRb = GetComponent<Rigidbody>();

        if(thisRb && thisRb != meshRendRb) Destroy(thisRb);

        rb = meshRendRb;

        rb.mass = rigidbodyMass;

        if (rb.mass < 20.0f) rb.mass = 20.0f;

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.sleepThreshold = 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!enabled) return;

        if(collision == null) return;

        ProcessCharacterControllerCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!enabled) return;

        if (collision == null) return;

        ProcessCharacterControllerCollision(collision);
    }

    private void ProcessCharacterControllerCollision(Collision collision)
    {
        if (!enabled) return;

        if (!rb) return;

        if (rb.IsSleeping()) rb.WakeUp();

        CharacterController charController = null;

        if (charControllersDict.ContainsKey(collision.collider))
        {
            charController = charControllersDict[collision.collider];
        }
        else
        {
            charController = collision.collider.GetComponent<CharacterController>();

            if (!charController) return;

            charControllersDict.TryAdd(collision.collider, charController);
        }

        if (!charController) return;

        rb.AddForce(charController.transform.forward + collision.relativeVelocity);
    }
}
