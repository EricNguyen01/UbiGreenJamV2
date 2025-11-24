
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldUIFacingCam : MonoBehaviour
{
    private Canvas canvas;

    private Camera cam;

    private void Awake()
    {
        TryGetComponent<Canvas>(out canvas);

        if (!canvas)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (!canvas)
        {
            enabled = false;

            return;
        }

        if (canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        if (!canvas.worldCamera)
        {
            enabled = false;

            return;
        }

        cam = canvas.worldCamera;
    }

    private void Start()
    {
        if (!GetComponentInParent<InteractableBase>())
        {
            enabled = false;

            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy || !gameObject.activeSelf) return;

        Vector3 dir = transform.position - cam.transform.position;

        float dist = Vector3.Distance(transform.position, cam.transform.position) * 0.85f;
        
        dir.Normalize();

        Vector3 displayPos = transform.parent.position + dir * dist;

        float distA = Vector3.Distance(displayPos, cam.transform.position);

        float distB = Vector3.Distance(transform.position, cam.transform.position);

        if(distA >= distB)
        {
            transform.position = transform.parent.position - dir * dist;
        }
        else
        {
            transform.position = displayPos;
        }
    }

    private void LateUpdate()
    {
        if (!enabled || !gameObject.activeInHierarchy || !gameObject.activeSelf) return;

        transform.LookAt(cam.transform.position);
    }
}
