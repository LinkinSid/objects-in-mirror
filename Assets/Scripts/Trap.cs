using UnityEngine;

public class Trap : MonoBehaviour
{
    public float damage = 25f;

    void OnTriggerEnter2D(Collider2D other)
    {
        ShadowDetector shadow = other.GetComponent<ShadowDetector>();
        if (shadow != null && shadow.isShadowSwimming)
            return;

        Health health = other.GetComponent<Health>();
        if (health != null)
            health.TakeDamage(damage);
    }
}
