using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class BossController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public BoxCollider2D arenaCollider;

    [Header("Fist (child object with SpriteRenderer + ShadowCaster2D)")]
    public SpriteRenderer fistRenderer;
    public ShadowCaster2D fistShadowCaster;

    [Header("Chase")]
    public float chaseSpeed = 2f;
    public float initialDelay = 2f;

    [Header("Slam Attack")]
    public float slamTelegraphTime = 1.5f;
    public float slamRadius = 5f;
    public float slamSafeRadius = 1.5f;
    public float slamDamage = 30f;

    [Header("Roar Attack")]
    public float roarTelegraphTime = 2f;
    public int roarZoneCount = 3;
    public float roarZoneRadius = 1.5f;
    public float roarDamage = 20f;
    public float revealDuration = 5f;

    [Header("Timing")]
    public float attackCooldown = 4f;
    public float recoverTime = 1f;
    [Range(0f, 1f)] public float slamChance = 0.7f;

    [Header("Screen Shake")]
    public float slamShakeIntensity = 0.3f;
    public float slamShakeDuration = 0.3f;

    [Header("Phase Thresholds")]
    [Range(0f, 1f)] public float phase2Threshold = 0.66f;
    [Range(0f, 1f)] public float phase3Threshold = 0.33f;

    [Header("Phase 2 - Survival")]
    public GameObject[] minionPrefabs;
    public int minionsPerWave = 2;
    public float minionSpawnInterval = 12f;
    public float minionSpawnRadius = 3f;
    public float phase2Duration = 60f;
    public float damagePerMinionKill = 15f;

    [Header("Phase 3 - Blackout")]
    public float lightDestroyInterval = 0.3f;
    public float phase3ChaseSpeed = 3f;
    public float phase3AttackCooldown = 2.5f;

    // State machine
    enum Phase { One, Two, Three }
    enum State { Chasing, MovingToCenter, Stationary, TelegraphSlam, TelegraphRoar, Recovering, DestroyingLights }
    Phase currentPhase = Phase.One;
    State state;
    float stateTimer;
    float cooldownTimer;

    // Cached references
    Rigidbody2D rb;
    Collider2D bossCollider;
    SpriteRenderer bossSr;
    ShadowDetector playerShadow;
    Health playerHealth;
    CameraFollow cam;
    Health bossHealth;

    // Slam indicators
    GameObject slamDangerRing;
    GameObject slamSafeZone;
    static Sprite circleSprite;

    // Reveal tracking
    [HideInInspector] public bool playerRevealed;
    float revealTimer;

    // Phase 2 tracking
    Vector2 arenaCenter;
    float minionSpawnTimer;
    float phase2Timer;
    bool waitingForMinionClear;
    List<Health> spawnedMinions = new List<Health>();

    // Phase 3 tracking
    List<Light2D> arenaLights = new List<Light2D>();
    float lightDestroySequenceTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        bossSr = GetComponent<SpriteRenderer>();
        bossHealth = GetComponent<Health>();
        playerShadow = player.GetComponent<ShadowDetector>();
        playerHealth = player.GetComponent<Health>();
        cam = Camera.main.GetComponent<CameraFollow>();

        SetFistActive(false);

        if (playerShadow != null)
            playerShadow.RefreshCaches();

        // Cache arena lights (exclude any parented to the boss)
        foreach (var light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.GetComponentInParent<BossController>() == null)
                arenaLights.Add(light);
        }

        state = State.Chasing;
        cooldownTimer = initialDelay;
    }

    void Update()
    {
        if (bossHealth != null && bossHealth.isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckPhaseTransitions();
        UpdateReveal();

        if (currentPhase == Phase.Two)
        {
            CheckMinionDeaths();

            if (waitingForMinionClear)
            {
                // All minions cleared — now transition to Phase 3
                if (spawnedMinions.Count == 0)
                    TransitionToPhase3();
            }
            else
            {
                // Spawn minions on a continuous timer (ticks during attacks too)
                if (minionPrefabs != null && minionPrefabs.Length > 0 && state != State.MovingToCenter)
                {
                    minionSpawnTimer -= Time.deltaTime;
                    if (minionSpawnTimer <= 0)
                    {
                        SpawnMinions();
                        minionSpawnTimer = minionSpawnInterval;
                    }
                }
            }
        }

        switch (state)
        {
            case State.Chasing:          UpdateChasing(); break;
            case State.MovingToCenter:   UpdateMovingToCenter(); break;
            case State.Stationary:       UpdateStationary(); break;
            case State.TelegraphSlam:    UpdateTelegraphSlam(); break;
            case State.TelegraphRoar:    UpdateTelegraphRoar(); break;
            case State.Recovering:       UpdateRecovering(); break;
            case State.DestroyingLights: UpdateDestroyingLights(); break;
        }
    }

    // ─── PHASE TRANSITIONS ───

    void CheckPhaseTransitions()
    {
        if (bossHealth == null) return;
        float healthPercent = bossHealth.currentHealth / bossHealth.maxHealth;

        if (currentPhase == Phase.One && healthPercent <= phase2Threshold)
            TransitionToPhase2();
        else if (currentPhase == Phase.Two && healthPercent <= phase3Threshold && !waitingForMinionClear)
            waitingForMinionClear = true;
    }

    void TransitionToPhase2()
    {
        currentPhase = Phase.Two;
        rb.linearVelocity = Vector2.zero;

        // Clean up any in-progress attack
        if (slamDangerRing != null) Destroy(slamDangerRing);
        if (slamSafeZone != null) Destroy(slamSafeZone);
        SetFistActive(false);

        if (cam != null)
            cam.Shake(slamShakeIntensity * 2f, 0.6f);

        // Compute arena center
        if (arenaCollider != null)
            arenaCenter = (Vector2)arenaCollider.transform.position + arenaCollider.offset;
        else
            arenaCenter = transform.position;

        if (bossHealth != null)
            bossHealth.invincible = true;

        // Phase through obstacles
        if (bossCollider != null)
            bossCollider.enabled = false;

        state = State.MovingToCenter;
    }

    void BeginStationary()
    {
        if (bossHealth != null)
            bossHealth.invincible = false;

        // Restore collider and sprite after phasing
        if (bossCollider != null)
            bossCollider.enabled = true;
        if (bossSr != null)
            bossSr.enabled = true;

        minionSpawnTimer = minionSpawnInterval * 0.5f;
        phase2Timer = phase2Duration;
        cooldownTimer = attackCooldown;

        state = State.Stationary;
    }

    void TransitionToPhase3()
    {
        currentPhase = Phase.Three;
        rb.linearVelocity = Vector2.zero;

        // Clean up any in-progress attack
        if (slamDangerRing != null) Destroy(slamDangerRing);
        if (slamSafeZone != null) Destroy(slamSafeZone);
        SetFistActive(false);

        if (bossHealth != null)
            bossHealth.invincible = true;

        // Big shake to signal the transition
        if (cam != null)
            cam.Shake(slamShakeIntensity * 2.5f, 0.8f);

        lightDestroySequenceTimer = lightDestroyInterval;
        state = State.DestroyingLights;
    }

    // ─── CHASE (Phase 1 & 3) ───

    void UpdateChasing()
    {
        float speed = currentPhase == Phase.Three ? phase3ChaseSpeed : chaseSpeed;
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = dir * speed;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0)
        {
            rb.linearVelocity = Vector2.zero;

            if (Random.value < slamChance)
                BeginTelegraphSlam();
            else
                BeginTelegraphRoar();
        }
    }

    // ─── MOVE TO CENTER (Phase 2 transition) ───

    void UpdateMovingToCenter()
    {
        // Invincibility flash
        if (bossSr != null)
            bossSr.enabled = Mathf.FloorToInt(Time.time / 0.1f) % 2 == 0;

        Vector2 dir = (arenaCenter - rb.position).normalized;
        float dist = Vector2.Distance(rb.position, arenaCenter);

        if (dist < 0.3f)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = arenaCenter;
            BeginStationary();
            return;
        }

        rb.linearVelocity = dir * chaseSpeed * 1.5f;
    }

    // ─── STATIONARY (Phase 2) ───

    void UpdateStationary()
    {
        rb.linearVelocity = Vector2.zero;

        // Survival timer — if it runs out, force transition to Phase 3
        phase2Timer -= Time.deltaTime;
        if (phase2Timer <= 0)
        {
            // Force boss HP to the Phase 3 threshold
            float targetHp = bossHealth.maxHealth * phase3Threshold;
            if (bossHealth.currentHealth > targetHp)
                bossHealth.TakeDamage(bossHealth.currentHealth - targetHp);
            return; // CheckPhaseTransitions will trigger Phase 3
        }

        // Attack cooldown
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0)
        {
            if (Random.value < slamChance)
                BeginTelegraphSlam();
            else
                BeginTelegraphRoar();
            return;
        }

    }

    void CheckMinionDeaths()
    {
        for (int i = spawnedMinions.Count - 1; i >= 0; i--)
        {
            if (spawnedMinions[i] == null || spawnedMinions[i].isDead)
            {
                spawnedMinions.RemoveAt(i);

                // Don't damage boss once the 33% threshold is reached
                if (!waitingForMinionClear && bossHealth != null)
                    bossHealth.TakeDamage(damagePerMinionKill);
            }
        }
    }

    // ─── DESTROYING LIGHTS (Phase 3 intro) ───

    void UpdateDestroyingLights()
    {
        rb.linearVelocity = Vector2.zero;

        lightDestroySequenceTimer -= Time.deltaTime;
        if (lightDestroySequenceTimer > 0) return;

        // Clean out already-dead lights
        arenaLights.RemoveAll(l => l == null || !l.enabled);

        if (arenaLights.Count > 0)
        {
            // Destroy one light
            int idx = Random.Range(0, arenaLights.Count);
            arenaLights[idx].enabled = false;
            arenaLights.RemoveAt(idx);

            if (cam != null)
                cam.Shake(slamShakeIntensity * 1.5f, 0.4f);

            if (playerShadow != null)
                playerShadow.RefreshCaches();

            lightDestroySequenceTimer = lightDestroyInterval;
        }
        else
        {
            // All lights out — begin the blackout chase
            if (bossHealth != null)
                bossHealth.invincible = false;

            if (cam != null)
                cam.Shake(slamShakeIntensity * 3f, 1f);

            cooldownTimer = attackCooldown;
            state = State.Chasing;
        }
    }

    // ─── SLAM ───

    void BeginTelegraphSlam()
    {
        state = State.TelegraphSlam;
        stateTimer = slamTelegraphTime;
        SetFistActive(true);
        fistRenderer.transform.localScale = Vector3.one * 0.3f;

        slamDangerRing = CreateIndicator(
            transform.position, slamRadius * 2f,
            new Color(1f, 0.15f, 0f, 0.1f), 3);

        slamSafeZone = CreateIndicator(
            fistRenderer.transform.position, slamSafeRadius * 2f,
            new Color(0f, 0.9f, 0.4f, 0.15f), 4);
    }

    void UpdateTelegraphSlam()
    {
        stateTimer -= Time.deltaTime;
        float progress = 1f - (stateTimer / slamTelegraphTime);

        float scale = Mathf.Lerp(0.3f, 1f, progress);
        fistRenderer.transform.localScale = Vector3.one * scale;

        if (slamDangerRing != null)
        {
            var sr = slamDangerRing.GetComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.15f, 0f, Mathf.Lerp(0.1f, 0.4f, progress));
            slamDangerRing.transform.position = transform.position;
        }

        if (slamSafeZone != null)
        {
            var sr = slamSafeZone.GetComponent<SpriteRenderer>();
            sr.color = new Color(0f, 0.9f, 0.4f, Mathf.Lerp(0.15f, 0.5f, progress));
            slamSafeZone.transform.position = fistRenderer.transform.position;
        }

        if (cam != null)
            cam.Shake(slamShakeIntensity * 0.3f * progress, 0.05f);

        if (stateTimer <= 0)
            ExecuteSlam();
    }

    void ExecuteSlam()
    {
        if (cam != null)
            cam.Shake(slamShakeIntensity, slamShakeDuration);

        float distToSlam = Vector2.Distance(transform.position, player.position);
        if (distToSlam <= slamRadius)
        {
            float distToSafe = Vector2.Distance(fistRenderer.transform.position, player.position);
            bool inSafeZone = distToSafe <= slamSafeRadius;
            bool canDodge = inSafeZone
                && playerShadow != null
                && playerShadow.swimHeld
                && playerShadow.stress < playerShadow.maxStressValue;

            if (!canDodge)
                playerHealth.TakeDamage(slamDamage);
        }

        SetFistActive(false);
        if (slamDangerRing != null) Destroy(slamDangerRing);
        if (slamSafeZone != null) Destroy(slamSafeZone);
        EnterRecovery();
    }

    // ─── ROAR ───

    void BeginTelegraphRoar()
    {
        state = State.TelegraphRoar;
        stateTimer = roarTelegraphTime;
        SpawnDangerZones();
    }

    void UpdateTelegraphRoar()
    {
        stateTimer -= Time.deltaTime;

        float progress = 1f - (stateTimer / roarTelegraphTime);
        if (cam != null)
            cam.Shake(slamShakeIntensity * 0.5f * progress, 0.05f);

        if (stateTimer <= 0)
            ExecuteRoar();
    }

    void ExecuteRoar()
    {
        if (cam != null)
            cam.Shake(slamShakeIntensity, slamShakeDuration);

        EnterRecovery();
    }

    void SpawnDangerZones()
    {
        // More zones in later phases
        int count = currentPhase == Phase.One ? roarZoneCount : roarZoneCount + 2;

        Bounds bounds = arenaCollider != null
            ? arenaCollider.bounds
            : new Bounds(transform.position, Vector3.one * 100f);

        for (int i = 0; i < count; i++)
        {
            Vector2 basePos = player.position;
            Vector2 offset = Random.insideUnitCircle * (slamRadius * 0.8f);
            Vector2 zonePos = basePos + offset;

            // Clamp so the entire circle stays inside the arena
            zonePos.x = Mathf.Clamp(zonePos.x, bounds.min.x + roarZoneRadius, bounds.max.x - roarZoneRadius);
            zonePos.y = Mathf.Clamp(zonePos.y, bounds.min.y + roarZoneRadius, bounds.max.y - roarZoneRadius);

            DangerZone.Spawn(
                zonePos, roarZoneRadius, roarDamage,
                roarTelegraphTime, revealDuration,
                playerHealth, this
            );
        }
    }

    // ─── RECOVERY ───

    void EnterRecovery()
    {
        state = State.Recovering;
        stateTimer = recoverTime;
    }

    void UpdateRecovering()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            float cd = currentPhase == Phase.Three ? phase3AttackCooldown : attackCooldown;
            cooldownTimer = cd;

            if (currentPhase == Phase.Two)
                state = State.Stationary;
            else
                state = State.Chasing;
        }
    }

    // ─── PHASE 2 MECHANICS ───

    void SpawnMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        if (cam != null)
            cam.Shake(0.1f, 0.2f);

        for (int i = 0; i < minionsPerWave; i++)
        {
            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * minionSpawnRadius;
            var minion = Instantiate(prefab, spawnPos, Quaternion.identity);

            var enemy = minion.GetComponent<EnemyScript>();
            if (enemy != null)
            {
                enemy.player = player;
                if (arenaCollider != null)
                    enemy.backgroundCollider = arenaCollider;
            }

            var health = minion.GetComponent<Health>();
            if (health != null)
                spawnedMinions.Add(health);
        }

        if (playerShadow != null)
            playerShadow.RefreshCaches();
    }

    // ─── REVEAL ───

    void UpdateReveal()
    {
        if (!playerRevealed) return;
        revealTimer -= Time.deltaTime;
        if (revealTimer <= 0) playerRevealed = false;
    }

    public void RevealPlayer(float duration)
    {
        playerRevealed = true;
        revealTimer = duration;
    }

    // ─── HELPERS ───

    void SetFistActive(bool active)
    {
        if (fistRenderer != null) fistRenderer.enabled = active;
        if (fistShadowCaster != null) fistShadowCaster.enabled = active;
    }

    GameObject CreateIndicator(Vector2 position, float diameter, Color color, int sortOrder)
    {
        var go = new GameObject("SlamIndicator");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * diameter;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = color;
        sr.sortingOrder = sortOrder;
        return go;
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

    // ─── GIZMOS ───

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);

        Gizmos.color = new Color(0, 1, 0.4f, 0.3f);
        if (fistRenderer != null)
            Gizmos.DrawWireSphere(fistRenderer.transform.position, slamSafeRadius);
    }
}
