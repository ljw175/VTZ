using System.Collections.Generic;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Cloud Spawning")]
    [SerializeField] private GameObject[] cloudPrefabs;
    [SerializeField] private int maxCloudCount = 10;
    [SerializeField] private float worldSpawnOffset = 510.0f;

    [Header("Weather Configuration")]
    [SerializeField] private WeatherStateDefinition defaultWeatherState;
    [SerializeField] private float cloudSpeed = 3.0f;

    private List<CloudController> activeClouds = new List<CloudController>();

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

        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void InitializeClouds()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;

        for (int i = 0; i < maxCloudCount; i++)
        {
            GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

            float randomX = Random.Range(-worldSpawnOffset, worldSpawnOffset);
            float randomY = Random.Range(-worldSpawnOffset, worldSpawnOffset);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GameObject spawnedCloud = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

            Vector3 moveDir = Random.value > 0.5f ? Vector3.right : Vector3.left;

            CloudController cloud = spawnedCloud.GetComponent<CloudController>();
            if (cloud != null)
            {
                cloud.Initialize(moveDir, defaultWeatherState != null ? defaultWeatherState.cloudMoveSpeed : cloudSpeed);

                if (defaultWeatherState != null)
                    cloud.InitializeWeather(defaultWeatherState);

                activeClouds.Add(cloud);
            }
        }
    }

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

        // defaultWeatherState로 초기화 후 재배치
        cloud.Initialize(moveDir, defaultWeatherState != null ? defaultWeatherState.cloudMoveSpeed : cloudSpeed);
        if (defaultWeatherState != null)
            cloud.InitializeWeather(defaultWeatherState);
    }

    private void HandleDayChanged(int day)
    {
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            if (activeClouds[i] == null)
            {
                activeClouds.RemoveAt(i);
                continue;
            }

            RollWeatherTransition(activeClouds[i]);
        }
    }

    private void RollWeatherTransition(CloudController cloud)
    {
        var currentState = cloud.CurrentWeatherState;
        if (currentState == null || currentState.transitions == null || currentState.transitions.Length == 0)
            return;

        float roll = Random.value;
        float cumulative = 0f;

        Debug.Log($"[Weather Transition] '{cloud.gameObject.name}' rolling transition — current: '{currentState.stateName}', roll: {roll:F3}");

        for (int i = 0; i < currentState.transitions.Length; i++)
        {
            cumulative += currentState.transitions[i].probability;
            if (roll <= cumulative)
            {
                var targetState = currentState.transitions[i].targetState;
                if (targetState != null && targetState != currentState)
                {
                    Debug.Log($"[Weather Transition] '{cloud.gameObject.name}' transitioning: '{currentState.stateName}' → '{targetState.stateName}' (roll: {roll:F3}, threshold: {cumulative:F3})");
                    cloud.TransitionWeather(targetState);
                }
                else
                {
                    Debug.Log($"[Weather Transition] '{cloud.gameObject.name}' staying at '{currentState.stateName}' (roll: {roll:F3}, threshold: {cumulative:F3})");
                }
                return;
            }
        }
    }
}
