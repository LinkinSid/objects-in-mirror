using UnityEngine;

public class Trap : MonoBehaviour
{
    public float damage = 25f;

    void OnTriggerEnter2D(Collider2D other)
    {
        Health health = other.GetComponent<Health>();
        if (health != null)
            health.TakeDamage(damage);
    }
}
