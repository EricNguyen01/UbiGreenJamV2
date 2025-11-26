using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class WorldUIFacingCam : MonoBehaviour
{
    private Canvas canvas;
    private Camera cam;
    private Vector3 smoothVel;

    private void Start()
    {
        TryGetComponent<Canvas>(out canvas);
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!canvas) { enabled = false; return; }

        StartCoroutine(WaitForLocalPlayerCamera());
    }

    private IEnumerator WaitForLocalPlayerCamera()
    {
        while (GameSceneManager.GameSceneManagerInstance == null ||
               GameSceneManager.GameSceneManagerInstance.localPlayerChar == null ||
               GameSceneManager.GameSceneManagerInstance.localPlayerChar.characterPickupDrop == null ||
               GameSceneManager.GameSceneManagerInstance.localPlayerChar.characterPickupDrop.aimCamera == null)
        {
            yield return null;
        }

        canvas.worldCamera = GameSceneManager.GameSceneManagerInstance.localPlayerChar.characterPickupDrop.aimCamera;

        if (!canvas.worldCamera) canvas.worldCamera = Camera.main;
        if (!canvas.worldCamera) { enabled = false; yield break; }

        cam = canvas.worldCamera;
    }

    private void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy || !gameObject.activeSelf || cam == null) return;

        Vector3 dir = transform.position - cam.transform.position;
        float dist = Vector3.Distance(transform.position, cam.transform.position) * 0.85f;
        dir.Normalize();

        Vector3 displayPos = transform.parent.position + dir * dist;
        float distA = Vector3.Distance(displayPos, cam.transform.position);
        float distB = Vector3.Distance(transform.position, cam.transform.position);

        if (distA >= distB)
        {
            transform.position = Vector3.SmoothDamp(transform.position, transform.parent.position - dir * dist, ref smoothVel, Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, displayPos, ref smoothVel, Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (!enabled || !gameObject.activeInHierarchy || !gameObject.activeSelf || cam == null) return;

        transform.LookAt(cam.transform.position);
    }
}
