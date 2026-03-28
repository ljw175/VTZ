using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Modifier Effect", menuName = "ScriptableObjects/ItemEffects/StatModifierEffect")]
public class StatModifierEffect : ItemEffectDefinition
{
    [Header("Stat Modifiers")]
    public StatModifier[] modifiers;

    public override void Apply(ItemEffectContext context)
    {
        if (context.ShipState == null || modifiers == null || modifiers.Length == 0) return;

        context.ShipState.AddEnvironmentalModifiers(this, new List<StatModifier>(modifiers));
    }

    public override void Remove(ItemEffectContext context)
    {
        if (context.ShipState == null) return;

        context.ShipState.RemoveEnvironmentalModifiers(this);
    }
}
