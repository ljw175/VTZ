using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Settings")]
    [Tooltip("게임 내에서 등장할 모든 퀘스트 리스트")]
    public List<QuestData> questDatabase;

    [Tooltip("현재 진행 중인 임무")]
    public QuestData currentQuest;

    [Header("Route Settings")]
    public LineRenderer routeLineRenderer;
    public GameObject tracePointPrefab;

    [Tooltip("경로 주변의 절대 시야를 밝힐 월드 반경")]
    public float fogRevealRadius = 5f;

    [Tooltip("TracePoint 간 최소 간격 (이 거리마다 경로 위에 포인트 생성)")]
    [SerializeField] private float tracePointSpacing = 1f;

    [Header("FoW Settings")]
    [Tooltip("FoW 텍스처 해상도 (FoWConfig와 동일하게 설정)")]
    [SerializeField] private int fowTextureResolution = 512;

    [Header("Map Bounds")]
    [Tooltip("맵 경계 BoxCollider2D (월드 크기 계산에 사용)")]
    [SerializeField] private BoxCollider2D mapBounds;

    private List<GameObject> activeTracePoints = new List<GameObject>();
    private ShipController shipController;

    [Header("Events")]
    public UnityEvent OnQuestAssigned;
    public UnityEvent OnQuestCompleted;
    public UnityEvent OnQuestFailed;

    /// <summary>현재 별자리 (UI 표시용)</summary>
    public ZodiacSign CurrentZodiac { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        shipController = FindFirstObjectByType<ShipController>();
    }

    /// <summary>
    /// Planning 페이즈 진입 시 호출.
    /// 현재 날짜의 별자리를 기반으로 퀘스트를 랜덤 할당하고 경로를 생성한다.
    /// </summary>
    public void GenerateRandomQuestForCurrentZodiac(int currentMonth, int currentDay)
    {
        CurrentZodiac = ZodiacSystem.GetZodiacSign(currentMonth, currentDay);
        Debug.Log($"Planning 페이즈 진입 - 현재 별자리: {ZodiacSystem.GetZodiacName(CurrentZodiac)}");

        List<QuestData> availableQuests = questDatabase.FindAll(q => q.providerZodiac == CurrentZodiac);

        if (availableQuests.Count > 0)
        {
            currentQuest = availableQuests[Random.Range(0, availableQuests.Count)];
            Debug.Log($"임무 할당 완료: {currentQuest.questId} ({currentQuest.questType})");

            // 런타임 상태 초기화
            currentQuest.objectiveCompleted = false;
            currentQuest.cargoLoaded = false;
            currentQuest.spawnedQuestDriftage = null;

            // 타입별 사전 설정
            SetupQuestTypeSpecific();

            CalculateQuestRoute();
            OnQuestAssigned?.Invoke();
        }
        else
        {
            Debug.LogWarning($"{CurrentZodiac} 별자리에 할당 가능한 퀘스트가 없습니다.");
        }
    }

    /// <summary>
    /// 퀘스트 타입에 따른 사전 설정 (표류물 스폰 등)
    /// </summary>
    private void SetupQuestTypeSpecific()
    {
        switch (currentQuest.questType)
        {
            case QuestType.Interact:
                SpawnQuestDriftage();
                break;

            case QuestType.Discard:
                // Discard 대상 아이템을 카고에 강제 적재 (플레이어가 버려야 하므로)
                if (currentQuest.discardTargetItem != null)
                {
                    var cargo = InventoryManager.Instance?.GetShipCargo();
                    if (cargo != null)
                    {
                        var item = new ItemInstance(currentQuest.discardTargetItem);
                        if (cargo.TryAddItem(item))
                            Debug.Log($"[Quest] Discard 대상 아이템 적재: {currentQuest.discardTargetItem.itemName}");
                        else
                            Debug.LogWarning("[Quest] Discard 대상 아이템 적재 실패: 카고 공간 부족");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Interact 퀘스트: 경로 중간 지점에 퀘스트 표류물을 강제 스폰한다.
    /// </summary>
    private void SpawnQuestDriftage()
    {
        if (currentQuest.interactDriftageDefinition == null || currentQuest.interactDriftagePrefab == null)
        {
            Debug.LogWarning("[QuestManager] Interact 퀘스트에 표류물 설정이 누락되었습니다.");
            return;
        }

        if (DriftageSpawner.Instance == null)
        {
            Debug.LogWarning("[QuestManager] DriftageSpawner 인스턴스가 없습니다.");
            return;
        }

        // 경로의 중간 경유지 근처에 스폰 (첫 경유지와 마지막 사이의 중간점)
        Vector2 spawnPos;
        if (currentQuest.targetLocations.Count >= 2)
        {
            int midIndex = currentQuest.targetLocations.Count / 2;
            Transform midTarget = currentQuest.targetLocations[midIndex];
            Transform prevTarget = currentQuest.targetLocations[midIndex - 1];
            spawnPos = Vector2.Lerp(prevTarget.position, midTarget.position, 0.5f);
        }
        else if (currentQuest.targetLocations.Count == 1 && shipController != null)
        {
            spawnPos = Vector2.Lerp(
                shipController.transform.position,
                currentQuest.targetLocations[0].position,
                0.5f
            );
        }
        else
        {
            spawnPos = shipController != null ? (Vector2)shipController.transform.position : Vector2.zero;
        }

        GameObject driftageObj = DriftageSpawner.Instance.SpawnQuestDriftage(
            currentQuest.interactDriftagePrefab,
            currentQuest.interactDriftageDefinition,
            spawnPos
        );

        if (driftageObj != null)
        {
            currentQuest.spawnedQuestDriftage = driftageObj;
            var controller = driftageObj.GetComponent<DriftageController>();
            if (controller != null)
                controller.isQuestDriftage = true;

            Debug.Log($"[Quest] 퀘스트 표류물 스폰 완료: {spawnPos}");
        }
    }

    /// <summary>
    /// A* 경로 탐색 → WaypointData 생성 → TracePoint 스폰 → FoW 노출 → ShipController 주입
    /// </summary>
    private void CalculateQuestRoute()
    {
        currentQuest.calculatedRoute.Clear();
        ClearActiveTracePoints();

        if (shipController == null)
        {
            Debug.LogError("[QuestManager] ShipController를 찾을 수 없습니다.");
            return;
        }

        Vector2 currentPos = shipController.transform.position;
        List<Vector2> astarPath = new List<Vector2>();

        // 경유지 → 목적지 순서로 A* 경로 탐색 (스무딩된 소수의 키포인트)
        foreach (Transform target in currentQuest.targetLocations)
        {
            if (target == null) continue;

            List<Vector2> pathSegment = CalculatePath(currentPos, target.position);
            astarPath.AddRange(pathSegment);

            currentPos = target.position;
        }

        // Interact 퀘스트: 스폰된 표류물 위치도 FoW 노출
        if (currentQuest.questType == QuestType.Interact && currentQuest.spawnedQuestDriftage != null)
        {
            RevealFogOfWarAroundPoint(currentQuest.spawnedQuestDriftage.transform.position);
        }

        // A* 키포인트를 일정 간격(tracePointSpacing)으로 보간하여 조밀한 경로 생성
        List<Vector2> fullPath = InterpolatePathBySpacing(astarPath, tracePointSpacing);

        // 전체 기한을 웨이포인트 수로 균등 분할
        int pointCount = fullPath.Count;
        float timePerPoint = currentQuest.totalTimeLimit / Mathf.Max(1, pointCount);

        if (routeLineRenderer != null) routeLineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 pointPos = fullPath[i];
            float pointDeadline = timePerPoint * (i + 1);

            currentQuest.calculatedRoute.Add(new WaypointData {
                position = pointPos,
                deadline = pointDeadline
            });

            if (routeLineRenderer != null) routeLineRenderer.SetPosition(i, pointPos);

            if (tracePointPrefab != null)
            {
                GameObject tp = Instantiate(tracePointPrefab, pointPos, Quaternion.identity);
                activeTracePoints.Add(tp);
            }

            RevealFogOfWarAroundPoint(pointPos);
        }

        // ShipController에 경로 주입 (유령선이 이 경로를 따라 이동)
        BridgeRouteToShipController(fullPath);
    }

    private void BridgeRouteToShipController(List<Vector2> path)
    {
        List<Vector3> tracePoints3D = new List<Vector3>();
        tracePoints3D.Add(shipController.transform.position);
        foreach (var p in path)
            tracePoints3D.Add(new Vector3(p.x, p.y, 0f));

        shipController.SetQuestRoute(tracePoints3D);
    }

    private void RevealFogOfWarAroundPoint(Vector2 point)
    {
        if (FoWManager.Instance == null) return;
        int radiusCells = WorldRadiusToFoWCells(fogRevealRadius);
        FoWManager.Instance.RevealArea(point, radiusCells);
    }

    private int WorldRadiusToFoWCells(float worldRadius)
    {
        if (FoWManager.Instance == null) return 1;

        float worldWidth = 100f;
        if (mapBounds != null)
            worldWidth = mapBounds.bounds.size.x;

        float cellsPerUnit = fowTextureResolution / worldWidth;
        return Mathf.Max(1, Mathf.RoundToInt(worldRadius * cellsPerUnit));
    }

    /// <summary>
    /// A* 키포인트 사이를 spacing 간격으로 보간하여 조밀한 경로를 생성한다.
    /// </summary>
    private List<Vector2> InterpolatePathBySpacing(List<Vector2> keyPoints, float spacing)
    {
        if (keyPoints == null || keyPoints.Count == 0) return new List<Vector2>();
        if (spacing <= 0.01f) return new List<Vector2>(keyPoints);

        List<Vector2> result = new List<Vector2>();
        result.Add(keyPoints[0]);

        float accumulated = 0f;

        for (int i = 1; i < keyPoints.Count; i++)
        {
            Vector2 prev = keyPoints[i - 1];
            Vector2 curr = keyPoints[i];
            float segmentLength = Vector2.Distance(prev, curr);

            if (segmentLength < 0.001f) continue;

            Vector2 dir = (curr - prev) / segmentLength;
            float remaining = segmentLength;
            float offset = spacing - accumulated;

            while (offset <= remaining)
            {
                Vector2 point = prev + dir * offset;
                result.Add(point);
                remaining -= offset;
                prev = point;
                offset = spacing;
                accumulated = 0f;
            }

            accumulated += remaining;
        }

        // 마지막 키포인트를 반드시 포함
        if (keyPoints.Count > 1)
        {
            Vector2 last = keyPoints[keyPoints.Count - 1];
            if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], last) > 0.01f)
                result.Add(last);
        }

        return result;
    }

    private List<Vector2> CalculatePath(Vector2 start, Vector2 end)
    {
        if (AStarPathfinder.Instance != null)
            return AStarPathfinder.Instance.FindPath(start, end);

        Debug.LogWarning("[QuestManager] AStarPathfinder 인스턴스가 없습니다. 직선 경로 사용.");
        List<Vector2> path = new List<Vector2>();
        int segments = 10;
        for (int i = 1; i <= segments; i++)
            path.Add(Vector2.Lerp(start, end, (float)i / segments));
        return path;
    }

    private void ClearActiveTracePoints()
    {
        foreach (var tp in activeTracePoints)
        {
            if (tp != null) Destroy(tp);
        }
        activeTracePoints.Clear();
    }
}
