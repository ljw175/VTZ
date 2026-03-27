using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private static WaveManager instance = null;
    public static WaveManager Instance
    {
        get { if (null == instance) return null; return instance; }
    }

    void Awake()
    {
        if (null == instance) instance = this;
        else Destroy(this.gameObject);

        enemySpawner = GetComponent<EnemySpawner>();
    }

    public event Action<int, int> OnWaveUpdated; 
    public event Action<int, int> OnEnemyProgressUpdated; 
    public event Action<bool> OnWaveModeChanged;       
    public event Action<float, float> OnBossHpUpdated; 

    [SerializeField] private WaveData[] waves; 
    private int currentWaveIndex = 0;
    private int enemiesRemainingAlive = 0;
    private int totalEnemiesInCurrentWave = 0; 
    private EnemySpawner enemySpawner;

    private bool isNextWeekReady = true; 

    void Start()
    {
        if (GameTimer.Instance != null) GameTimer.Instance.OnWeekChanged += HandleWeekChanged;
        StartCoroutine(SpawnWaves());
        AudioManager.Instance.PlayIngameBgm();
    }

    void OnDestroy()
    {
        if (GameTimer.Instance != null) GameTimer.Instance.OnWeekChanged -= HandleWeekChanged;
    }

    private void HandleWeekChanged(int week)
    {
        isNextWeekReady = true; 
    }

    private IEnumerator SpawnWaves()
    {
        while (currentWaveIndex < waves.Length)
        {
            // 1. 7일(다음 주차)이 될 때까지 대기
            while (!isNextWeekReady) yield return null;
            isNextWeekReady = false;
            
            // [추가완료] 첫 웨이브(Index 0)가 아닐 경우, 다음 웨이브로 넘어가기 전 맵 청소 및 페이즈 초기화
            if (currentWaveIndex > 0)
            {
                // 게임 루프의 핵심: 청소 -> 보존 -> 계획 페이즈 진입
                GameManager.Instance.PrepareNextWave();
            }

            WaveData currentWave = waves[currentWaveIndex];
            OnWaveUpdated?.Invoke(currentWaveIndex + 1, waves.Length);

            if (currentWave.isBossWave)
            {
                OnWaveModeChanged?.Invoke(true); 
                totalEnemiesInCurrentWave = 1;
                enemiesRemainingAlive = 1;

                enemySpawner.ResetSpawnPoints();
                GameObject[] bossObjs = enemySpawner.SpawnEnemyGroup(currentWave.enemies[0].enemyPrefab, 1);
                
                if (bossObjs != null && bossObjs.Length > 0 && bossObjs[0] != null)
                {
                    Enemy bossEnemy = bossObjs[0].GetComponent<Enemy>();
                    if (bossEnemy != null)
                    {
                        bossEnemy.OnHpChanged += (currentHp, maxHp) => { OnBossHpUpdated?.Invoke(currentHp, maxHp); };
                        OnBossHpUpdated?.Invoke(bossEnemy.MaxHp, bossEnemy.MaxHp);
                    }
                }
            }
            else 
            {
                totalEnemiesInCurrentWave = 0;
                foreach (var enemy in currentWave.enemies) totalEnemiesInCurrentWave += enemy.enemyCount;
                enemiesRemainingAlive = 0;
                
                OnEnemyProgressUpdated?.Invoke(totalEnemiesInCurrentWave, totalEnemiesInCurrentWave);
                enemySpawner.ResetSpawnPoints();

                foreach (var enemy in currentWave.enemies)
                {
                    enemySpawner.SpawnEnemyGroup(enemy.enemyPrefab, enemy.enemyCount);
                }
            }

            // 웨이브가 성공적으로 스폰되었으니 인덱스 증가
            currentWaveIndex++;
        }

        // 마지막 웨이브까지 도달한 후 7일을 더 버티면(이 로직이 맞다면) 클리어
        while (!isNextWeekReady) yield return null;
        GameManager.Instance.GameClear(); 
    }

    public void OnEnemyKilled()
    {
        enemiesRemainingAlive--;
        OnEnemyProgressUpdated?.Invoke(enemiesRemainingAlive, totalEnemiesInCurrentWave);
    }
}