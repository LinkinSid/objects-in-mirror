using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    public Color barColor = new Color(0.2f, 0.8f, 0.2f);
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f);
    public float barWidth = 1.2f;
    public float barHeight = 0.15f;
    public float yOffset = 0.8f;

    private Health health;
    private Transform fillBar;
    private GameObject barRoot;

    void Start()
    {
        health = GetComponent<Health>();

        EnemyScript enemy = GetComponent<EnemyScript>();
        if (enemy != null && enemy.config != null)
        {
            barColor = enemy.config.healthBarColor;
            backgroundColor = enemy.config.healthBarBgColor;
            yOffset = enemy.config.healthBarYOffset;
        }

        BuildBar();
    }

    void BuildBar()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

        // Background
        barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(transform);
        barRoot.transform.localPosition = new Vector3(0, yOffset, 0);
        barRoot.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        SpriteRenderer bgSr = barRoot.AddComponent<SpriteRenderer>();
        bgSr.sprite = pixel;
        bgSr.color = backgroundColor;
        bgSr.sortingOrder = 90;

        // Fill
        GameObject fill = new GameObject("HealthBarFill");
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
        if (health == null || fillBar == null) return;

        if (health.isDead && barRoot != null)
        {
            barRoot.SetActive(false);
            return;
        }

        float ratio = health.currentHealth / health.maxHealth;
        fillBar.localScale = new Vector3(ratio, 1f, 1f);
        fillBar.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
    }
}
