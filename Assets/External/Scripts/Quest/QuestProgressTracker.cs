using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀘스트 진행을 실시간 모니터링한다.
/// 유령선 인덱스가 아닌 실제 입항/타입별 조건으로 완료를 판정한다.
///
/// 완료 조건 요약:
/// - Delivery: 첫 경유지 입항(화물 적재) → 최종 목적지 입항(화물 존재 확인 → 제거)
/// - Discard: 대상 아이템 바다 투기 → 최종 목적지 입항
/// - Interact: 퀘스트 표류물 접촉(루팅) → 최종 목적지 입항
/// - Gather: 최종 목적지 입항 시 골드 >= 목표액
/// </summary>
public class QuestProgressTracker : MonoBehaviour
{
    public static QuestProgressTracker Instance { get; private set; }

    private QuestData activeQuest;
    private float questStartTime;
    private bool isTracking;

    // targetLocations 중 다음으로 도달해야 할 인덱스
    private int nextTargetIndex;
    private List<Transform> targetLocations;

    [Tooltip("목적지/경유지에 도달했다고 판정하는 거리 (입항 시스템과 별개로, 입항 이벤트로 판정)")]
    [SerializeField] private float destinationReachDistance = 5f;

    public event Action<int, int> OnWaypointReached;  // (도달 인덱스, 전체)
    public event Action OnQuestCompleted;
    public event Action OnQuestFailed;
    public event Action<string> OnObjectiveUpdated;   // 목표 상태 텍스트 갱신

    public bool IsTracking => isTracking;
    public QuestData ActiveQuest => activeQuest;
    public int NextTargetIndex => nextTargetIndex;

    public float Progress
    {
        get
        {
            if (!isTracking || targetLocations == null || targetLocations.Count == 0) return 0f;
            return Mathf.Clamp01((float)nextTargetIndex / targetLocations.Count);
        }
    }

