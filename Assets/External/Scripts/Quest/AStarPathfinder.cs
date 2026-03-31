using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 기반 A* 경로 탐색기.
/// Physics2D.OverlapCircle로 Island 콜라이더를 감지하여 회피 경로를 생성한다.
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder Instance { get; private set; }

    [Header("Map")]
    [Tooltip("맵 경계를 정의하는 BoxCollider2D (CameraController/FoWManager와 동일)")]
    [SerializeField] private BoxCollider2D mapBounds;

    [Header("Grid Settings")]
    [Tooltip("A* 그리드 해상도 (128 = 128x128 셀)")]
    [SerializeField] private int gridResolution = 128;

    [Tooltip("장애물 감지 반경 (섬 주변 안전 패딩)")]
    [SerializeField] private float obstacleCheckRadius = 1.5f;

    [Tooltip("장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Path Smoothing")]
    [Tooltip("스무딩 시 CircleCast 반경 (선박 폭 고려)")]
    [SerializeField] private float smoothingRadius = 0.8f;

    private bool[,] walkableGrid;
    private Vector2 worldMin;
    private Vector2 worldSize;
    private Vector2 cellSize;

    // A* 노드
    private struct Node
    {
        public int x, y;
        public float gCost, hCost;
        public float fCost => gCost + hCost;
        public int parentX, parentY;
    }

    // 8방향 이동 (상하좌우 + 대각선)
    private static readonly int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly float[] moveCost = { 1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        BakeGrid();
    }

    /// <summary>
    /// Physics2D.OverlapCircle로 각 셀의 통행 가능 여부를 캐싱한다.
    /// </summary>
    public void BakeGrid()
    {
        if (mapBounds == null)
        {
            Debug.LogError("[AStarPathfinder] mapBounds가 할당되지 않았습니다.");
            return;
        }

        worldMin = (Vector2)mapBounds.bounds.min;
        Vector2 worldMax = (Vector2)mapBounds.bounds.max;
        worldSize = worldMax - worldMin;
        cellSize = worldSize / gridResolution;

        walkableGrid = new bool[gridResolution, gridResolution];

        for (int y = 0; y < gridResolution; y++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                Vector2 worldPos = GridToWorld(new Vector2Int(x, y));
                Collider2D hit = Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer);
                walkableGrid[x, y] = (hit == null);
            }
        }
    }

    /// <summary>
    /// A* 알고리즘으로 start에서 end까지의 경로를 월드 좌표 리스트로 반환한다.
    /// 경로를 찾지 못하면 직선 폴백을 반환한다.
    /// </summary>
    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        if (walkableGrid == null)
        {
            Debug.LogWarning("[AStarPathfinder] 그리드가 베이크되지 않았습니다. 직선 폴백.");
            return FallbackLinearPath(start, end);
        }

        Vector2Int startCell = WorldToGrid(start);
        Vector2Int endCell = WorldToGrid(end);

        // 시작/끝 셀이 막혀있으면 가장 가까운 walkable 셀로 보정
        startCell = FindNearestWalkable(startCell);
        endCell = FindNearestWalkable(endCell);

        if (startCell.x < 0 || endCell.x < 0)
        {
            Debug.LogWarning("[AStarPathfinder] 유효한 시작/끝 셀을 찾을 수 없습니다.");
            return FallbackLinearPath(start, end);
        }

        // A* 탐색
        float[,] gCost = new float[gridResolution, gridResolution];
        bool[,] closed = new bool[gridResolution, gridResolution];
        int[,] parentX = new int[gridResolution, gridResolution];
        int[,] parentY = new int[gridResolution, gridResolution];

        for (int y = 0; y < gridResolution; y++)
            for (int x = 0; x < gridResolution; x++)
                gCost[x, y] = float.MaxValue;

        // Binary Heap 대신 SortedSet 사용 (간결성 우선)
        var openSet = new SortedSet<(float fCost, float gCost, int x, int y)>();

        gCost[startCell.x, startCell.y] = 0;
        float startH = Heuristic(startCell, endCell);
        openSet.Add((startH, 0f, startCell.x, startCell.y));
        parentX[startCell.x, startCell.y] = -1;
        parentY[startCell.x, startCell.y] = -1;

        bool found = false;

        while (openSet.Count > 0)
        {
            var current = openSet.Min;
            openSet.Remove(current);

            int cx = current.x;
            int cy = current.y;

            if (closed[cx, cy]) continue;
            closed[cx, cy] = true;

            if (cx == endCell.x && cy == endCell.y)
            {
                found = true;
                break;
            }

            for (int i = 0; i < 8; i++)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];

                if (nx < 0 || nx >= gridResolution || ny < 0 || ny >= gridResolution) continue;
                if (!walkableGrid[nx, ny] || closed[nx, ny]) continue;

                // 대각선 이동 시 양쪽 인접 셀도 walkable이어야 코너 클리핑 방지
                if (i % 2 == 1) // 대각선 인덱스: 1, 3, 5, 7
                {
                    if (!walkableGrid[cx + dx[i], cy] || !walkableGrid[cx, cy + dy[i]])
                        continue;
                }

                float newG = gCost[cx, cy] + moveCost[i];

                if (newG < gCost[nx, ny])
                {
                    gCost[nx, ny] = newG;
                    parentX[nx, ny] = cx;
                    parentY[nx, ny] = cy;
                    float h = Heuristic(new Vector2Int(nx, ny), endCell);
                    openSet.Add((newG + h, newG, nx, ny));
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning("[AStarPathfinder] 경로를 찾지 못했습니다. 직선 폴백.");
            return FallbackLinearPath(start, end);
        }

        // 경로 역추적
        List<Vector2Int> gridPath = new List<Vector2Int>();
        int px = endCell.x, py = endCell.y;
        while (px != -1 && py != -1)
        {
            gridPath.Add(new Vector2Int(px, py));
            int tempX = parentX[px, py];
            int tempY = parentY[px, py];
            px = tempX;
            py = tempY;
        }
        gridPath.Reverse();

        // 월드 좌표 변환
        List<Vector2> worldPath = new List<Vector2>();
        foreach (var cell in gridPath)
            worldPath.Add(GridToWorld(cell));

        // 경로 스무딩
        worldPath = SmoothPath(worldPath);

        // 시작/끝을 정확한 월드 좌표로 교체
        if (worldPath.Count > 0)
        {
            worldPath[0] = start;
            worldPath[worldPath.Count - 1] = end;
        }

        return worldPath;
    }

    /// <summary>
    /// 비인접 노드 간 직선 통과 가능 시 중간 노드를 제거하여 경로를 단순화한다.
    /// </summary>
    private List<Vector2> SmoothPath(List<Vector2> path)
    {
        if (path.Count <= 2) return path;

        List<Vector2> smoothed = new List<Vector2> { path[0] };
        int current = 0;

        while (current < path.Count - 1)
        {
            int farthest = current + 1;

            for (int test = path.Count - 1; test > current + 1; test--)
            {
                Vector2 dir = path[test] - path[current];
                float dist = dir.magnitude;

                if (dist < 0.01f) continue;

                RaycastHit2D hit = Physics2D.CircleCast(
                    path[current], smoothingRadius, dir.normalized, dist, obstacleLayer
                );

                if (hit.collider == null)
                {
                    farthest = test;
                    break;
                }
            }

            smoothed.Add(path[farthest]);
            current = farthest;
        }

        return smoothed;
    }

    private Vector2Int FindNearestWalkable(Vector2Int cell)
    {
        if (cell.x >= 0 && cell.x < gridResolution && cell.y >= 0 && cell.y < gridResolution
            && walkableGrid[cell.x, cell.y])
            return cell;

        // BFS로 가장 가까운 walkable 셀 탐색
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(cell);
        visited.Add(cell);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            for (int i = 0; i < 8; i++)
            {
                int nx = c.x + dx[i];
                int ny = c.y + dy[i];
                var next = new Vector2Int(nx, ny);

                if (nx < 0 || nx >= gridResolution || ny < 0 || ny >= gridResolution) continue;
                if (visited.Contains(next)) continue;
                visited.Add(next);

                if (walkableGrid[nx, ny]) return next;
                queue.Enqueue(next);
            }

            if (visited.Count > 1000) break; // 안전 장치
        }

        return new Vector2Int(-1, -1);
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        float ddx = Mathf.Abs(a.x - b.x);
        float ddy = Mathf.Abs(a.y - b.y);
        // Octile distance (8방향에 최적)
        return Mathf.Max(ddx, ddy) + 0.414f * Mathf.Min(ddx, ddy);
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int gx = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - worldMin.x) / cellSize.x), 0, gridResolution - 1);
        int gy = Mathf.Clamp(Mathf.FloorToInt((worldPos.y - worldMin.y) / cellSize.y), 0, gridResolution - 1);
        return new Vector2Int(gx, gy);
    }

    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        float wx = worldMin.x + (gridPos.x + 0.5f) * cellSize.x;
        float wy = worldMin.y + (gridPos.y + 0.5f) * cellSize.y;
        return new Vector2(wx, wy);
    }

    private List<Vector2> FallbackLinearPath(Vector2 start, Vector2 end)
    {
        List<Vector2> path = new List<Vector2>();
        int segments = 10;
        for (int i = 0; i <= segments; i++)
            path.Add(Vector2.Lerp(start, end, (float)i / segments));
        return path;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (walkableGrid == null) return;

        for (int y = 0; y < gridResolution; y++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                if (!walkableGrid[x, y])
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    Vector2 pos = GridToWorld(new Vector2Int(x, y));
                    Gizmos.DrawCube(pos, (Vector3)cellSize * 0.9f);
                }
            }
        }
    }
#endif
}
