using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Constraints")]
    [Tooltip("섬과 같은 장애물 레이어 (해당 레이어 위에는 스폰되지 않음)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("플레이어와 같은 레이어 (해당 레이어 위에는 스폰되지 않음")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("월드맵 반경 한계치")]
    [SerializeField] private float spawnMapRadius = 45.0f;
    [Tooltip("섬과의 최소 안전 거리")]
    [SerializeField] private float safeRadius = 3.0f;
    [Tooltip("플레이어와의 최소 안전 거리 (스폰 방지용)")]
    [SerializeField] private float playerSafeRadius = 15.0f;
    [Tooltip("다른 적 군집과의 최소 유지 거리 (균등 분포용)")]
    [SerializeField] private float groupSpacing = 20.0f;

    [SerializeField] private Transform _worldMapTransform;
    
    private Transform playerTransform;

    // 이번 웨이브에 스폰된 군집들의 중심점 위치를 기억
    private List<Vector2> recentSpawnPoints = new List<Vector2>();

    private void Awake()
    {
        if (_worldMapTransform == null)
        {
            GameObject worldMap = GameObject.FindGameObjectWithTag("WorldMap");
            if (worldMap != null) _worldMapTransform = worldMap.transform;
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    // 새로운 웨이브가 시작될 때 이전 스폰 위치 기록 초기화
    public void ResetSpawnPoints()
    {
        recentSpawnPoints.Clear();
    }

    // WaveManager가 호출하여 적 무리를 한 번에 스폰하고 오브젝트 배열을 반환하는 기능
    public GameObject[] SpawnEnemyGroup(GameObject enemyPrefab, int count)
    {
        if (enemyPrefab == null || count <= 0) return null;

        GameObject[] spawnedEnemies = new GameObject[count];
        Vector2 groupCenter = GetValidSpawnPoint();

        if (groupCenter != Vector2.zero)
        {
            recentSpawnPoints.Add(groupCenter); // 스폰된 위치 기억
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = GetValidIndividualSpawnPoint(groupCenter);
            spawnedEnemies[i] = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        
        return spawnedEnemies;
    }

    private Vector2 GetValidIndividualSpawnPoint(Vector2 center)
    {
        FindPlayer();
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;

        // 개별 적의 스폰 위치도 플레이어와 너무 가깝지 않도록 검증
        for (int i = 0; i < 10; i++)
        {
            Vector2 pos = center + new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            if (Vector2.Distance(pos, playerPos) >= playerSafeRadius)
            {
                return pos;
            }
        }
        
        // 10번 시도해도 실패하면 강제로 플레이어 반대 방향으로 안전거리만큼 밀어냄
        Vector2 dirFromPlayer = (center - playerPos).normalized;
        if (dirFromPlayer == Vector2.zero) dirFromPlayer = Random.insideUnitCircle.normalized;
        return playerPos + dirFromPlayer * playerSafeRadius;
    }

    private Vector2 GetValidSpawnPoint()
    {
        FindPlayer();
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;

        Vector2 bestPoint = Vector2.zero;
        float maxMinDistance = -1f;

        // 최대 50번 시도하여 섬과 플레이어가 없고 다른 군집과 충분히 멀리 떨어진 공간을 찾음
        for (int i = 0; i < 50; i++)
        {
            Vector2 randomPos = new Vector2(Random.Range(-spawnMapRadius, spawnMapRadius), Random.Range(-spawnMapRadius, spawnMapRadius));
            
            // 플레이어와의 거리를 물리 검사 이전에 수학적 거리로 한 번 더 확실하게 차단
            if (Vector2.Distance(randomPos, playerPos) < playerSafeRadius)
            {
                continue; // 너무 가깝다면 바로 건너뛰기
            }

            Collider2D hitToIsland = Physics2D.OverlapCircle(randomPos, safeRadius, obstacleLayer);
            Collider2D hitToPlayer = Physics2D.OverlapCircle(randomPos, safeRadius, playerLayer);

            if (hitToIsland == null && hitToPlayer == null)
            {
                // 첫 군집이면 무조건 통과
                if (recentSpawnPoints.Count == 0)
                {
                    return randomPos;
                }

                // 기존 군집들과의 최소 거리 계산
                float minDistanceToOthers = float.MaxValue;
                foreach (var point in recentSpawnPoints)
                {
                    float dist = Vector2.Distance(randomPos, point);
                    if (dist < minDistanceToOthers)
                    {
                        minDistanceToOthers = dist;
                    }
                }

                // 설정한 군집 간 간격(groupSpacing)보다 멀면 즉시 채택
                if (minDistanceToOthers >= groupSpacing)
                {
                    return randomPos;
                }

                // 간격 조건을 완벽히 만족하지는 못하더라도, 시도한 것 중 가장 멀리 떨어진 점을 일단 기억
                if (minDistanceToOthers > maxMinDistance)
                {
                    maxMinDistance = minDistanceToOthers;
                    bestPoint = randomPos;
                }
            }
        }
        
        // 완벽한 점을 못 찾았다면, 그나마 가장 멀리 떨어진 점을 반환
        if (bestPoint != Vector2.zero) return bestPoint;
        
        // 아예 찾지 못했을 때 Vector2.zero(플레이어 위치)를 주던 기존 문제를 제거
        // 대신 맵 가장 바깥쪽의 무작위 외곽 좌표를 반환하여 안전하게 스폰
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * spawnMapRadius;
    }
}
