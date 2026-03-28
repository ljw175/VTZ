using UnityEngine;

[CreateAssetMenu(fileName = "New Ocean Current", menuName = "ScriptableObjects/OceanCurrentDefinition")]
public class OceanCurrentDefinition : ScriptableObject
{
    [Header("Identity")]
    public string currentName;

    [Header("Seasonal Activity")]
    [Tooltip("활성화 시작 월")] public int activeStartMonth = 1;
    [Tooltip("활성화 시작 일")] public int activeStartDay = 1;
    [Tooltip("활성화 종료 월")] public int activeEndMonth = 12;
    [Tooltip("활성화 종료 일")] public int activeEndDay = 31;

    [Header("Physics")]
    [Tooltip("해류가 오브젝트에 가하는 힘의 크기")]
    public float forceStrength = 5f;
    [Tooltip("해류 위에서의 WaterFriction 변동값 (Flat modifier)")]
    public float waterFrictionModifier = -0.5f;
    [Tooltip("true = 경로 역방향으로 힘 적용")]
    public bool reverseDirection = false;

    [Header("Detection")]
    [Tooltip("경로로부터 수직 감지 거리")]
    public float detectionRadius = 3f;
}
