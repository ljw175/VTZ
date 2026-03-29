using System.Collections.Generic;
using UnityEngine;

public class DriftageSpawner : MonoBehaviour
{
    [Header("Spawn Constraints")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float spawnMapRadius = 45f;
    [SerializeField] private float safeRadius = 3f;
    [SerializeField] private float playerSafeRadius = 15f;
    [SerializeField] private float driftageSpacing = 10f;

    private Transform playerTransform;
    private List<Vector2> spawnPoints = new List<Vector2>();
    private List<GameObject> activeDriftage = new List<GameObject>();

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

    public void SpawnDriftage(DriftageSpawnEntry entry)
    {
        if (entry.driftagePrefab == null || entry.count <= 0) return;

        for (int i = 0; i < entry.count; i++)
        {
            Vector2 pos = GetValidSpawnPoint();
            if (pos == Vector2.zero) continue;

            spawnPoints.Add(pos);
            GameObject obj = Instantiate(entry.driftagePrefab, pos, Quaternion.identity);

            var controller = obj.GetComponent<DriftageController>();
            if (controller != null && entry.definition != null)
                controller.SetDefinition(entry.definition);

            activeDriftage.Add(obj);
        }
    }

    public void ClearAll()
    {
        for (int i = activeDriftage.Count - 1; i >= 0; i--)
        {
            if (activeDriftage[i] != null)
                Destroy(activeDriftage[i]);
        }
        activeDriftage.Clear();
        spawnPoints.Clear();
    }

    private Vector2 GetValidSpawnPoint()
    {
        FindPlayer();
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;

        Vector2 bestPoint = Vector2.zero;
        float maxMinDistance = -1f;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(-spawnMapRadius, spawnMapRadius),
                Random.Range(-spawnMapRadius, spawnMapRadius)
            );

            if (Vector2.Distance(candidate, playerPos) < playerSafeRadius)
                continue;

            if (Physics2D.OverlapCircle(candidate, safeRadius, obstacleLayer) != null)
                continue;

            if (Physics2D.OverlapCircle(candidate, safeRadius, playerLayer) != null)
                continue;

            if (spawnPoints.Count == 0)
                return candidate;

            float minDist = float.MaxValue;
            for (int j = 0; j < spawnPoints.Count; j++)
            {
                float dist = Vector2.Distance(candidate, spawnPoints[j]);
                if (dist < minDist) minDist = dist;
            }

            if (minDist >= driftageSpacing)
                return candidate;

            if (minDist > maxMinDistance)
            {
                maxMinDistance = minDist;
                bestPoint = candidate;
            }
        }

        if (bestPoint != Vector2.zero) return bestPoint;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnMapRadius;
    }
}
