using System.Collections.Generic;

public class ShipPartInstance
{
    public ShipPartDefinition Definition { get; private set; }
    public string AssignedSlotId { get; set; }
    public float CurrentDurability { get; private set; }
    public float MaxDurability { get; private set; }

    public ShipPartInstance(ShipPartDefinition definition, float maxDurability = 100f)
    {
        Definition = definition;
        MaxDurability = maxDurability;
        CurrentDurability = maxDurability;
    }

    public List<StatModifier> GetActiveModifiers()
    {
        if (Definition == null || Definition.statModifiers == null)
            return new List<StatModifier>();

        return new List<StatModifier>(Definition.statModifiers);
    }
}
