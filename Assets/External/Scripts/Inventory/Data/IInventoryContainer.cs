using System;
using System.Collections.Generic;

public interface IInventoryContainer
{
    string ContainerId { get; }
    string DisplayName { get; }
    InventoryContainerType ContainerType { get; }
    InventoryGridState GridState { get; }

    event Action OnContainerChanged;

    bool TryAddItem(ItemInstance item);
    bool TryPlaceItem(ItemInstance item, int x, int y, int rotation);
    bool RemoveItem(ItemInstance item);

    float GetTotalWeight();
    float GetWeightCapacity();
    bool HasWeightCapacity(float additionalWeight);

    List<ItemInstance> AutoSort();
}
