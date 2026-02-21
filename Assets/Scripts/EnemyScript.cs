using UnityEngine;
using System.Collections.Generic;

public class EnemyScript : MonoBehaviour
{
    public EnemyConfig config;
    public Transform player;
    public Collider2D backgroundCollider;
    public Transform[] patrolWaypoints;

    private float moveSpeed;
    private float turnSpeed;
    private float visionRange;
    private float visionHalfAngle;
    private float alertDuration;
    private float alertSpeedMultiplier;
    private float wanderRadius;
    private float cellSize;
    private float pathRecalcInterval;
    private float gridBufferMultiplier;
    private float waypointReachDist;
    private float passiveAwarenessRange;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Collider2D playerCollider;
    private ShadowDetector playerShadow;
    private Vector2 currentDir;

    // Alert state
    private bool isAlerted;
    private float alertTimer;
    private float baseMoveSpeed;
    private float basePathRecalcInterval;

    // Wander state
    private Vector2 wanderTarget;
    private bool hasWanderTarget;
    private int patrolIndex;

    // Sentry sweep
    private float sweepAngle;
    private int sweepDirection = 1;
    private Vector2 sweepCenterDir;
    private float sentryCheckTimer;

    // Cached health
    private Health myHealth;

    // Vision cone mesh
    private MeshFilter coneMeshFilter;
    private MeshRenderer coneMeshRenderer;
    private Mesh coneMesh;
    private Color coneColor;
    private Color coneAlertColor;
    private int coneSegments = 20;

    // Grid
    private bool[,] grid;
    private bool[,] trapGrid;
    private int gridWidth, gridHeight;
    private Vector2 gridOrigin;

    // Path
    private List<Vector2> path = new List<Vector2>();
    private int pathIndex;
    private float pathTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myHealth = GetComponent<Health>();
        myCollider = GetComponent<Collider2D>();
        playerCollider = player.GetComponent<Collider2D>();
        playerShadow = player.GetComponent<ShadowDetector>();

        // Apply config
        if (config != null)
        {
            moveSpeed = config.moveSpeed;
            turnSpeed = config.turnSpeed;
            visionRange = config.visionRange;
            visionHalfAngle = config.visionHalfAngle;
            alertDuration = config.alertDuration;
            alertSpeedMultiplier = config.alertSpeedMultiplier;
            wanderRadius = config.wanderRadius;
            cellSize = config.cellSize > 0 ? config.cellSize : 0.5f;
            pathRecalcInterval = config.pathRecalcInterval;
            gridBufferMultiplier = config.gridBufferMultiplier;
            waypointReachDist = config.waypointReachDist;
            passiveAwarenessRange = config.passiveAwarenessRange;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (config.sprite != null && sr != null)
                sr.sprite = config.sprite;

            if (config.visionConeColor.a > 0f && config.detectionMode == DetectionMode.VisionCone)
                BuildConeMesh();
        }
        else
        {
            // Defaults
            moveSpeed = 2f;
            turnSpeed = 3f;
            visionRange = 4f;
            visionHalfAngle = 45f;
            alertDuration = 0.75f;
            alertSpeedMultiplier = 1.5f;
            wanderRadius = 4f;
            cellSize = 0.5f;
            pathRecalcInterval = 0.5f;
            gridBufferMultiplier = 1.8f;
            waypointReachDist = 0.3f;
            passiveAwarenessRange = 50f;
        }

        currentDir = ((Vector2)player.position - rb.position).normalized;
        sweepCenterDir = currentDir;
        baseMoveSpeed = moveSpeed;
        basePathRecalcInterval = pathRecalcInterval;