    public float RemainingDeadline
    {
        get
        {
            if (!isTracking || activeQuest == null || GameTimer.Instance == null) return 0f;
            float elapsed = GameTimer.Instance.CurrentTime - questStartTime;
            return Mathf.Max(0f, activeQuest.totalTimeLimit - elapsed);
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnEnable()
    {
        if (PortManager.Instance != null)
            PortManager.Instance.OnPortOpened += OnPortOpened;
    }

    private void OnDisable()
    {
        if (PortManager.Instance != null)
            PortManager.Instance.OnPortOpened -= OnPortOpened;
    }

    public void StartTracking(QuestData quest)
    {
        activeQuest = quest;
        activeQuest.objectiveCompleted = false;
        activeQuest.cargoLoaded = false;
        questStartTime = GameTimer.Instance != null ? GameTimer.Instance.CurrentTime : 0f;
        nextTargetIndex = 0;
        targetLocations = quest.targetLocations;
        isTracking = true;

        EmitObjectiveText();
    }

    public void StopTracking()
    {
        isTracking = false;
        activeQuest = null;
        targetLocations = null;
    }

    private void Update()
    {
        if (!isTracking || activeQuest == null) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.RealTime) return;

        // 기한 체크
        if (GameTimer.Instance != null)
        {
            float elapsed = GameTimer.Instance.CurrentTime - questStartTime;
            if (elapsed > activeQuest.totalTimeLimit)
            {
                Debug.Log("[QuestProgressTracker] 전체 기한 초과!");
                FailQuest();
            }
        }
    }

    /// <summary>
    /// 항구 입항 시 호출됨. 현재 경유지/목적지인지 확인하고 타입별 로직을 처리한다.
    /// </summary>
    private void OnPortOpened(PortDefinition portDef)
    {
        if (!isTracking || activeQuest == null || targetLocations == null) return;
        if (nextTargetIndex >= targetLocations.Count) return;

        Transform currentTarget = targetLocations[nextTargetIndex];
        if (currentTarget == null) return;

        // 입항한 항구와 현재 목표 경유지가 동일한지 확인 (거리 기반)
        float dist = Vector2.Distance(
            PortManager.Instance.CurrentPort.worldPosition,
            (Vector2)currentTarget.position
        );

        if (dist > destinationReachDistance) return;

        // 경유지 도달 확인
        bool isLastTarget = (nextTargetIndex == targetLocations.Count - 1);

        if (isLastTarget)
        {
            // 최종 목적지 도달 → 타입별 최종 판정
            HandleFinalDestination();
        }
        else
        {
            // 중간 경유지 도달 → 타입별 경유지 로직
            HandleWaypointArrival(nextTargetIndex);
            nextTargetIndex++;
            OnWaypointReached?.Invoke(nextTargetIndex, targetLocations.Count);
            EmitObjectiveText();
        }
    }

    /// <summary>
    /// 중간 경유지 도달 시 타입별 처리
    /// </summary>
    private void HandleWaypointArrival(int index)
    {
        switch (activeQuest.questType)
        {
            case QuestType.Delivery:
                // 첫 경유지에서 화물 적재
                if (!activeQuest.cargoLoaded && activeQuest.deliveryItem != null)
                {
                    LoadDeliveryCargo();
                }
                break;

            // 다른 타입은 경유지에서 특별한 처리 없음
        }
    }

    /// <summary>
    /// 최종 목적지 도달 시 타입별 완료 판정
    /// </summary>
    private void HandleFinalDestination()
    {
        switch (activeQuest.questType)
        {
            case QuestType.Delivery:
                if (!activeQuest.cargoLoaded)
                {
                    Debug.Log("[Quest] 화물이 적재되지 않은 상태로 목적지에 도착했습니다.");
                    OnObjectiveUpdated?.Invoke("화물을 먼저 적재해야 합니다!");
                    return; // 입항은 했지만 퀘스트 진행하지 않음
                }
                // 화물이 카고에 있는지 확인
                if (HasItemInCargo(activeQuest.deliveryItem))
                {
                    RemoveItemFromCargo(activeQuest.deliveryItem);
                    CompleteQuest();
                }
                else
                {
                    OnObjectiveUpdated?.Invoke("배달 화물을 분실했습니다!");
                }
                break;

            case QuestType.Discard:
                if (activeQuest.objectiveCompleted)
                {
                    CompleteQuest();
                }
                else
                {
                    OnObjectiveUpdated?.Invoke("아이템을 먼저 바다에 버려야 합니다!");
                }
                break;

            case QuestType.Interact:
                if (activeQuest.objectiveCompleted)
                {
                    CompleteQuest();
                }
                else
                {
                    OnObjectiveUpdated?.Invoke("표류물과 먼저 접촉해야 합니다!");
                }
                break;

            case QuestType.Gather:
                if (PlayerWallet.Instance != null && PlayerWallet.Instance.CurrentGold >= activeQuest.gatherTargetGold)
                {
                    CompleteQuest();
                }
                else
                {
                    int current = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;
                    OnObjectiveUpdated?.Invoke($"골드 부족! ({current}/{activeQuest.gatherTargetGold})");
                }
                break;
        }
    }

    // ===== Delivery 헬퍼 =====

    private void LoadDeliveryCargo()
    {
        if (activeQuest.deliveryItem == null) return;

        var cargo = InventoryManager.Instance?.GetShipCargo();
        if (cargo == null) return;

        var item = new ItemInstance(activeQuest.deliveryItem);
        if (cargo.TryAddItem(item))
        {
            activeQuest.cargoLoaded = true;
            Debug.Log($"[Quest] 배달 화물 적재 완료: {activeQuest.deliveryItem.itemName}");
            OnObjectiveUpdated?.Invoke($"화물 적재 완료! 목적지로 운반하세요.");
        }
        else
        {
            Debug.LogWarning("[Quest] 화물을 적재할 카고 공간이 부족합니다.");
            OnObjectiveUpdated?.Invoke("카고 공간 부족! 공간을 확보하세요.");
        }
    }

    private bool HasItemInCargo(ItemDefinition targetDef)
    {
        var cargo = InventoryManager.Instance?.GetShipCargo();
        if (cargo == null) return false;

        foreach (var item in cargo.GridState.PlacedItems)
        {
            if (item.DefinitionId == targetDef.itemId)
                return true;
        }
        return false;
    }

    private void RemoveItemFromCargo(ItemDefinition targetDef)
    {
        var cargo = InventoryManager.Instance?.GetShipCargo();
        if (cargo == null) return;

        foreach (var item in cargo.GridState.PlacedItems)
        {
            if (item.DefinitionId == targetDef.itemId)
            {
                cargo.RemoveItem(item);
                Debug.Log($"[Quest] 배달 화물 전달 완료: {targetDef.itemName}");
                return;
            }
        }
    }

    // ===== Discard 외부 호출 =====

    /// <summary>
    /// 아이템이 바다에 버려졌을 때 InventoryDragHandler에서 호출.
    /// Discard 퀘스트의 대상 아이템인지 확인한다.
    /// </summary>
    public void NotifyItemDiscarded(ItemInstance item)
    {
        if (!isTracking || activeQuest == null) return;
        if (activeQuest.questType != QuestType.Discard) return;
        if (activeQuest.discardTargetItem == null) return;

        if (item.DefinitionId == activeQuest.discardTargetItem.itemId)
        {
            activeQuest.objectiveCompleted = true;
            Debug.Log($"[Quest] 아이템 투기 완료: {item.Definition.itemName}");
            OnObjectiveUpdated?.Invoke("아이템 투기 완료! 목적지로 이동하세요.");
        }
    }

    // ===== Interact 외부 호출 =====

    /// <summary>
    /// 퀘스트 표류물과 접촉(루팅)했을 때 DriftageController에서 호출.
    /// </summary>
    public void NotifyQuestDriftageInteracted()
    {
        if (!isTracking || activeQuest == null) return;
        if (activeQuest.questType != QuestType.Interact) return;

        activeQuest.objectiveCompleted = true;
        Debug.Log("[Quest] 퀘스트 표류물 접촉 완료");
        OnObjectiveUpdated?.Invoke("표류물 접촉 완료! 목적지로 이동하세요.");
    }

    // ===== 공통 =====

    private void CompleteQuest()
    {
        Debug.Log($"[QuestProgressTracker] 퀘스트 완료: {activeQuest.questId}");
        OnQuestCompleted?.Invoke();
        QuestManager.Instance?.OnQuestCompleted?.Invoke();
        StopTracking();
    }

    private void FailQuest()
    {
        Debug.Log($"[QuestProgressTracker] 퀘스트 실패: {activeQuest.questId}");
        OnQuestFailed?.Invoke();
        QuestManager.Instance?.OnQuestFailed?.Invoke();
        StopTracking();
    }

    private void EmitObjectiveText()
    {
        if (activeQuest == null) return;

        switch (activeQuest.questType)
        {
            case QuestType.Delivery:
                if (!activeQuest.cargoLoaded)
                    OnObjectiveUpdated?.Invoke("경유지에서 화물을 수령하세요.");
                else
                    OnObjectiveUpdated?.Invoke("목적지에 화물을 전달하세요.");
                break;

            case QuestType.Discard:
                if (!activeQuest.objectiveCompleted)
                    OnObjectiveUpdated?.Invoke($"{activeQuest.discardTargetItem?.itemName}을(를) 바다에 버리세요.");
                else
                    OnObjectiveUpdated?.Invoke("목적지로 이동하세요.");
                break;

            case QuestType.Interact:
                if (!activeQuest.objectiveCompleted)
                    OnObjectiveUpdated?.Invoke("표류물과 접촉하세요.");
                else
                    OnObjectiveUpdated?.Invoke("목적지로 이동하세요.");
                break;

            case QuestType.Gather:
                int current = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;
                OnObjectiveUpdated?.Invoke($"골드를 확보하세요. ({current}/{activeQuest.gatherTargetGold})");
                break;
        }
    }
}
