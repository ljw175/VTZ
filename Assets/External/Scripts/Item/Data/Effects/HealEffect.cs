using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Effect", menuName = "ScriptableObjects/ItemEffects/HealEffect")]
public class HealEffect : ItemEffectDefinition
{
    [Header("Heal")]
    public int healAmount = 1;

    public override void Apply(ItemEffectContext context)
    {
        if (context.ShipState == null) return;

        context.ShipState.Heal(healAmount);
    }
}
