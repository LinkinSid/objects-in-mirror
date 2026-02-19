using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ShadowZone : MonoBehaviour
{
    [Header("Fog Visual")]
    public Color fogColor = new Color(0f, 0f, 0.05f, 0.5f);
    public int sortingOrder = 5;

    [Header("Fog Shader")]
    public Material fogMaterial;

    void Start()
    {
        // Ensure collider is a trigger
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        BuildFogVisual(box);
    }

    void BuildFogVisual(BoxCollider2D box)
    {
        // 1x1 white pixel sprite
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

        // Child object for the visual
        var fogGo = new GameObject("Fog");
        fogGo.transform.SetParent(transform);
        fogGo.transform.localPosition = new Vector3(box.offset.x, box.offset.y, 0f);
        fogGo.transform.localRotation = Quaternion.identity;
        fogGo.transform.localScale = new Vector3(box.size.x, box.size.y, 1f);

        var sr = fogGo.AddComponent<SpriteRenderer>();
        sr.sprite = pixel;
        sr.sortingOrder = sortingOrder;

        if (fogMaterial != null)
        {
            // Use the assigned material (animated fog shader)
            sr.material = new Material(fogMaterial);
        }
        else
        {
            // Simple dark overlay — always works, no shader needed
            sr.color = fogColor;
        }
    }
}
