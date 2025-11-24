using UnityEngine;

public class WaterUVScroller : MonoBehaviour
{
    public float scrollSpeedX = 0.02f;
    public float scrollSpeedY = 0.01f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float x = Time.time * scrollSpeedX;
        float y = Time.time * scrollSpeedY;

        rend.material.SetTextureOffset("_BaseMap", new Vector2(x, y));
        rend.material.SetTextureOffset("_BumpMap", new Vector2(x, y));
    }
}