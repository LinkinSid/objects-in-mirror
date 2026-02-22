using UnityEngine;

public class DarkenRoom : MonoBehaviour
{
    public float flickerInterval = 2f;
    public Color darkColor = new Color(0f, 0f, 0f, 0.92f);

    private SpriteRenderer overlay;
    private float timer;
    private bool dark;
    private bool active;

    void Update()
    {
        if (!active)
        {
            if (GameManager.Instance == null || !GameManager.Instance.bossChaseActive) return;
            active = true;

            GameObject go = new GameObject("DarkenOverlay");
            go.transform.SetParent(Camera.main.transform);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);

            overlay = go.AddComponent<SpriteRenderer>();
            overlay.sprite = CreateWhiteSquare();
            overlay.color = new Color(0f, 0f, 0f, 0f);
            overlay.sortingOrder = 9;
            go.transform.localScale = new Vector3(50f, 50f, 1f);
        }

        // Chase ended — remove overlay
        if (!GameManager.Instance.bossChaseActive)
        {
            if (overlay != null)
                Destroy(overlay.gameObject);
            enabled = false;
            return;
        }

        timer += Time.deltaTime;
        bool shouldBeDark = ((int)(timer / flickerInterval) % 2) == 0;

        if (shouldBeDark != dark)
        {
            dark = shouldBeDark;
            overlay.color = dark ? darkColor : new Color(0f, 0f, 0f, 0f);
        }
    }

    Sprite CreateWhiteSquare()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
