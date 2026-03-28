using UnityEngine;

public class ItemEffectContext
{
    public ShipRuntimeState ShipState;

    public ItemEffectContext(ShipRuntimeState shipState)
    {
        ShipState = shipState;
    }
}

public abstract class ItemEffectDefinition : ScriptableObject
{
    [TextArea] public string effectDescription;

    public abstract void Apply(ItemEffectContext context);

    public virtual void Remove(ItemEffectContext context) { }
}
