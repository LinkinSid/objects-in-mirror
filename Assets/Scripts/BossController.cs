using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Chase")]
    public float chaseSpeed = 6f;
    [Header("Danger Zones")]
    public float dangerZoneInterval = 3f;
    public int dangerZoneCount = 3;
    public float dangerZoneRadius = 1.5f;
    public float dangerZoneDamage = 20f;
    public float dangerZoneTelegraph = 1.5f;
    public float dangerZoneAheadDistance = 2f;
    public float dangerZoneSpread = 2.5f;

    [Header("Proximity Shake")]
    public float shakeMaxDistance = 10f;
    public float shakeMaxIntensity = 0.15f;

    [Header("Contact Damage")]
    public float contactDamage = 25f;
    public float contactCooldown = 1f;

    // State
    float dangerZoneTimer;
    float contactTimer;
    bool chaseStarted;

    // Cached
    Rigidbody2D rb;
    CameraFollow cam;
    Health playerHealth;
    ShadowDetector playerShadow;
    Rigidbody2D playerRb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main.GetComponent<CameraFollow>();
        playerHealth = player.GetComponent<Health>();
        playerShadow = player.GetComponent<ShadowDetector>();
        playerRb = player.GetComponent<Rigidbody2D>();

        dangerZoneTimer = dangerZoneInterval;
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!chaseStarted)
        {
            // Start chase when boss is visible on camera
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            bool onScreen = viewPos.x >= 0f && viewPos.x <= 1f
                         && viewPos.y >= 0f && viewPos.y <= 1f
                         && viewPos.z > 0f;

            if (onScreen)
            {
                chaseStarted = true;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBossRoarSFX();
                    AudioManager.Instance.PlayBossMusic();
                }
            }
            return;
        }

        UpdateChase();
        UpdateDangerZones();

        if (contactTimer > 0)
            contactTimer -= Time.deltaTime;
    }

    void UpdateChase()
    {
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = dir * chaseSpeed;
    }

    void UpdateDangerZones()
    {
        dangerZoneTimer -= Time.deltaTime;
        if (dangerZoneTimer > 0) return;

        dangerZoneTimer = dangerZoneInterval;
        SpawnDangerZonesAhead();
    }

    void SpawnDangerZonesAhead()
    {
        Vector2 playerVel = playerRb != null ? playerRb.linearVelocity : Vector2.zero;
        Vector2 moveDir = playerVel.sqrMagnitude > 0.1f
            ? playerVel.normalized
            : ((Vector2)player.position - rb.position).normalized;

        Vector2 aheadPos = (Vector2)player.position + moveDir * dangerZoneAheadDistance;

        for (int i = 0; i < dangerZoneCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dangerZoneSpread;
            Vector2 spawnPos = aheadPos + offset;

            DangerZone.Spawn(
                spawnPos, dangerZoneRadius, dangerZoneDamage,
                dangerZoneTelegraph, playerHealth, cam
            );
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBossGruntSFX();

        if (cam != null)
            cam.Shake(0.1f, 0.2f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryContactDamage(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryContactDamage(collision);
    }

    void TryContactDamage(Collision2D collision)
    {
        if (contactTimer > 0) return;
        if (collision.gameObject != player.gameObject) return;

        // Immune while shadow swimming
        if (playerShadow != null && playerShadow.isShadowSwimming
            && playerShadow.stress < playerShadow.maxStressValue)
            return;

        if (playerHealth != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBossAttackSFX();

            playerHealth.TakeDamage(contactDamage);
            contactTimer = contactCooldown;

            if (cam != null)
                cam.Shake(0.3f, 0.3f);
        }
    }

    public void StopChase()
    {
        chaseStarted = false;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }
}
