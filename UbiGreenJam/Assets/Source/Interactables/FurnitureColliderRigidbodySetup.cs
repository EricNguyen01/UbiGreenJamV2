using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class FurnitureColliderRigidbodySetup : MonoBehaviour
{
    [SerializeField]
    [Min(20.0f)]
    private float rigidbodyMass = 25.0f;

    private Rigidbody rb;

    private List<Collider> colliders = new List<Collider>();

    private MeshRenderer meshRend;

    private Collider meshRendTrigger;

    private Dictionary<Collider, CharacterController> charControllersDict = new Dictionary<Collider, CharacterController>();

    private bool isProcessingDrop = false;

    private bool isInOtherTrigger = false;

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

        colliders.AddRange(GetComponentsInChildren<Collider>());

        foreach (Collider childCollider in colliders)
        {
            if (!childCollider) continue;

            childCollider.gameObject.isStatic = false;

            if (childCollider.transform.parent != meshRend.transform)
            {
                childCollider.transform.SetParent(meshRend.transform);
            }

            if(!childCollider.isTrigger) childColliderCount++;
        }

        Collider meshRendCollider = meshRend.GetComponent<Collider>();

        if (!meshRendCollider)
        {
            //add also a trigger collider for pickup raycast
            meshRendCollider = meshRend.AddComponent<BoxCollider>();
        }

        colliders.Add(meshRendCollider);

        if (childColliderCount > 1)
        {
            meshRendCollider.isTrigger = true;

            meshRendTrigger = meshRendCollider;
        }
        else
        {
            meshRendTrigger = meshRend.AddComponent<BoxCollider>();

            meshRendTrigger.isTrigger = true;

            colliders.Add(meshRendTrigger);
        }

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

    /*private void OnCollisionEnter(Collision collision)
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
    }*/

    private void OnTriggerEnter(Collider other)
    {
        isInOtherTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        isInOtherTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isInOtherTrigger = false;
    }

    public void ProcessFurnitureDropped()
    {
        if (isProcessingDrop)
        {
            StopCoroutine(ProcessFurnitureDroppedCoroutine());
        }

        isProcessingDrop = false;

        StartCoroutine(ProcessFurnitureDroppedCoroutine());
    }

    private IEnumerator ProcessFurnitureDroppedCoroutine()
    {
        if (!rb || !meshRendTrigger) yield break;

        if (isProcessingDrop) yield break;

        isProcessingDrop = true;

        if (isInOtherTrigger)
        {
            rb.AddForce(Vector3.up * 2.5f, ForceMode.Impulse);

            yield return new WaitForSecondsRealtime(0.1f);

            DisableFurnitureColliders(false);
        }
        else
        {
            DisableFurnitureColliders(false);
        }

        isInOtherTrigger = false;

        isProcessingDrop = false;

        yield break;
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

    public void DisableFurnitureColliders(bool disabled)
    {
        if(colliders == null || colliders.Count == 0) return;

        if (disabled)
        {
            foreach(var collider in colliders)
            {
                if(!collider.isTrigger) collider.enabled = false;
            }

            return;
        }

        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
    }
}
