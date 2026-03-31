using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 별자리/퀘스트 정보를 표시하는 HUD.
/// Planning 페이즈: 별자리 이름 + 퀘스트 정보
/// RealTime 페이즈: 목표 상태 + 남은 기한
/// </summary>
public class QuestHUD : MonoBehaviour
{
    [Header("Planning Phase UI")]
    [Tooltip("현재 별자리 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI zodiacNameText;
    [Tooltip("퀘스트 설명 텍스트")]
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [Tooltip("Planning 페이즈 패널")]
    [SerializeField] private GameObject planningPanel;

    [Header("RealTime Phase UI")]
    [Tooltip("현재 목표 텍스트")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [Tooltip("남은 기한 텍스트")]
    [SerializeField] private TextMeshProUGUI deadlineText;
    [Tooltip("진행도 슬라이더")]
    [SerializeField] private Slider progressSlider;
    [Tooltip("RealTime 페이즈 패널")]
    [SerializeField] private GameObject progressPanel;

    [Header("Result Feedback")]
    [Tooltip("퀘스트 완료/실패 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI resultText;
    [Tooltip("결과 텍스트 표시 시간")]
    [SerializeField] private float resultDisplayDuration = 3f;

    private float resultTimer;

    private void Start()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestAssigned.AddListener(OnQuestAssigned);

        if (QuestProgressTracker.Instance != null)
        {
            QuestProgressTracker.Instance.OnWaypointReached += OnWaypointReached;
            QuestProgressTracker.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestProgressTracker.Instance.OnQuestFailed += OnQuestFailed;
            QuestProgressTracker.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
        }

        SetPanelVisibility(false, false);
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestAssigned.RemoveListener(OnQuestAssigned);

        if (QuestProgressTracker.Instance != null)
        {
            QuestProgressTracker.Instance.OnWaypointReached -= OnWaypointReached;
            QuestProgressTracker.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestProgressTracker.Instance.OnQuestFailed -= OnQuestFailed;
            QuestProgressTracker.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        bool isPlanning = GameManager.Instance.CurrentPhase == GamePhase.Paused;
        bool isRealTime = GameManager.Instance.CurrentPhase == GamePhase.RealTime;
        bool isTracking = QuestProgressTracker.Instance != null && QuestProgressTracker.Instance.IsTracking;

        SetPanelVisibility(isPlanning, isRealTime && isTracking);

        if (isRealTime && isTracking)
        {
            UpdateDeadlineDisplay();
            if (progressSlider != null)
                progressSlider.value = QuestProgressTracker.Instance.Progress;
        }

        // 결과 텍스트 타이머
        if (resultTimer > 0f)
        {
            resultTimer -= Time.unscaledDeltaTime;
            if (resultTimer <= 0f && resultText != null)
                resultText.gameObject.SetActive(false);
        }
    }

    private void OnQuestAssigned()
    {
        var quest = QuestManager.Instance.currentQuest;
        if (quest == null) return;

        if (zodiacNameText != null)
            zodiacNameText.text = ZodiacSystem.GetZodiacName(QuestManager.Instance.CurrentZodiac);

        if (questDescriptionText != null)
            questDescriptionText.text = $"{quest.questId}\n{GetQuestTypeDescription(quest)}";

        if (progressSlider != null)
            progressSlider.value = 0f;
    }

    private void OnWaypointReached(int reached, int total)
    {
        // 진행도는 Update에서 슬라이더로 갱신됨
    }

    private void OnObjectiveUpdated(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    private void OnQuestCompleted()
    {
        ShowResult("임무 완료!");
    }

    private void OnQuestFailed()
    {
        ShowResult("임무 실패...");
    }

    private void ShowResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
            resultText.gameObject.SetActive(true);
            resultTimer = resultDisplayDuration;
        }
    }

    private void UpdateDeadlineDisplay()
    {
        if (deadlineText == null || QuestProgressTracker.Instance == null) return;
        float remaining = QuestProgressTracker.Instance.RemainingDeadline;
        int days = Mathf.FloorToInt(remaining / 24f);
        int hours = Mathf.FloorToInt(remaining % 24f);
        deadlineText.text = days > 0 ? $"남은 기한: {days}일 {hours}시간" : $"남은 기한: {hours}시간";
    }

    private void SetPanelVisibility(bool showPlanning, bool showProgress)
    {
        if (planningPanel != null) planningPanel.SetActive(showPlanning);
        if (progressPanel != null) progressPanel.SetActive(showProgress);
    }

    private string GetQuestTypeDescription(QuestData quest)
    {
        switch (quest.questType)
        {
            case QuestType.Delivery:
                string itemName = quest.deliveryItem != null ? quest.deliveryItem.itemName : "화물";
                return $"[배달] {itemName}을(를) 목적지에 전달하세요.";

            case QuestType.Discard:
                string discardName = quest.discardTargetItem != null ? quest.discardTargetItem.itemName : "아이템";
                return $"[투기] {discardName}을(를) 바다에 버리고 목적지로 이동하세요.";

            case QuestType.Interact:
                return "[탐사] 경로 상의 표류물과 접촉 후 목적지로 이동하세요.";

            case QuestType.Gather:
                return $"[수집] 골드 {quest.gatherTargetGold}G 이상을 확보하고 목적지로 이동하세요.";

            default:
                return quest.questType.ToString();
        }
    }
}
