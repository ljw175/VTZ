using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Weather Settings")]
    [SerializeField] private GameObject[] cloudPrefabs;
    [SerializeField] private int maxCloudCount = 10; // 생성할 구름의 총 개수
    [SerializeField] private float worldSpawnOffset = 510.0f; // 파괴 경계(60f)와 맞춰서 재배치 시 자연스럽게 진입하도록 변경
    [SerializeField] private float cloudSpeed = 3.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeClouds();
    }

    private void InitializeClouds()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;

        for (int i = 0; i < maxCloudCount; i++)
        {
            GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
            
            // 초기 시작 시에는 화면 전역에 무작위로 분포하도록 생성
            float randomX = Random.Range(-worldSpawnOffset, worldSpawnOffset);
            float randomY = Random.Range(-worldSpawnOffset, worldSpawnOffset);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GameObject spawnedCloud = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            
            // 이동 방향 (좌 또는 우 지정)
            Vector3 moveDir = Random.value > 0.5f ? Vector3.right : Vector3.left;
            
            spawnedCloud.GetComponent<CloudController>()?.Initialize(moveDir, cloudSpeed);
        }
    }

    // 구름이 경계를 벗어났을 때 반대편 위치에서 새롭게 시작하도록 설정
    public void RepositionCloud(CloudController cloud)
    {
        Vector3 spawnPos = Vector3.zero;
        Vector3 moveDir = Vector3.zero;
        
        int spawnSide = Random.Range(0, 2);

        switch (spawnSide)
        {
            case 0: // Left to Right
                spawnPos = new Vector3(-worldSpawnOffset, Random.Range(-worldSpawnOffset, worldSpawnOffset), 0);
                moveDir = Vector3.right;
                break;
            case 1: // Right to Left
                spawnPos = new Vector3(worldSpawnOffset, Random.Range(-worldSpawnOffset, worldSpawnOffset), 0);
                moveDir = Vector3.left;
                break;
        }

        cloud.transform.position = spawnPos;
        cloud.Initialize(moveDir, cloudSpeed);
    }
}