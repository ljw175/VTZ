using System.Collections.Generic;
using System.Linq;

public static class ShipStatCalculator
{
    /// <summary>
    /// 단일 스탯의 최종값을 계산한다.
    /// 계산 순서: baseValue + Flat → ×(1 + ΣPercentAdd) → ×(1 + PercentMultiply) 각각 순차 적용
    /// </summary>
    public static float CalculateFinalStat(float baseValue, List<StatModifier> modifiers)
    {
        float flatSum = 0f;
        float percentAddSum = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            switch (modifiers[i].ModType)
            {
                case StatModifierType.Flat:
                    flatSum += modifiers[i].Value;
                    break;
                case StatModifierType.PercentAdd:
                    percentAddSum += modifiers[i].Value;
                    break;
            }
        }

        float finalValue = baseValue + flatSum;
        finalValue *= (1f + percentAddSum);

        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].ModType == StatModifierType.PercentMultiply)
            {
                finalValue *= (1f + modifiers[i].Value);
            }
        }

        return finalValue;
    }

    /// <summary>
    /// ShipDefinition의 기본 스탯과 장착된 파츠의 수정자를 합산하여 모든 스탯의 최종값을 계산한다.
    /// </summary>
    public static Dictionary<ShipStatType, float> CalculateAllStats(
        ShipDefinition definition,
        IEnumerable<ShipPartInstance> equippedParts)
    {
        var allModifiers = new Dictionary<ShipStatType, List<StatModifier>>();

        if (equippedParts != null)
        {
            foreach (var part in equippedParts)
            {
                if (part == null) continue;

                var partModifiers = part.GetActiveModifiers();
                foreach (var mod in partModifiers)
                {
                    if (!allModifiers.ContainsKey(mod.StatType))
                    {
                        allModifiers[mod.StatType] = new List<StatModifier>();
                    }
                    allModifiers[mod.StatType].Add(mod);
                }
            }
        }

        var result = new Dictionary<ShipStatType, float>();
        var statTypes = System.Enum.GetValues(typeof(ShipStatType));

        foreach (ShipStatType statType in statTypes)
        {
            float baseValue = definition.GetBaseStat(statType);
            if (allModifiers.TryGetValue(statType, out var mods))
            {
                result[statType] = CalculateFinalStat(baseValue, mods);
            }
            else
            {
                result[statType] = baseValue;
            }
        }

        return result;
    }
}
