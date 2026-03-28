using UnityEngine;

[CreateAssetMenu(fileName = "New Weather State", menuName = "ScriptableObjects/WeatherStateDefinition")]
public class WeatherStateDefinition : ScriptableObject
{
    [Header("Identity")]
    public string stateName;

    [Header("Transitions")]
    [Tooltip("각 전이의 probability 합이 1.0이 되도록 설정")]
    public WeatherTransition[] transitions;

    [Header("Visual")]
    public Color cloudColor = Color.white;
    public float cloudMoveSpeed = 3f;

    [Header("Lifetime")]
    [Tooltip("음수 = 무제한 (경계 재배치는 유지)")]
    public float cloudLifetime = -1f;

    [Header("Effect")]
    [Tooltip("날씨 효과가 미치는 반경")]
    public float effectRadius = 30f;
    [Tooltip("해당 날씨 상태에서 선박에 적용되는 스탯 수정자")]
    public StatModifier[] statEffects;

    [Header("Damage Over Time")]
    [Tooltip("초당 선체 피해 (0 = 없음)")]
    public float hullDamagePerSecond = 0f;
}
