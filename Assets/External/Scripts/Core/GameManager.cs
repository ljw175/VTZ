using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum GamePhase { Paused, RealTime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action OnGameOver;
    public event Action OnGameClear;
    public event Action OnPlanningEnded;

    public bool IsPaused { get; private set; }
    
    public GamePhase CurrentPhase = GamePhase.Paused; 
    public bool isPaused => CurrentPhase == GamePhase.Paused;

    [Header("운명 일치율 (Fate Sync Rate)")]
    [SerializeField] private float absoluteMaxSyncRate = 100f;
    [SerializeField] private float syncDepletionRate = 5f; 
    [SerializeField] private float lostDepletionMultiplier = 3f;
    [SerializeField] private float maxSyncPenaltyRate = 1f; 
    [SerializeField] private float syncRecoveryRate = 5f;

    public float CurrentMaxSyncRate { get; private set; }
    public float CurrentFateSyncRate { get; private set; }
    private bool isShipLost = false;

    [Header("UI Settings")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject clearText;
    [SerializeField] private GameObject timerText;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private GameObject firstClearPanel;
    [SerializeField] private GameObject secondClearPanel;
    [SerializeField] private GameObject thirdClearPanel;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Path Planning UI")]
    [SerializeField] private Slider pathLimitSlider;
    [SerializeField] private GameObject pathLimitUIContainer;

    [Header("RealTime UI Settings")]
    [SerializeField] private Slider fateSyncSlider;
    [SerializeField] private GameObject calenderObj;
    [SerializeField] private TextMeshProUGUI accelLevelText;
    [SerializeField] private GameObject steeringObj;

    [Header("Cursor Settings")]
    [SerializeField] private GameObject dynamicUICursorObj;

    [Header("UI Keyboard Focus Settings")]
    [SerializeField] private GameObject firstTitleButton;
    [SerializeField] private GameObject firstPauseButton;
    [SerializeField] private GameObject firstGameOverButton;
    [SerializeField] private GameObject firstGameClearInputButton;
    [SerializeField] private GameObject secondClearButton;
    [SerializeField] private GameObject thirdSelectButton;

    [Header("Leaderboard UI")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Gauge(Steering) Settings")]
    [SerializeField] private float CurrentGauge = 100f;


    private bool secondPanelReady = false;
    private string playerName;
    
    public bool IsSteeringMode { get; private set; } = false;
    private ShipController shipController; 
    
    public bool IsDetached => shipController != null && !shipController.IsSynchronized;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        OnPlanningEnded += HandlePathLimitUI;

        CurrentMaxSyncRate = absoluteMaxSyncRate;
        CurrentFateSyncRate = absoluteMaxSyncRate;
    }

    private void Start()
    {
        shipController = FindFirstObjectByType<ShipController>();
        if (shipController != null) 
        {
            shipController.OnLostStateChanged += HandleLostState;
        }

        TitleUIFocus();
        UpdateLeaderboardUI();
        
        ChangePhase(GamePhase.Paused); 
    }

    private void OnDestroy()
    {
        if (shipController != null) shipController.OnLostStateChanged -= HandleLostState;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Space) && CurrentPhase == GamePhase.Paused && !IsPaused) ChangePhase(GamePhase.RealTime);
        if (Input.GetKeyDown(KeyCode.R) && !firstClearPanel.activeSelf) RestartGame();
        
        HandleSteeringGauge();
        HandleFateSyncRate();
        HandlePathLimitUI();

        if (secondPanelReady && secondClearPanel != null && secondClearPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            secondClearPanel.SetActive(false);
            thirdClearPanel.SetActive(true);
            clearText?.SetActive(true);
            timerText?.SetActive(true);
            SetUIFocus(thirdSelectButton); 
            secondPanelReady = false; 
        }
    }

    // [추가완료] 웨이브 전환 시 맵을 청소하고 상태를 리셋하는 코어 함수
    public void PrepareNextWave()
    {
        // 1. 맵 상의 모든 적과 발사체 강제 삭제
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies) Destroy(enemy);

        // 발사체 태그가 없다면 Enemy 총알 스크립트를 찾아 지웁니다.
        ShootEnemyBullet[] bullets = GameObject.FindObjectsByType<ShootEnemyBullet>(FindObjectsSortMode.None);
        foreach (var bullet in bullets) Destroy(bullet.gameObject);
        
        BossBullet[] bossBullets = GameObject.FindObjectsByType<BossBullet>(FindObjectsSortMode.None);
        foreach (var bullet in bossBullets) Destroy(bullet.gameObject);

        // 2. 플레이어 함선의 이전 궤적 초기화
        if (shipController != null)
        {
            shipController.ResetPathForNextWave();
        }

        // 3. 현재 운명 일치율(Fate Sync Rate)은 CurrentFateSyncRate 변수에 자연스럽게 유지됨.
        
        // 4. 강제로 계획 페이즈로 전환하여 시간을 멈춤
        ChangePhase(GamePhase.Paused);
    }

    #region UI & State Logic
    private void HandlePathLimitUI()
    {
        if (CurrentPhase == GamePhase.Paused && pathLimitSlider != null && shipController != null)
        {
            pathLimitSlider.value = shipController.GetTraceProgress();
        }
        if (CurrentPhase == GamePhase.RealTime && pathLimitUIContainer != null)
        {
            pathLimitUIContainer.SetActive(false);
        }
    }

    private void HandleLostState(bool lost) { isShipLost = lost; }

    private void HandleFateSyncRate()
    {
        if (CurrentPhase != GamePhase.RealTime) return;

        if (IsDetached)
        {
            float efficiencyModifier = 1f;
            if (shipController != null && shipController.StatsProvider != null)
            {
                float efficiency = shipController.StatsProvider.GetStat(ShipStatType.FuelEfficiency);
                efficiencyModifier = 1f / Mathf.Max(0.1f, efficiency);
            }

            float currentDepletionRate = isShipLost ? (syncDepletionRate * lostDepletionMultiplier) : syncDepletionRate;
            currentDepletionRate *= efficiencyModifier;
            float currentMaxPenaltyRate = isShipLost ? (maxSyncPenaltyRate * lostDepletionMultiplier) : maxSyncPenaltyRate;

            CurrentFateSyncRate -= currentDepletionRate * Time.deltaTime;
            CurrentMaxSyncRate -= currentMaxPenaltyRate * Time.deltaTime;
            
            CurrentMaxSyncRate = Mathf.Max(1f, CurrentMaxSyncRate); 
            CurrentFateSyncRate = Mathf.Max(0f, CurrentFateSyncRate);

            if (CurrentFateSyncRate <= 0) GameOver(); 
        }
        else 
        {
            if (CurrentFateSyncRate < CurrentMaxSyncRate)
            {
                CurrentFateSyncRate += syncRecoveryRate * Time.deltaTime;
                CurrentFateSyncRate = Mathf.Min(CurrentFateSyncRate, CurrentMaxSyncRate);
            }
        }
    }
    #endregion

    #region Game Flow & Phase Control
    public void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase) return;

        switch (CurrentPhase)
        {
            case GamePhase.Paused: OnPlanningEnded?.Invoke(); break;
            case GamePhase.RealTime: break;
        }

        CurrentPhase = nextPhase;

        switch (nextPhase)
        {
            case GamePhase.Paused: 
                AudioManager.Instance.SetSlowBgm(); 
                SetPlanningUIAndCursor(true);
                break;
            case GamePhase.RealTime:
                AudioManager.Instance.SetNormalBgm();
                SetPlanningUIAndCursor(false);
                break;
        }
    }

    private void SetPlanningUIAndCursor(bool isPlanning)
    {
        Cursor.visible = !isPlanning;
        Cursor.lockState = isPlanning ? CursorLockMode.Confined : CursorLockMode.None;

        if (dynamicUICursorObj != null) dynamicUICursorObj.SetActive(isPlanning);

        bool isRealTime = !isPlanning;
        if (fateSyncSlider != null) fateSyncSlider.gameObject.SetActive(isRealTime);
        if (calenderObj != null) calenderObj.SetActive(isRealTime);
        if (accelLevelText != null) accelLevelText.gameObject.SetActive(isRealTime);
        if (steeringObj != null) steeringObj.SetActive(isRealTime);
    }

    public void TogglePause() { if (IsPaused) ResumeGame(); else PauseGame(); }
    public void TitleUIFocus() { if (firstTitleButton != null) SetUIFocus(firstTitleButton); }
    public void PauseGame() { IsPaused = true; Time.timeScale = 0f; if (pauseMenu != null) { pauseMenu.SetActive(true); SetUIFocus(firstPauseButton); } }
    public void ResumeGame() { IsPaused = false; Time.timeScale = 1f; if (pauseMenu != null) { pauseMenu.SetActive(false); EventSystem.current.SetSelectedGameObject(null); } }

    public void GameOver()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;
        ChangePhase(GamePhase.Paused); 
        StartCoroutine(ShowGameOverPanelRoutine());
        OnGameOver?.Invoke();
    }

    private IEnumerator ShowGameOverPanelRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null) { gameOverPanel.SetActive(true); SetUIFocus(firstGameOverButton); }
    }

    public void GameClear()
    {
        if (gameClearPanel != null) { gameClearPanel.SetActive(true); firstClearPanel.SetActive(true); SetUIFocus(firstGameClearInputButton); }
        OnGameClear?.Invoke();
    }

    public void OnFirstPanelSubmit()
    {
        playerName = string.IsNullOrEmpty(nameInputField.text) ? "Unnamed" : nameInputField.text;
        LeaderboardManager.Instance.AddScore(playerName, GameTimer.Instance.CurrentTime);
        firstClearPanel.SetActive(false); secondClearPanel.SetActive(true);
        clearText.SetActive(false); timerText.SetActive(false);
        UpdateSecondPanelLeaderboard();
        EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(EnableSecondPanelInputNextFrame());
    }
    private IEnumerator EnableSecondPanelInputNextFrame() { yield return null; secondPanelReady = true; }

    private void UpdateSecondPanelLeaderboard()
    {
        if (LeaderboardManager.Instance == null) return;
        var leaderboard = LeaderboardManager.Instance.GetLeaderboard();
        int count = Mathf.Min(leaderboard.Count, 10); 
        rankText.text = ""; nameText.text = ""; timeText.text = "";
        for (int i = 0; i < count; i++)
        {
            var entry = leaderboard[i];
            rankText.text += $"{entry.rank}\n"; nameText.text += $"{entry.playerName}\n"; timeText.text += $"{entry.clearTime:F2}\n";
        }
    }
    private void SetUIFocus(GameObject firstSelected)
    {
        if (firstSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
    #endregion

    #region Gauge(Focus) Logic
    public float GetCurrentGauge() => CurrentGauge;
    public float GetGaugePercentage() => IsSteeringMode ? 1f : 0f;

    public float GetFateSyncPercentage() => CurrentFateSyncRate / absoluteMaxSyncRate;
    public float GetMaxSyncPenaltyPercentage() => CurrentMaxSyncRate / absoluteMaxSyncRate;
    
    private void HandleSteeringGauge()
    {
        if (CurrentPhase != GamePhase.RealTime)
        {
            IsSteeringMode = false;
            return;
        }

        // Shift 키를 누르고 있는 동안만 활성화, 게이지 제약 삭제
        IsSteeringMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
    
    #endregion

    #region Scene Interaction
    public void UpdateLeaderboardUI()
    {
        if (LeaderboardManager.Instance == null || rankText == null) return;
        var leaderboard = LeaderboardManager.Instance.GetLeaderboard();
        rankText.text = ""; nameText.text = ""; timeText.text = "";
        foreach (var entry in leaderboard)
        {
            rankText.text += $"{entry.rank}\n"; nameText.text += $"{entry.playerName}\n"; timeText.text += $"{entry.clearTime:F2}\n";
        }
    }
    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("TitleScene"); }
    public void StartGame() { Time.timeScale = 1f; SceneManager.LoadScene("GameScene"); }
    public void QuitGame() { Application.Quit(); }
    #endregion
}