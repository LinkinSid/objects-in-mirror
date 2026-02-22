using UnityEngine;

public class UnlitSprite : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = Mathf.Max(sr.sortingOrder, 10);
    }
}
