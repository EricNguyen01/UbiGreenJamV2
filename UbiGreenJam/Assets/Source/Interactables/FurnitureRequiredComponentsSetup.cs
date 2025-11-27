using DG.Tweening;
using GameCore;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class FurnitureRequiredComponentsSetup : MonoBehaviour
{
    [Header("Rigidbody Settings")]

    [SerializeField]
    [Min(20.0f)]
    private float rigidbodyMass = 25.0f;

    [Header("Furniture Outline Settings")]

    [SerializeField]
    private Outline.Mode outlineMode = Outline.Mode.OutlineAll;

    [SerializeField]
    private Color outlineColor;

    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float outlineWidth = 5.0f;

    [Header("Furniture Health Overlay Material")]

    [SerializeField]
    private Material BaseHealthOverlayMat;

    private Material instantiatedHealthOverlayMat;

    //INTERNALS -------------------------------------------------------------------------------------------------------------------

    private Rigidbody rb;

    private List<Collider> colliders = new List<Collider>();

    public MeshRenderer meshRend { get; private set; }

    private Collider meshRendTrigger;

    private Dictionary<Collider, CharacterController> charControllersDict = new Dictionary<Collider, CharacterController>();

    private InteractableBase interactableUsing;

    private InteractableDamageReceiver interactableDamageReceiver;

    private Outline outlineComp;

    private bool isProcessingCollidersDelay = false;

    private bool isInOtherTrigger = false;

    private void Awake()
    {
        interactableUsing = GetComponent<InteractableBase>();

        if(!interactableUsing) interactableUsing = GetComponentInChildren<InteractableBase>();

        if (!interactableUsing)
        {
            Debug.LogError($"FurnitureRequiredComponentsSetup component on {name} doesn't have an InteractableBase component associated with. " +
                           "Disabling game object...");

            enabled = false;

            gameObject.SetActive(false);

            return;
        }

        meshRend = GetComponent<MeshRenderer>();

        if(!meshRend) meshRend = GetComponentInChildren<MeshRenderer>();

        if (!meshRend)
        {
            Debug.LogError($"Couldn't find a mesh renderer on Interactable {name}. " +
                            "It's assumed that this interactable is not properly setup and will now be disabled!");

            enabled = false;

            gameObject.SetActive(false);

            return;
        }

        interactableDamageReceiver = meshRend.GetComponent<InteractableDamageReceiver>();

        if(!interactableDamageReceiver) interactableDamageReceiver = meshRend.AddComponent<InteractableDamageReceiver>();

        interactableDamageReceiver.InitDamageReceiver(interactableUsing);

        SetupHealthOverlayMat();

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

        GetOrAddOutlineComponent();

        EnableOutline(false);
    }

    private void Update()
    {
        if (meshRend)
        {
            instantiatedHealthOverlayMat.SetFloat("_MinY", meshRend.bounds.min.y - 0.1f);

            instantiatedHealthOverlayMat.SetFloat("_MaxY", meshRend.bounds.max.y);
        }
    }

    public void DisableFurnitureColliders(bool disabled)
    {
        if(colliders == null || colliders.Count == 0) return;

        if (disabled)
        {
            foreach(var collider in colliders)
            {
                collider.enabled = false;
            }

            return;
        }

        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
    }

    public void DisableFurnitureColliders(bool disabled, float delay)
    {
        if(delay <= 0.0f)
        {
            DisableFurnitureColliders(disabled);

            return;
        }

        if (isProcessingCollidersDelay)
        {
            StopCoroutine(DisableFurnitureCollidersDelay(disabled, delay));

            isProcessingCollidersDelay = false;
        }

        StartCoroutine(DisableFurnitureCollidersDelay(disabled, delay));
    }

    private IEnumerator DisableFurnitureCollidersDelay(bool disabled, float delay)
    {
        if (isProcessingCollidersDelay) yield break;

        if(delay <= 0.0f)
        {
            DisableFurnitureColliders(disabled);

            yield break;
        }

        isProcessingCollidersDelay = true;

        yield return new WaitForSecondsRealtime(delay);

        DisableFurnitureColliders(disabled);

        isProcessingCollidersDelay = false;
    }

    public void EnableOutline(bool enabled)
    {
        if(!outlineComp) return;

        if(enabled)
        {
            outlineComp.enabled = true;

            return;
        }

        outlineComp.enabled = false;
    }

    [ContextMenu("AddOutlineComponent_Editor")]
    private void GetOrAddOutlineComponent()
    {
        MeshRenderer meshRend = this.meshRend;

        if(!meshRend) meshRend = GetComponent<MeshRenderer>();

        if (!meshRend) meshRend = GetComponentInChildren<MeshRenderer>();

        if (!meshRend) return;

        outlineComp = meshRend.GetComponent<Outline>();

        if (!outlineComp) outlineComp = meshRend.AddComponent<Outline>();

        foreach (Outline outline in meshRend.GetComponentsInChildren<Outline>())
        {
            if (outline)
            {
                if (outline != outlineComp)
                {
                    if (!Application.isPlaying) DestroyImmediate(outline);
                    else Destroy(outline);
                }
            }
        }

        outlineComp.OutlineMode = outlineMode;

        outlineComp.OutlineColor = outlineColor;

        outlineComp.OutlineWidth = outlineWidth;

        outlineComp.precomputeOutline = true;
    }

    private void SetupHealthOverlayMat()
    {
        if (!BaseHealthOverlayMat) return;

        if (!meshRend) return;

        instantiatedHealthOverlayMat = Instantiate(BaseHealthOverlayMat);

        HelperFunction.AddUniqueMaterial(meshRend, instantiatedHealthOverlayMat);

        instantiatedHealthOverlayMat.SetFloat("_MinY", meshRend.bounds.min.y - 0.1f);

        instantiatedHealthOverlayMat.SetFloat("_MaxY", meshRend.bounds.max.y);

        instantiatedHealthOverlayMat.SetFloat("_Float", 0.0f);
    }

    public void SetHealthOverlayShaderValue(float value)
    {
        if(!instantiatedHealthOverlayMat) return;

        value = (float)System.Math.Round(value, 1);

        float currentFloat = instantiatedHealthOverlayMat.GetFloat("_Float");

        DOTween.To(() => currentFloat, x => currentFloat = x, value, 0.1f)
            .OnUpdate(() =>
            {
                instantiatedHealthOverlayMat.SetFloat("_Float", Mathf.Clamp01(currentFloat));
            });
    }
}
