using UnityEngine;

public class PlayerFloodDetector : MonoBehaviour
{
    public float waterLevelY;
    public bool isInWater;
    public float submergedAmount;  // 0 = dry, 1 = head under water

    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller == null) return;

        // CharacterController bottom/top in WORLD space
        float feetY = transform.position.y + controller.center.y - (controller.height * 0.5f);
        float headY = transform.position.y + controller.center.y + (controller.height * 0.5f);

        // Are feet in water?
        if (waterLevelY > feetY)
        {
            isInWater = true;

            // 0 when water at feet, 1 when water at head
            submergedAmount = Mathf.InverseLerp(feetY, headY, waterLevelY);
            submergedAmount = Mathf.Clamp01(submergedAmount);
        }
        else
        {
            isInWater = false;
            submergedAmount = 0f;
        }

        // Optional debug
        // Debug.Log($"WaterY {waterLevelY:F2} Feet {feetY:F2} Head {headY:F2} Sub {submergedAmount:F2}");
    }
}