using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        if(!enabled) return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        Vector3 lookPos = transform.position + targetCamera.transform.rotation * Vector3.forward;
        Vector3 up = targetCamera.transform.rotation * Vector3.up;
        transform.LookAt(lookPos, up);
    }
}