        Invoke(nameof(BuildGrid), 0.1f);
    }

    void BuildGrid()
    {
        // Use the composite bounds
        CompositeCollider2D composite = backgroundCollider.GetComponent<CompositeCollider2D>();
        Bounds b = composite != null ? composite.bounds : backgroundCollider.bounds;
        Vector2 center = b.center;
        Vector2 size = b.size;

        gridOrigin = center - size / 2f;
        gridWidth = Mathf.CeilToInt(size.x / cellSize);
        gridHeight = Mathf.CeilToInt(size.y / cellSize);
        grid = new bool[gridWidth, gridHeight];
        trapGrid = new bool[gridWidth, gridHeight];

        float checkRadius = myCollider.bounds.extents.x * gridBufferMultiplier;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = GridToWorld(x, y);
                Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, checkRadius);
                bool walkable = true;
                bool hasTrap = false;
                foreach (Collider2D hit in hits)
                {
                    if (hit != myCollider && hit != playerCollider && hit.gameObject != backgroundCollider.gameObject && !hit.isTrigger)
                    {
                        walkable = false;
                        break;
                    }
                    if (hit.isTrigger && hit.GetComponent<Trap>() != null)
                        hasTrap = true;
                }
                grid[x, y] = walkable;
                trapGrid[x, y] = hasTrap;
            }
        }
    }

    bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
        if (!grid[x, y]) return false;
        if (!isAlerted && trapGrid[x, y]) return false;
        return true;
    }

    Vector2 GridToWorld(int x, int y)
    {
        return gridOrigin + new Vector2(x * cellSize + cellSize / 2f, y * cellSize + cellSize / 2f);
    }

    void WorldToGrid(Vector2 world, out int x, out int y)
    {
        x = Mathf.Clamp(Mathf.FloorToInt((world.x - gridOrigin.x) / cellSize), 0, gridWidth - 1);
        y = Mathf.Clamp(Mathf.FloorToInt((world.y - gridOrigin.y) / cellSize), 0, gridHeight - 1);
    }

    void Update()
    {
        if (grid == null) return;

        // Sentry: periodically check if all other enemies are dead
        if (config != null && config.isStationary)
        {
            sentryCheckTimer -= Time.deltaTime;
            if (sentryCheckTimer <= 0f)
            {
                sentryCheckTimer = 1f;
                bool anyAlive = false;
                foreach (EnemyScript other in FindObjectsByType<EnemyScript>(FindObjectsSortMode.None))
                {
                    if (other == this || !other.enabled) continue;
                    if (other.config != null && other.config.isStationary) continue;
                    anyAlive = true;
                    break;
                }
                if (!anyAlive)
                {
                    SpriteRenderer sr = GetComponent<SpriteRenderer>();
                    if (config.deathSprite != null && sr != null)
                        sr.sprite = config.deathSprite;

                    enabled = false;
                    return;
                }
            }
        }

        // Sentry: ping-pong sweep the vision cone
        if (config != null && config.isStationary && !isAlerted)
        {
            sweepAngle += sweepDirection * config.sentrySweepSpeed * Time.deltaTime;
            float halfSweep = config.sentrySweepAngle / 2f;

            if (sweepAngle >= halfSweep)
            {
                sweepAngle = halfSweep;
                sweepDirection = -1;
            }
            else if (sweepAngle <= -halfSweep)
            {
                sweepAngle = -halfSweep;
                sweepDirection = 1;
            }

            float rad = sweepAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            currentDir = new Vector2(
                sweepCenterDir.x * cos - sweepCenterDir.y * sin,
                sweepCenterDir.x * sin + sweepCenterDir.y * cos
            ).normalized;
        }

        if (CanSeePlayer())
        {
            if (!isAlerted)
            {
                isAlerted = true;
                moveSpeed = baseMoveSpeed * alertSpeedMultiplier;
                pathRecalcInterval = basePathRecalcInterval / 2f;
                RecalculatePath();
                pathTimer = pathRecalcInterval;
            }
            alertTimer = alertDuration;
        }

        // Alert cooldown
        if (isAlerted)
        {
            alertTimer -= Time.deltaTime;
            if (alertTimer <= 0f)
            {
                isAlerted = false;
                moveSpeed = baseMoveSpeed;
                pathRecalcInterval = basePathRecalcInterval;
            }
        }

        // Sentry: alert nearby enemies
        if (isAlerted && config != null && config.alertsNearbyEnemies)
            AlertNearbyEnemies();

        pathTimer -= Time.deltaTime;
        if (pathTimer <= 0f)
        {
            RecalculatePath();
            pathTimer = pathRecalcInterval;
        }

        UpdateConeMesh();
    }

    bool CanSeePlayer()
    {
        // Shadow visibility check
        if (config != null && config.canSeeShadowSwimmer)
        {
            // Can see shadow swimmers, but not players in light
            // Actually, this enemy sees everyone shadow swimming doesn't help
        }
        else
        {
            if (IsPlayerHidden())
                return false;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;

        if (distance > visionRange)
            return false;

        // Proximity mode: skip angle check (360 awareness)
        if (config == null || config.detectionMode == DetectionMode.VisionCone)
        {
            float angle = Vector2.Angle(currentDir, toPlayer);
            if (angle > visionHalfAngle)
                return false;
        }

        // Raycast LOS check (shared by all modes)
        Vector2 dir = toPlayer.normalized;
        float skinOffset = 0.1f;
        RaycastHit2D hit = Physics2D.Raycast(rb.position + dir * skinOffset, dir, distance - skinOffset);
        if (hit.collider != null && hit.collider != myCollider && hit.collider != playerCollider && !hit.collider.isTrigger)
            return false;

        return true;
    }

    bool IsPlayerHidden()
    {
        if (playerShadow == null || !playerShadow.isShadowSwimming)
            return false;

        // Full stress reveals the player even in shadows
        return playerShadow.stress < playerShadow.maxStressValue;
    }

    void AlertNearbyEnemies()
    {
        float radius = config.alertBroadcastRadius;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(rb.position, radius);
        foreach (Collider2D col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            EnemyScript other = col.GetComponent<EnemyScript>();
            if (other != null && !other.isAlerted)
                other.ReceiveAlert();
        }
    }

    public void ReceiveAlert()
    {
        isAlerted = true;
        alertTimer = alertDuration;
        moveSpeed = baseMoveSpeed * alertSpeedMultiplier;
        pathRecalcInterval = basePathRecalcInterval / 2f;
        RecalculatePath();
        pathTimer = pathRecalcInterval;
    }

    void RecalculatePath()
    {
        if (config != null && config.isStationary) return;

        WorldToGrid(rb.position, out int startX, out int startY);

        if (!IsWalkable(startX, startY))
            FindNearestWalkable(startX, startY, out startX, out startY);

        int endX, endY;

        if (isAlerted)
        {
            hasWanderTarget = false;
            WorldToGrid(player.position, out endX, out endY);
        }
        else if (config != null && config.wanderMode == WanderMode.Patrol
            && patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            // Patrol waypoints: always follow when not alerted
            if (Vector2.Distance(rb.position, patrolWaypoints[patrolIndex].position) < waypointReachDist)
                patrolIndex = (patrolIndex + 1) % patrolWaypoints.Length;

            WorldToGrid(patrolWaypoints[patrolIndex].position, out endX, out endY);
        }
        else if (IsPlayerHidden())
        {
            // Random wander: only when player is hidden
            if (!hasWanderTarget || Vector2.Distance(rb.position, wanderTarget) < waypointReachDist)
                PickWanderTarget();

            WorldToGrid(wanderTarget, out endX, out endY);
        }
        else if (Vector2.Distance(rb.position, player.position) <= passiveAwarenessRange)
        {
            hasWanderTarget = false;
            WorldToGrid(player.position, out endX, out endY);
        }
        else
        {
            if (!hasWanderTarget || Vector2.Distance(rb.position, wanderTarget) < waypointReachDist)
                PickWanderTarget();

            WorldToGrid(wanderTarget, out endX, out endY);
        }

        if (!IsWalkable(endX, endY))
            FindNearestWalkable(endX, endY, out endX, out endY);

        path = FindPath(startX, startY, endX, endY);
        pathIndex = 0;
    }

    void PickWanderTarget()
    {
        // Bias away from the player's last known position
        Vector2 awayDir = (rb.position - (Vector2)player.position).normalized;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Offset mostly away from the player with some randomness
            Vector2 offset = (awayDir + Random.insideUnitCircle * 0.5f).normalized * Random.Range(wanderRadius * 0.5f, wanderRadius);
            Vector2 candidate = rb.position + offset;

            WorldToGrid(candidate, out int cx, out int cy);
            if (cx >= 0 && cx < gridWidth && cy >= 0 && cy < gridHeight && IsWalkable(cx, cy))
            {
                wanderTarget = GridToWorld(cx, cy);
                hasWanderTarget = true;
                return;
            }
        }
    }

    void FindNearestWalkable(int cx, int cy, out int rx, out int ry)
    {
        for (int r = 1; r < Mathf.Max(gridWidth, gridHeight); r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight && IsWalkable(nx, ny))
                    {
                        rx = nx;
                        ry = ny;
                        return;
                    }
                }
            }
        }
        rx = cx;
        ry = cy;
    }

    void FixedUpdate()
    {
        if (config != null && config.isStationary)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (grid == null || path.Count == 0)
            return;

        if (pathIndex < path.Count && Vector2.Distance(rb.position, path[pathIndex]) < waypointReachDist)
            pathIndex++;

        bool wandering = !isAlerted && (IsPlayerHidden()
            || (config != null && config.wanderMode == WanderMode.Patrol
                && patrolWaypoints != null && patrolWaypoints.Length > 0));
        float speed = wandering ? baseMoveSpeed : moveSpeed;

        Vector2 targetDir;
        if (pathIndex < path.Count)
        {
            targetDir = (path[pathIndex] - rb.position).normalized;
            // Smooth turning while wandering, snap while chasing
            if (wandering)
                currentDir = Vector2.Lerp(currentDir, targetDir, turnSpeed * Time.fixedDeltaTime).normalized;
            else
                currentDir = targetDir;
        }
        else if (!wandering)
        {
            // Smooth turn toward player only when chasing
            targetDir = ((Vector2)player.position - rb.position).normalized;
            currentDir = Vector2.Lerp(currentDir, targetDir, turnSpeed * Time.fixedDeltaTime).normalized;
        }
        else
        {
            // Reached wander target, immediately pick a new one
            hasWanderTarget = false;
            RecalculatePath();
            pathTimer = pathRecalcInterval;
            if (path.Count == 0) return;
            currentDir = (path[0] - rb.position).normalized;
        }

        rb.linearVelocity = currentDir * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        NudgeAndRepath(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        NudgeAndRepath(collision);
    }

    void NudgeAndRepath(Collision2D collision)
    {
        if (grid == null) return;

        // Push away from the obstacle using the contact normal
        Vector2 pushDir = Vector2.zero;
        foreach (ContactPoint2D contact in collision.contacts)
            pushDir += contact.normal;
        if (pushDir.sqrMagnitude > 0f)
            rb.position += pushDir.normalized * 0.05f;

        RecalculatePath();
        pathTimer = pathRecalcInterval;

        if (path.Count > 0 && pathIndex < path.Count)
            currentDir = (path[pathIndex] - rb.position).normalized;
    }

    // A* pathfinding
    List<Vector2> FindPath(int startX, int startY, int endX, int endY)
    {
        if (gridWidth == 0 || gridHeight == 0)
            return new List<Vector2>();

        startX = Mathf.Clamp(startX, 0, gridWidth - 1);
        startY = Mathf.Clamp(startY, 0, gridHeight - 1);
        endX = Mathf.Clamp(endX, 0, gridWidth - 1);
        endY = Mathf.Clamp(endY, 0, gridHeight - 1);

        int[,] gCost = new int[gridWidth, gridHeight];
        int[,] fCost = new int[gridWidth, gridHeight];
        int[,] parentX = new int[gridWidth, gridHeight];
        int[,] parentY = new int[gridWidth, gridHeight];
        bool[,] closed = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                gCost[x, y] = int.MaxValue;

        gCost[startX, startY] = 0;
        fCost[startX, startY] = Heuristic(startX, startY, endX, endY);
        parentX[startX, startY] = -1;
        parentY[startX, startY] = -1;

        List<Vector2Int> open = new List<Vector2Int>();
        open.Add(new Vector2Int(startX, startY));

        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] cost = { 10, 14, 10, 14, 10, 14, 10, 14 }; // 10 = cardinal, 14 = diagonal (~sqrt(2)*10)

        while (open.Count > 0)
        {
            // Find node with lowest fCost
            int bestIdx = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (fCost[open[i].x, open[i].y] < fCost[open[bestIdx].x, open[bestIdx].y])
                    bestIdx = i;
            }

            Vector2Int current = open[bestIdx];
            open.RemoveAt(bestIdx);

            if (current.x == endX && current.y == endY)
                return ReconstructPath(parentX, parentY, endX, endY);

            closed[current.x, current.y] = true;

            for (int i = 0; i < 8; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];

                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                    continue;
                if (!IsWalkable(nx, ny) || closed[nx, ny])
                    continue;

                // For diagonal movement, check that both cardinal neighbors are walkable
                if (cost[i] == 14)
                {
                    if (!IsWalkable(current.x + dx[i], current.y) || !IsWalkable(current.x, current.y + dy[i]))
                        continue;
                }

                int newG = gCost[current.x, current.y] + cost[i];
                if (newG < gCost[nx, ny])
                {
                    gCost[nx, ny] = newG;
                    fCost[nx, ny] = newG + Heuristic(nx, ny, endX, endY);
                    parentX[nx, ny] = current.x;
                    parentY[nx, ny] = current.y;

                    if (!open.Contains(new Vector2Int(nx, ny)))
                        open.Add(new Vector2Int(nx, ny));
                }
            }
        }

        return new List<Vector2>(); // No path found
    }

    int Heuristic(int ax, int ay, int bx, int by)
    {
        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);
        return 10 * (dx + dy) + (14 - 2 * 10) * Mathf.Min(dx, dy);
    }

    List<Vector2> ReconstructPath(int[,] parentX, int[,] parentY, int endX, int endY)
    {
        List<Vector2> result = new List<Vector2>();
        int cx = endX, cy = endY;

        while (parentX[cx, cy] != -1)
        {
            result.Add(GridToWorld(cx, cy));
            int px = parentX[cx, cy];
            int py = parentY[cx, cy];
            cx = px;
            cy = py;
        }

        result.Reverse();
        return result;
    }

    void BuildConeMesh()
    {
        coneColor = config.visionConeColor;
        coneAlertColor = new Color(1f, 0f, 0f, coneColor.a);

        var coneGo = new GameObject("VisionCone");
        coneGo.transform.SetParent(transform);
        coneGo.transform.localPosition = Vector3.zero;
        coneGo.transform.localRotation = Quaternion.identity;

        coneMeshFilter = coneGo.AddComponent<MeshFilter>();
        coneMeshRenderer = coneGo.AddComponent<MeshRenderer>();

        coneMesh = new Mesh();
        coneMeshFilter.mesh = coneMesh;

        // Unlit transparent material
        coneMeshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        coneMeshRenderer.sortingOrder = 2;
    }

    void UpdateConeMesh()
    {
        if (coneMesh == null) return;

        if (myHealth != null && myHealth.isDead)
        {
            Destroy(coneMeshFilter.gameObject);
            coneMesh = null;
            return;
        }

        Vector2 origin = rb.position;
        Vector2 forward = currentDir.sqrMagnitude > 0f ? currentDir.normalized : Vector2.up;

        float halfRad = visionHalfAngle * Mathf.Deg2Rad;
        float startAngle = Mathf.Atan2(forward.y, forward.x) - halfRad;
        float endAngle = Mathf.Atan2(forward.y, forward.x) + halfRad;

        // Vertices: origin + arc points
        var verts = new Vector3[coneSegments + 2];
        verts[0] = Vector3.zero; // local origin

        for (int i = 0; i <= coneSegments; i++)
        {
            float t = (float)i / coneSegments;
            float a = Mathf.Lerp(startAngle, endAngle, t);
            Vector2 worldPoint = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * visionRange;
            verts[i + 1] = transform.InverseTransformPoint(worldPoint);
        }

        // Triangles: fan from origin
        var tris = new int[coneSegments * 3];
        for (int i = 0; i < coneSegments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        // Color
        Color col = isAlerted ? coneAlertColor : coneColor;
        var colors = new Color[verts.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = col;

        coneMesh.Clear();
        coneMesh.vertices = verts;
        coneMesh.triangles = tris;
        coneMesh.colors = colors;
    }

    // Only for debug
    void OnDrawGizmos()
    {
        if (rb == null) return;

        Vector2 origin = rb.position;
        Vector2 forward = currentDir.sqrMagnitude > 0f ? currentDir.normalized : Vector2.up;

        float halfRad = visionHalfAngle * Mathf.Deg2Rad;
        Vector2 leftDir = new Vector2(
            forward.x * Mathf.Cos(halfRad) - forward.y * Mathf.Sin(halfRad),
            forward.x * Mathf.Sin(halfRad) + forward.y * Mathf.Cos(halfRad)
        );
        Vector2 rightDir = new Vector2(
            forward.x * Mathf.Cos(-halfRad) - forward.y * Mathf.Sin(-halfRad),
            forward.x * Mathf.Sin(-halfRad) + forward.y * Mathf.Cos(-halfRad)
        );

        Gizmos.color = isAlerted ? Color.red : Color.yellow;

        // Draw the two edge lines
        Gizmos.DrawLine(origin, origin + leftDir * visionRange);
        Gizmos.DrawLine(origin, origin + rightDir * visionRange);

        // Draw the arc
        int segments = 20;
        float startAngle = Mathf.Atan2(rightDir.y, rightDir.x);
        float endAngle = Mathf.Atan2(leftDir.y, leftDir.x);
        if (endAngle < startAngle) endAngle += 2f * Mathf.PI;

        Vector2 prev = origin + rightDir * visionRange;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = Mathf.Lerp(startAngle, endAngle, t);
            Vector2 point = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * visionRange;
            Gizmos.DrawLine(prev, point);
            prev = point;
        }
    }
}
