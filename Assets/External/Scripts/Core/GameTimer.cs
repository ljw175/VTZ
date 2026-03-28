using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

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

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI timerToDayText;
    [SerializeField] private TextMeshProUGUI timerToWeekText;
    [SerializeField] private TextMeshProUGUI timerToMonthText;
    [SerializeField] private TextMeshProUGUI gameOverTimerText;
    [SerializeField] private TextMeshProUGUI gameClearTimerText;

    [Header("Timer Settings")]
    [SerializeField] private float currentTime = 0;
    [SerializeField] private bool isRunning = true;

    public float CurrentTime => currentTime;
    public int Months { get; private set; } = 1;
    public int Weeks { get; private set; } = 1;
    public int Days { get; private set; } = 1;

    private int hours;
    private int minutes;
    private int seconds;

    public event Action<int> OnDayChanged;
    public event Action<int> OnMonthChanged;
    public event Action<int> OnWeekChanged;

    private int previousTotalDays = -1;
    private int previousDay = -1;
    private int currentYear = 1;
    public int CurrentYear => currentYear;
    private readonly int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    [Header("Distance Fading Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform timerWorldPosition;

    [Header("Time Reduce Settings")]
    [SerializeField] private TextMeshProUGUI reduceText;
    [SerializeField] private float effectDuration = 1f;
    private float accumulatedAmount = 0f;
    private Coroutine effectCoroutine;

    void Start()
    {
        if (timerWorldPosition == null)
        {
            timerWorldPosition = transform;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += StopGameOverTimer;
            GameManager.Instance.OnGameClear += StopGameClearTimer;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= StopGameOverTimer;
            GameManager.Instance.OnGameClear -= StopGameClearTimer;
        }
    }

    void Update()
    {
        if (isRunning && GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay();

            int currentTotalDays = Mathf.FloorToInt(currentTime / 24f);
            if (currentTotalDays != previousTotalDays)
            {
                UpdateCalendar(currentTotalDays);
                previousTotalDays = currentTotalDays;
            }
        }
    }

    void StopGameOverTimer()
    {
        isRunning = false;
        if (gameOverTimerText != null)
        {
            UpdateGameOverDisplay();
        }
    }

    void StopGameClearTimer()
    {
        isRunning = false;
        if (gameClearTimerText != null)
        {
            UpdateGameClearDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        hours = Mathf.FloorToInt(currentTime / 3600f);
        minutes = Mathf.FloorToInt(currentTime % 3600f / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    void UpdateCalendar(int totalDays)
    {
        int tempDays = totalDays;
        int tempMonth = 1;
        int tempYear = 1;

        while (true)
        {
            int maxDays = daysInMonth[tempMonth - 1];

            if (tempMonth == 2 && tempYear % 4 == 0 && (tempYear % 100 != 0 || tempYear % 400 == 0))
            {
                maxDays = 29;
            }

            if (tempDays >= maxDays)
            {
                tempDays -= maxDays;
                tempMonth++;
                if (tempMonth > 12)
                {
                    tempMonth = 1;
                    tempYear++;
                }
            }
            else
            {
                break;
            }
        }

        int previousMonth = Months;
        int previousWeek = Weeks;

        Months = tempMonth;
        Days = 1 + tempDays;
        currentYear = tempYear;

        Weeks = Mathf.FloorToInt((Days - 1) / 7f) + 1;

        if (previousDay != Days) { OnDayChanged?.Invoke(Days); previousDay = Days; }
        if (previousMonth != Months) OnMonthChanged?.Invoke(Months);
        if (previousWeek != Weeks) OnWeekChanged?.Invoke(Weeks);

        UpdateDayTimerDisplay();
        UpdateWeekTimerDisplay();
        UpdateMonthTimerDisplay();
    }

    void UpdateDayTimerDisplay()
    {
        if (timerToDayText != null)
            timerToDayText.text = string.Format("{0:0}", Days);
    }

    void UpdateWeekTimerDisplay()
    {
        if (timerToWeekText == null) return;

        string suffix = Weeks switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
        timerToWeekText.text = $"{Weeks}{suffix} Week";
    }

    void UpdateMonthTimerDisplay()
    {
        if (timerToMonthText != null)
            timerToMonthText.text = string.Format("{0:0}", Months);
    }

    void UpdateGameOverDisplay()
    {
        string suffix = Days switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
        gameOverTimerText.text = $"DEATH: Jan. {Days:00}{suffix}";
    }

    void UpdateGameClearDisplay()
    {
        hours = Mathf.FloorToInt(currentTime / 3600f);
        minutes = Mathf.FloorToInt(currentTime % 3600f / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);

        gameClearTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    public void ReduceTime(int amount)
    {
        currentTime = Mathf.Max(0, currentTime - amount);

        accumulatedAmount += amount;

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        effectCoroutine = StartCoroutine(ShowReduceEffectRoutine());
    }

    private IEnumerator ShowReduceEffectRoutine()
    {
        yield return new WaitForEndOfFrame();

        reduceText.gameObject.SetActive(true);
        reduceText.text = $"- {accumulatedAmount:F0}:00";

        float timer = 0f;
        Color c = reduceText.color;
        while (timer < effectDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / effectDuration;

            c.a = Mathf.Lerp(1f, 0f, ratio);
            reduceText.color = c;

            yield return null;
        }

        reduceText.gameObject.SetActive(false);
        accumulatedAmount = 0f;
        effectCoroutine = null;
    }
}
