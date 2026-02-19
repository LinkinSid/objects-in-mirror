using UnityEngine;

public class Switch : MonoBehaviour
{
    public EnemyScript sentry;
    public Sprite activatedSprite;

    private bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (other.GetComponent<PlayerScript>() == null) return;

        used = true;

        if (sentry != null)
        {
            SpriteRenderer sentrySr = sentry.GetComponent<SpriteRenderer>();
            if (sentry.config != null && sentry.config.deathSprite != null && sentrySr != null)
                sentrySr.sprite = sentry.config.deathSprite;

            sentry.enabled = false;
        }

        if (activatedSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = activatedSprite;
        }
    }
}
