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

    [Header("Health Regen (Player Only)")]
    public float regenRate = 2f;
    public float regenDelay = 5f;

    [Header("Death")]
    public Sprite deathSprite;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool invincible;

    private float invincibilityTimer;
    private float regenDelayTimer;
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
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;

            if (sr != null)
                sr.enabled = Mathf.FloorToInt(invincibilityTimer / flashInterval) % 2 == 0;

            if (invincibilityTimer <= 0f && sr != null)
                sr.enabled = true;
        }

        if (isPlayer && !isDead && currentHealth < maxHealth)
        {
            if (regenDelayTimer > 0f)
                regenDelayTimer -= Time.deltaTime;
            else
                currentHealth = Mathf.Min(currentHealth + regenRate * Time.deltaTime, maxHealth);
        }
    }

    public void GrantIFrames()
    {
        if (isDead || invincibilityTimer > 0f) return;
        if (invincibilityDuration > 0f)
            invincibilityTimer = invincibilityDuration;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || invincible || invincibilityTimer > 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        regenDelayTimer = regenDelay;

        if (isPlayer && AudioManager.Instance != null)
            AudioManager.Instance.PlayDamageSFX();

        if (currentHealth <= 0f)
            Die();
        else if (invincibilityDuration > 0f)
            invincibilityTimer = invincibilityDuration;
    }

    void Die()
    {
        isDead = true;

        if (AudioManager.Instance != null)
        {
            if (isPlayer)
                AudioManager.Instance.PlayDeathSFX();
            else if (GetComponent<BossController>() != null)
                AudioManager.Instance.PlayBossDeathSFX();
            else if (GetComponent<EnemyScript>() != null)
                AudioManager.Instance.PlayMonsterDeathSFX();
        }

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

        var boss = GetComponent<BossController>();
        if (boss != null) boss.enabled = false;

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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayAttackSFX();

        TakeDamage(enemyContactDamage);
    }
}
