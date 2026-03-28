using UnityEngine;

[CreateAssetMenu(fileName = "New Ship Part", menuName = "ScriptableObjects/ShipPartDefinition", order = 3)]
public class ShipPartDefinition : ScriptableObject
{
    [Header("Identity")]
    public string partName;
    [TextArea] public string description;
    public Sprite partIcon;

    [Header("Classification")]
    public PartSlotType slotType;
    public PartTier tier;

    [Header("Stat Modifiers")]
    public StatModifier[] statModifiers;

    [Header("Economy")]
    public int baseCost;
    public int sellValue;

    [Header("Requirements")]
    public ShipSize minimumShipSize;
    public int requiredCrewToOperate;
}
