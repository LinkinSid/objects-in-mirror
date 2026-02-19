using UnityEngine;

public class DangerZone : MonoBehaviour
{
    float radius;
    float damage;
    float delay;
    float timer;
    Health targetHealth;
    SpriteRenderer sr;

    static Sprite circleSprite;

    public static void Spawn(
        Vector2 position, float radius, float damage,
        float delay, Health targetHealth)
    {
        var go = new GameObject("DangerZone");
        go.transform.position = position;

        var zone = go.AddComponent<DangerZone>();
        zone.radius = radius;
        zone.damage = damage;
        zone.delay = delay;
        zone.timer = delay;
        zone.targetHealth = targetHealth;

        zone.sr = go.AddComponent<SpriteRenderer>();
        zone.sr.sprite = GetCircleSprite();
        zone.sr.color = new Color(0f, 0.4f, 1f, 0.15f);
        zone.sr.sortingOrder = 5;
        go.transform.localScale = Vector3.one * radius * 2f;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        float progress = 1f - (timer / delay);
        float alpha = Mathf.Lerp(0.15f, 0.5f, progress);
        sr.color = new Color(0f, 0.4f, 1f, alpha);

        if (timer <= 0)
        {
            Detonate();
            Destroy(gameObject);
        }
    }

    void Detonate()
    {
        sr.color = new Color(0f, 0.6f, 1f, 0.8f);

        float dist = Vector2.Distance(transform.position, targetHealth.transform.position);
        if (dist <= radius)
            targetHealth.TakeDamage(damage);
    }

    static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        int size = 64;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float r = size / 2f;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= r
                    ? Color.white : Color.clear);

        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        return circleSprite;
    }
}
