using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Chase")]
    public float chaseSpeed = 6f;
    public float initialDelay = 2f;

    [Header("Danger Zones")]
    public float dangerZoneInterval = 3f;
    public int dangerZoneCount = 3;
    public float dangerZoneRadius = 1.5f;
    public float dangerZoneDamage = 20f;
    public float dangerZoneTelegraph = 1.5f;
    public float dangerZoneAheadDistance = 4f;
    public float dangerZoneSpread = 2f;

    [Header("Proximity Shake")]
    public float shakeMaxDistance = 10f;
    public float shakeMaxIntensity = 0.15f;

    [Header("Contact Damage")]
    public float contactDamage = 25f;
    public float contactCooldown = 1f;

    // State
    float dangerZoneTimer;
    float contactTimer;
    float delayTimer;
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

        delayTimer = initialDelay;
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
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0)
                chaseStarted = true;
            return;
        }

        UpdateChase();
        UpdateDangerZones();
        UpdateProximityShake();

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
            DangerZone.Spawn(
                aheadPos + offset, dangerZoneRadius, dangerZoneDamage,
                dangerZoneTelegraph, playerHealth
            );
        }

        if (cam != null)
            cam.Shake(0.1f, 0.2f);
    }

    void UpdateProximityShake()
    {
        if (cam == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < shakeMaxDistance)
        {
            float t = 1f - (dist / shakeMaxDistance);
            cam.Shake(t * t * shakeMaxIntensity, 0.05f);
        }
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
