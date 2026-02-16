using UnityEngine;
using System.Collections.Generic;

public class EnemyScript : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float turnSpeed = 5f;
    public Transform player;
    public BoxCollider2D backgroundCollider;
    public float cellSize = 0.5f;
    public float pathRecalcInterval = 0.5f;
    public float gridBufferMultiplier = 1.8f;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Collider2D playerCollider;
    private Vector2 currentDir;

    // Grid
    private bool[,] grid;
    private int gridWidth, gridHeight;
    private Vector2 gridOrigin;

    // Path
    private List<Vector2> path = new List<Vector2>();
    private int pathIndex;
    private float pathTimer;
    private float waypointReachDist = 0.3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        playerCollider = player.GetComponent<Collider2D>();
        currentDir = ((Vector2)player.position - rb.position).normalized;

        Invoke(nameof(BuildGrid), 0.1f);
    }

    void BuildGrid()
    {
        Vector2 center = (Vector2)backgroundCollider.transform.position + backgroundCollider.offset;
        Vector2 size = backgroundCollider.size;

        gridOrigin = center - size / 2f;
        gridWidth = Mathf.CeilToInt(size.x / cellSize);
        gridHeight = Mathf.CeilToInt(size.y / cellSize);
        grid = new bool[gridWidth, gridHeight];

        float checkRadius = myCollider.bounds.extents.x * gridBufferMultiplier;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = GridToWorld(x, y);
                Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, checkRadius);
                bool walkable = true;
                foreach (Collider2D hit in hits)
                {
                    if (hit != myCollider && hit != playerCollider && !hit.isTrigger)
                    {
                        walkable = false;
                        break;
                    }
                }
                grid[x, y] = walkable;
            }
        }
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

        pathTimer -= Time.deltaTime;
        if (pathTimer <= 0f)
        {
            RecalculatePath();
            pathTimer = pathRecalcInterval;
        }
    }

    void RecalculatePath()
    {
        WorldToGrid(rb.position, out int startX, out int startY);
        WorldToGrid(player.position, out int endX, out int endY);

        // If enemy is inside a blocked cell, find nearest walkable cell
        if (!grid[startX, startY])
            FindNearestWalkable(startX, startY, out startX, out startY);

        // If player is inside a blocked cell (also near walls/corners), path to nearest walkable cell
        if (!grid[endX, endY])
            FindNearestWalkable(endX, endY, out endX, out endY);

        path = FindPath(startX, startY, endX, endY);
        pathIndex = 0;
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
                    if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight && grid[nx, ny])
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
        if (grid == null || path.Count == 0)
            return;

        if (pathIndex < path.Count && Vector2.Distance(rb.position, path[pathIndex]) < waypointReachDist)
            pathIndex++;

        Vector2 targetDir;
        if (pathIndex < path.Count)
        {
            // Snap direction immediately when following A* waypoints
            targetDir = (path[pathIndex] - rb.position).normalized;
            currentDir = targetDir;
        }
        else
        {
            // Smooth turn only for the final segment
            targetDir = ((Vector2)player.position - rb.position).normalized;
            currentDir = Vector2.Lerp(currentDir, targetDir, turnSpeed * Time.fixedDeltaTime).normalized;
        }

        rb.linearVelocity = currentDir * moveSpeed;
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
                if (!grid[nx, ny] || closed[nx, ny])
                    continue;

                // For diagonal movement, check that both cardinal neighbors are walkable
                if (cost[i] == 14)
                {
                    if (!grid[current.x + dx[i], current.y] || !grid[current.x, current.y + dy[i]])
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
}
