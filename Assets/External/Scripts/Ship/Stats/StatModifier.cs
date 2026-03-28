using UnityEngine;

[System.Serializable]
public struct StatModifier
{
    public ShipStatType StatType;
    public StatModifierType ModType;
    public float Value;

    public StatModifier(ShipStatType statType, StatModifierType modType, float value)
    {
        StatType = statType;
        ModType = modType;
        Value = value;
    }
}
