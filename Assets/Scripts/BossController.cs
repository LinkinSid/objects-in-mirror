using System.Collections;
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

    [Header("Death Cutscene")]
    public float panSpeed = 2f;
    public float idleHoldTime = 0.5f;
    public float deathAnimDuration = 2.5f;
    public float panBackSpeed = 2f;

    // State
    float dangerZoneTimer;
    float contactTimer;
    bool chaseStarted;
    bool spawning;
    bool defeated;

    // Cached
    Rigidbody2D rb;
    CameraFollow cam;
    Health playerHealth;
    ShadowDetector playerShadow;
    Rigidbody2D playerRb;
    Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        cam = Camera.main.GetComponent<CameraFollow>();
        playerHealth = player.GetComponent<Health>();
        playerShadow = player.GetComponent<ShadowDetector>();
        playerRb = player.GetComponent<Rigidbody2D>();

        dangerZoneTimer = dangerZoneInterval;

        // Disable animator until boss appears on screen;
        // spawn is the default state so it plays when enabled
        if (animator != null)
            animator.enabled = false;
    }

    void Update()
    {
        if (defeated) return;

        if (playerHealth != null && playerHealth.isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (spawning) return;

        if (!chaseStarted)
        {
            // Start chase when boss is visible on camera
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            bool onScreen = viewPos.x >= 0f && viewPos.x <= 1f
                         && viewPos.y >= 0f && viewPos.y <= 1f
                         && viewPos.z > 0f;

            if (onScreen)
            {
                spawning = true;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBossRoarSFX();
                    AudioManager.Instance.PlayBossMusic();
                }

                // Enable animator — default state is boss_spawn
                if (animator != null)
                    animator.enabled = true;
            }
            return;
        }

        UpdateChase();
        UpdateDangerZones();

        if (contactTimer > 0)
            contactTimer -= Time.deltaTime;
    }

    // Called by Spawn animation event on the last frame
    public void OnSpawnComplete()
    {
        spawning = false;
        chaseStarted = true;
    }

    void UpdateChase()
    {
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = dir * chaseSpeed;

        // Track dominant direction for attack animation selection
        if (animator != null)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                animator.SetFloat("MoveX", Mathf.Sign(dir.x));
                animator.SetFloat("MoveY", 0f);
            }
            else
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", Mathf.Sign(dir.y));
            }
        }
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

            if (animator != null)
                animator.SetTrigger("Attack");

            if (cam != null)
                cam.Shake(0.3f, 0.3f);
        }
    }

    public void StopChase()
    {
        defeated = true;
        chaseStarted = false;
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(DeathCutsceneRoutine());
    }

    IEnumerator DeathCutsceneRoutine()
    {
        // 1. Freeze gameplay
        Time.timeScale = 0f;

        // 2. Let animator run during freeze
        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.Play("boss_idle", 0, 0f);
        }

        // 3. Pan camera to boss
        if (cam != null)
            cam.PanTo(transform.position, panSpeed);

        // 4. Wait for camera to arrive
        while (cam != null && !cam.HasReachedCinematicTarget())
            yield return null;

        // 5. Hold on idle boss briefly
        yield return new WaitForSecondsRealtime(idleHoldTime);

        // 6. Play death animation + SFX + shake
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBossDeathSFX();

        if (cam != null)
            cam.Shake(0.5f, 0.4f);

        if (animator != null)
            animator.Play("boss_death", 0, 0f);

        // 7. Wait for death animation to finish
        yield return new WaitForSecondsRealtime(deathAnimDuration);

        // 8. Pan camera back to player
        if (cam != null)
            cam.PanTo(player.position, panBackSpeed);

        // 9. Wait for camera to arrive at player
        while (cam != null && !cam.HasReachedCinematicTarget())
            yield return null;

        // 10. Resume gameplay
        if (cam != null)
            cam.ExitCinematic();

        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPostBossMusic();

        // 11. Deactivate boss
        gameObject.SetActive(false);
    }

    // Called by death animation event — no-op, coroutine handles deactivation
    public void OnDeathComplete() { }
}
