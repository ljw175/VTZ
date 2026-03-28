using UnityEngine;

[CreateAssetMenu(fileName = "New Damage Effect", menuName = "ScriptableObjects/ItemEffects/DamageEffect")]
public class DamageEffect : ItemEffectDefinition
{
    [Header("Damage")]
    public int baseDamage = 1;
    public float attackSpeed = 1f;

    public override void Apply(ItemEffectContext context)
    {
        // 전투 시스템 연동 시 구현
    }
}
