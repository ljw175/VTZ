using System;
using System.Collections.Generic;
using System.Linq;

public class InventoryContainer : IInventoryContainer
{
    public string ContainerId { get; private set; }
    public InventoryContainerType ContainerType { get; private set; }
    public InventoryGridState GridState { get; private set; }

    private Func<float> weightCapacityProvider;

    public event Action OnContainerChanged;

    public InventoryContainer(InventoryGridDefinition gridDef, Func<float> capacityProvider)
    {
        ContainerId = gridDef.gridId;
        ContainerType = gridDef.containerType;
        GridState = new InventoryGridState(gridDef.width, gridDef.height);
        weightCapacityProvider = capacityProvider;

        GridState.OnGridChanged += () => OnContainerChanged?.Invoke();
    }

    public float GetTotalWeight()
    {
        return GridState.GetTotalWeight();
    }

    public float GetWeightCapacity()
    {
        return weightCapacityProvider != null ? weightCapacityProvider() : -1f;
    }

    public bool HasWeightCapacity(float additionalWeight)
    {
        float capacity = GetWeightCapacity();
        if (capacity < 0f) return true; // 무제한

        return GetTotalWeight() + additionalWeight <= capacity;
    }

    public bool TryAddItem(ItemInstance item)
    {
        if (item == null || item.Definition == null) return false;
        if (!HasWeightCapacity(item.Definition.weight)) return false;

        return GridState.TryAutoPick(item);
    }

    public bool TryPlaceItem(ItemInstance item, int x, int y, int rotation)
    {
        if (item == null || item.Definition == null) return false;

        // 이미 이 그리드에 있는 아이템이면 무게 검사에서 자기 무게 제외
        bool alreadyInGrid = item.IsPlaced && GridState.PlacedItems.Contains(item);
        float additionalWeight = alreadyInGrid ? 0f : item.Definition.weight;

        if (!HasWeightCapacity(additionalWeight)) return false;

        // 겹침 검사
        var overlaps = GridState.GetOverlappingItems(item.Definition, x, y, rotation, item.InstanceId);

        if (overlaps.Count == 0)
        {
            // 기존 위치에서 제거 후 새 위치에 배치
            if (alreadyInGrid)
                GridState.LiftItem(item);

            bool placed = GridState.TryPlace(item, x, y, rotation);

            if (!placed && alreadyInGrid)
                GridState.StampItem(item);

            return placed;
        }

        if (overlaps.Count == 1)
        {
            return GridState.TrySwap(item, x, y, rotation);
        }

        return false; // 2개 이상 겹침 → 거부
    }

    public bool RemoveItem(ItemInstance item)
    {
        return GridState.RemoveItem(item);
    }

    public List<ItemInstance> AutoSort()
    {
        return GridState.AutoSort();
    }
}
