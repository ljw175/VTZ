using System.Collections.Generic;
using UnityEngine;

public enum QuestType
{
    Delivery,   // 배달
    Discard,    // 버리기
    Interact,   // 특정 표류물 접촉
    Gather      // 재화 확보
}

[System.Serializable]
public class QuestData
{
    public string questId;                  // (1) Quest id
    public ZodiacSign providerZodiac;       // (2) Quest를 제공하는 별자리
    public QuestType questType;             // (3) Quest Type

    public float totalTimeLimit;            // 임무 전체 기한 (일수 또는 초)

    // (4) Quest Route (경유지가 포함된 목적지 목록, 예: 출발지 -> 무작위항구A -> 목적지)
    public List<Transform> targetLocations = new List<Transform>();

    // (5) Quest Schedule (계산된 궤적 웨이포인트와 각 포인트 도달 권장 기한)
    // 인스펙터에서는 숨기고, 런타임에 시스템이 채워 넣습니다.
    [HideInInspector]
    public List<WaypointData> calculatedRoute = new List<WaypointData>();

    // ===== 타입별 설정 =====

    [Header("Delivery Settings")]
    [Tooltip("배달할 화물 아이템 (첫 경유지 입항 시 자동 적재, 목적지 입항 시 전달)")]
    public ItemDefinition deliveryItem;

    [Header("Discard Settings")]
    [Tooltip("바다에 버려야 하는 아이템")]
    public ItemDefinition discardTargetItem;

    [Header("Interact Settings")]
    [Tooltip("접촉 대상 표류물의 DriftageDefinition (퀘스트 할당 시 경로 상에 강제 스폰)")]
    public DriftageDefinition interactDriftageDefinition;
    [Tooltip("표류물 스폰에 사용할 프리팹")]
    public GameObject interactDriftagePrefab;

    [Header("Gather Settings")]
    [Tooltip("목적지 입항 시 보유해야 할 최소 골드")]
    public int gatherTargetGold;

    // ===== 런타임 상태 (인스펙터 숨김) =====

    /// <summary>타입별 목표 달성 여부 (바다 투기 완료, 표류물 접촉 완료 등)</summary>
    [HideInInspector] public bool objectiveCompleted;

    /// <summary>Delivery: 화물 적재 완료 여부</summary>
    [HideInInspector] public bool cargoLoaded;

    /// <summary>Interact: 강제 스폰된 퀘스트 표류물 참조</summary>
    [HideInInspector] public GameObject spawnedQuestDriftage;
}

[System.Serializable]
public struct WaypointData
{
    public Vector2 position;       // 웨이포인트(TracePoint) 위치
    public float deadline;         // 이 지점까지 도달해야 하는 슬립스트림 기한
}
