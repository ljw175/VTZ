using System;

public interface IPartSlotProvider
{
    ShipPartSlotConfig[] GetSlotConfigs();
    bool TryEquipPart(string slotId, ShipPartInstance part);
    ShipPartInstance UnequipPart(string slotId);
    ShipPartInstance GetEquippedPart(string slotId);
    event Action<string, ShipPartInstance> OnPartEquipped;
    event Action<string, ShipPartInstance> OnPartUnequipped;
}
