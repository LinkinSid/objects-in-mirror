using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WallOutline : MonoBehaviour
{
    public Color outlineColor = new Color(0.3f, 0.3f, 0.5f, 0.5f);
    public float lineWidth = 0.05f;

    void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 4;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = outlineColor;
        lr.endColor = outlineColor;
        lr.sortingOrder = 10;

        // Simple unlit colored material
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = outlineColor;

        Vector2 c = box.offset;
        Vector2 s = box.size / 2f;
        lr.SetPosition(0, new Vector3(c.x - s.x, c.y - s.y, 0));
        lr.SetPosition(1, new Vector3(c.x + s.x, c.y - s.y, 0));
        lr.SetPosition(2, new Vector3(c.x + s.x, c.y + s.y, 0));
        lr.SetPosition(3, new Vector3(c.x - s.x, c.y + s.y, 0));
    }
}
