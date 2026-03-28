using UnityEngine;

[CreateAssetMenu(fileName = "New Port", menuName = "ScriptableObjects/PortDefinition")]
public class PortDefinition : ScriptableObject
{
    [Header("Identity")]
    public string portName;
    [TextArea] public string description;
    public Sprite portIcon;

    [Header("Location")]
    [Tooltip("WorldMap 기준 좌표 (임무 경로 설정에 사용)")]
    public Vector2 worldPosition;

    [Header("Facilities")]
    public PortFacilityType[] availableFacilities;

    [Header("Events")]
    [Tooltip("특정 주기(년/월/주/일)마다 활성화되는 이벤트 목록")]
    public PortEventSchedule[] eventSchedules;
}
