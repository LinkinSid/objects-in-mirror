using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action onDeath;

    public float maxHealth = 100f;

    [Header("Contact Damage")]
    public float enemyContactDamage = 15f;

    [Header("Invincibility Frames")]
    public float invincibilityDuration = 1.5f;
    public float flashInterval = 0.1f;

    [Header("Death")]
    public Sprite deathSprite;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool invincible;

    private float invincibilityTimer;
    private SpriteRenderer sr;
    private bool isPlayer;

    public bool isInIFrames => invincibilityTimer > 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        isPlayer = GetComponent<PlayerScript>() != null;

        if (!isPlayer)
        {
            EnemyScript enemy = GetComponent<EnemyScript>();
            if (enemy != null && enemy.config != null)
            {
                maxHealth = enemy.config.maxHealth;
                enemyContactDamage = enemy.config.contactDamage;
                invincibilityDuration = enemy.config.invincibilityDuration;
                deathSprite = enemy.config.deathSprite;
            }
        }

        currentHealth = maxHealth;
    }

    void Update()
    {
        if (invincibilityTimer <= 0f) return;

        invincibilityTimer -= Time.deltaTime;

        if (sr != null)
            sr.enabled = Mathf.FloorToInt(invincibilityTimer / flashInterval) % 2 == 0;

        if (invincibilityTimer <= 0f && sr != null)
            sr.enabled = true;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || invincible || invincibilityTimer > 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        if (currentHealth <= 0f)
            Die();
        else if (invincibilityDuration > 0f)
            invincibilityTimer = invincibilityDuration;
    }

    void Die()
    {
        isDead = true;

        if (deathSprite != null && sr != null)
        {
            sr.sprite = deathSprite;
            sr.enabled = true;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        var enemy = GetComponent<EnemyScript>();
        if (enemy != null) enemy.enabled = false;

        var player = GetComponent<PlayerScript>();
        if (player != null) player.enabled = false;

        onDeath?.Invoke();
        enabled = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryTakeEnemyDamage(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryTakeEnemyDamage(collision);
    }

    void TryTakeEnemyDamage(Collision2D collision)
    {
        if (!isPlayer) return;
        if (collision.gameObject.GetComponent<EnemyScript>() == null) return;

        // Immune while shadow swimming
        var shadow = GetComponent<ShadowDetector>();
        if (shadow != null && shadow.isShadowSwimming
            && shadow.stress < shadow.maxStressValue)
            return;

        TakeDamage(enemyContactDamage);
    }
}
