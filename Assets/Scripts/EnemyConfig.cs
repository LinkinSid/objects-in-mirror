using UnityEngine;

public enum DetectionMode
{
    VisionCone,
    Proximity
}

public enum WanderMode
{
    Random,
    Patrol
}

[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public Sprite sprite;
    public Sprite deathSprite;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float turnSpeed = 5f;
    public bool isStationary = false;
    public float sentrySweepAngle = 90f;
    public float sentrySweepSpeed = 30f;

    [Header("Detection")]
    public DetectionMode detectionMode = DetectionMode.VisionCone;
    public float visionRange = 6f;
    public float visionHalfAngle = 30f;
    public bool canSeeShadowSwimmer = false;
    public Color visionConeColor = Color.clear;

    [Header("Alert")]
    public float alertDuration = 2f;
    public float alertSpeedMultiplier = 1.5f;
    public bool alertsNearbyEnemies = false;
    public float alertBroadcastRadius = 8f;

    [Header("Pathfinding")]
    public float cellSize = 0.5f;
    public float pathRecalcInterval = 0.5f;
    public float gridBufferMultiplier = 1.8f;
    public float waypointReachDist = 0.3f;

    [Header("Wander")]
    public WanderMode wanderMode = WanderMode.Random;
    public float wanderRadius = 4f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float contactDamage = 15f;
    public float invincibilityDuration = 1.5f;

    [Header("Health Bar")]
    public Color healthBarColor = new Color(0.9f, 0.2f, 0.2f);
    public Color healthBarBgColor = new Color(0.2f, 0.2f, 0.2f);
    public float healthBarYOffset = 0.8f;
}
