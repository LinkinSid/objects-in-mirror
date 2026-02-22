using UnityEngine;

public class StaminaBarUI : MonoBehaviour
{
    public Color barColor = new Color(0.2f, 0.5f, 1f);
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f);
    public float barWidth = 1.2f;
    public float barHeight = 0.1f;
    public float yOffset = 0.6f;

    private Stamina stamina;
    private Transform fillBar;
    private GameObject barRoot;

    void Start()
    {
        stamina = GetComponent<Stamina>();
        BuildBar();
    }

    void BuildBar()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

        barRoot = new GameObject("StaminaBar");
        barRoot.transform.SetParent(transform);
        barRoot.transform.localPosition = new Vector3(0, yOffset, 0);
        barRoot.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        SpriteRenderer bgSr = barRoot.AddComponent<SpriteRenderer>();
        bgSr.sprite = pixel;
        bgSr.color = backgroundColor;
        bgSr.sortingOrder = 90;

        GameObject fill = new GameObject("StaminaBarFill");
        fill.transform.SetParent(barRoot.transform);
        fill.transform.localPosition = Vector3.zero;
        fill.transform.localScale = Vector3.one;

        SpriteRenderer fillSr = fill.AddComponent<SpriteRenderer>();
        fillSr.sprite = pixel;
        fillSr.color = barColor;
        fillSr.sortingOrder = 91;

        fillBar = fill.transform;
    }

    void Update()
    {
        if (stamina == null || fillBar == null) return;

        float ratio = stamina.currentStamina / stamina.maxStamina;
        fillBar.localScale = new Vector3(ratio, 1f, 1f);
        fillBar.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
    }
}
