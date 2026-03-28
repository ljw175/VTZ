using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    public string DefinitionId;
    public string InstanceId;
    public float CurrentFreshness;
    public int GridX = -1;
    public int GridY = -1;
    public int RotationIndex;

    [NonSerialized] private ItemDefinition cachedDefinition;
    [NonSerialized] private FreshnessState previousFreshnessState;

    public ItemDefinition Definition
    {
        get
        {
            if (cachedDefinition == null)
                cachedDefinition = ItemDatabase.GetDefinition(DefinitionId);
            return cachedDefinition;
        }
    }

    public bool IsPlaced => GridX >= 0 && GridY >= 0;

    [NonSerialized] public Action<FreshnessState> OnFreshnessStateChanged;

    public ItemInstance(ItemDefinition def)
    {
        if (def == null) return;

        cachedDefinition = def;
        DefinitionId = def.itemId;
        InstanceId = Guid.NewGuid().ToString();
        CurrentFreshness = def.maxFreshness;
        previousFreshnessState = GetFreshnessState();
    }

    public FreshnessState GetFreshnessState()
    {
        if (Definition == null || !Definition.IsPerishable)
            return FreshnessState.Fresh;

        float ratio = CurrentFreshness / Definition.maxFreshness;

        if (ratio > 0.75f) return FreshnessState.Fresh;
        if (ratio > 0.50f) return FreshnessState.Okay;
        if (ratio > 0.25f) return FreshnessState.Aging;
        return FreshnessState.Rotten;
    }

    public void TickFreshness()
    {
        if (Definition == null || !Definition.IsPerishable) return;
        if (CurrentFreshness <= 0f) return;

        CurrentFreshness = Mathf.Max(0f, CurrentFreshness - Definition.freshnessDecayPerDay);

        FreshnessState newState = GetFreshnessState();
        if (newState != previousFreshnessState)
        {
            previousFreshnessState = newState;
            OnFreshnessStateChanged?.Invoke(newState);
        }
    }

    /// <summary>
    /// 현재 회전 상태의 오프셋 배열 반환 (앵커 기준 상대 좌표).
    /// </summary>
    public Vector2Int[] GetCurrentOffsets()
    {
        if (Definition == null || Definition.shapeData == null)
            return new Vector2Int[] { Vector2Int.zero };

        return Definition.shapeData.GetRotatedOffsets(RotationIndex);
    }

    /// <summary>
    /// 그리드 위 실제 점유 셀 좌표 배열 반환.
    /// </summary>
    public Vector2Int[] GetOccupiedCells()
    {
        Vector2Int[] offsets = GetCurrentOffsets();
        Vector2Int[] cells = new Vector2Int[offsets.Length];

        for (int i = 0; i < offsets.Length; i++)
        {
            cells[i] = new Vector2Int(GridX + offsets[i].x, GridY + offsets[i].y);
        }

        return cells;
    }

    /// <summary>
    /// 역직렬화 후 캐시 복원용.
    /// </summary>
    public void ResolveDefinition()
    {
        cachedDefinition = ItemDatabase.GetDefinition(DefinitionId);
        previousFreshnessState = GetFreshnessState();
    }
}
