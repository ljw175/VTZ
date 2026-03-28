using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryGridState
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private List<ItemInstance> placedItems = new List<ItemInstance>();
    private string[,] cellMap;

    // 킥 오프셋 (맨해튼 거리 1~2, 가까운 순)
    private static readonly Vector2Int[] KickOffsets = GenerateKickOffsets(2);

    public IReadOnlyList<ItemInstance> PlacedItems => placedItems;

    public event Action OnGridChanged;

    public InventoryGridState(int width, int height)
    {
        Width = width;
        Height = height;
        cellMap = new string[width, height];
    }

    // --- 조회 ---

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public string GetCellOccupant(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return cellMap[x, y];
    }

    public ItemInstance FindByInstanceId(string instanceId)
    {
        return placedItems.FirstOrDefault(item => item.InstanceId == instanceId);
    }

    public float GetTotalWeight()
    {
        return placedItems.Where(item => item.Definition != null).Sum(item => item.Definition.weight);
    }

    // --- 배치 검증 ---

    public bool CanPlace(ItemDefinition def, int anchorX, int anchorY, int rotationIndex, string ignoreInstanceId = null)
    {
        if (def == null || def.shapeData == null) return false;

        Vector2Int[] offsets = def.shapeData.GetRotatedOffsets(rotationIndex);

        for (int i = 0; i < offsets.Length; i++)
        {
            int cx = anchorX + offsets[i].x;
            int cy = anchorY + offsets[i].y;

            if (!IsInBounds(cx, cy)) return false;

            string occupant = cellMap[cx, cy];
            if (occupant != null && occupant != ignoreInstanceId)
                return false;
        }

        return true;
    }

    public HashSet<string> GetOverlappingItems(ItemDefinition def, int anchorX, int anchorY, int rotationIndex, string ignoreInstanceId = null)
    {
        var overlaps = new HashSet<string>();
        if (def == null || def.shapeData == null) return overlaps;

        Vector2Int[] offsets = def.shapeData.GetRotatedOffsets(rotationIndex);

        for (int i = 0; i < offsets.Length; i++)
        {
            int cx = anchorX + offsets[i].x;
            int cy = anchorY + offsets[i].y;

            if (!IsInBounds(cx, cy)) continue;

            string occupant = cellMap[cx, cy];
            if (occupant != null && occupant != ignoreInstanceId)
                overlaps.Add(occupant);
        }

        return overlaps;
    }

    // --- 배치/제거 ---

    public bool TryPlace(ItemInstance item, int anchorX, int anchorY, int rotationIndex)
    {
        if (item == null || item.Definition == null) return false;

        if (!CanPlace(item.Definition, anchorX, anchorY, rotationIndex))
            return false;

        item.GridX = anchorX;
        item.GridY = anchorY;
        item.RotationIndex = rotationIndex;

        StampItem(item);

        if (!placedItems.Contains(item))
            placedItems.Add(item);

        OnGridChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemInstance item)
    {
        if (item == null || !placedItems.Contains(item)) return false;

        LiftItem(item);
        placedItems.Remove(item);

        item.GridX = -1;
        item.GridY = -1;

        OnGridChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// cellMap에서만 임시 제거 (스왑/이동/회전 검증용). placedItems 리스트는 유지.
    /// </summary>
    public void LiftItem(ItemInstance item)
    {
        if (item == null || !item.IsPlaced) return;

        Vector2Int[] cells = item.GetOccupiedCells();
        for (int i = 0; i < cells.Length; i++)
        {
            if (IsInBounds(cells[i].x, cells[i].y) && cellMap[cells[i].x, cells[i].y] == item.InstanceId)
                cellMap[cells[i].x, cells[i].y] = null;
        }
    }

    /// <summary>
    /// cellMap에 현재 위치/회전 기준으로 복원.
    /// </summary>
    public void StampItem(ItemInstance item)
    {
        if (item == null || !item.IsPlaced) return;

        Vector2Int[] cells = item.GetOccupiedCells();
        for (int i = 0; i < cells.Length; i++)
        {
            if (IsInBounds(cells[i].x, cells[i].y))
                cellMap[cells[i].x, cells[i].y] = item.InstanceId;
        }
    }

    // --- 회전 ---

    public bool TryRotate(ItemInstance item)
    {
        if (item == null || item.Definition == null || !item.IsPlaced) return false;

        int maxRot = item.Definition.maxRotations;
        if (maxRot <= 1) return false;

        int nextRot = (item.RotationIndex + 1) % maxRot;
        int origX = item.GridX;
        int origY = item.GridY;
        int origRot = item.RotationIndex;

        LiftItem(item);

        // 현재 위치에서 회전 가능한지 확인
        if (CanPlace(item.Definition, origX, origY, nextRot))
        {
            item.GridX = origX;
            item.GridY = origY;
            item.RotationIndex = nextRot;
            StampItem(item);
            OnGridChanged?.Invoke();
            return true;
        }

        // 킥 로직: 주변 오프셋 탐색
        for (int i = 0; i < KickOffsets.Length; i++)
        {
            int testX = origX + KickOffsets[i].x;
            int testY = origY + KickOffsets[i].y;

            if (CanPlace(item.Definition, testX, testY, nextRot))
            {
                item.GridX = testX;
                item.GridY = testY;
                item.RotationIndex = nextRot;
                StampItem(item);
                OnGridChanged?.Invoke();
                return true;
            }
        }

        // 실패: 원래 상태 복원
        item.GridX = origX;
        item.GridY = origY;
        item.RotationIndex = origRot;
        StampItem(item);
        return false;
    }

    // --- 스왑 ---

    public bool TrySwap(ItemInstance itemA, int targetX, int targetY, int rotA)
    {
        if (itemA == null || itemA.Definition == null) return false;

        var overlaps = GetOverlappingItems(itemA.Definition, targetX, targetY, rotA, itemA.InstanceId);
        if (overlaps.Count != 1) return false;

        string overlapId = null;
        foreach (var id in overlaps) { overlapId = id; break; }

        ItemInstance itemB = FindByInstanceId(overlapId);
        if (itemB == null) return false;

        // 원래 상태 저장
        int oldAx = itemA.GridX, oldAy = itemA.GridY, oldArot = itemA.RotationIndex;
        int oldBx = itemB.GridX, oldBy = itemB.GridY, oldBrot = itemB.RotationIndex;

        LiftItem(itemA);
        LiftItem(itemB);

        // A를 목표 위치에 배치
        itemA.GridX = targetX;
        itemA.GridY = targetY;
        itemA.RotationIndex = rotA;
        StampItem(itemA);

        // B를 A의 원래 위치에 배치 시도 (모든 회전)
        int maxRotB = itemB.Definition.maxRotations;
        for (int rot = 0; rot < maxRotB; rot++)
        {
            if (CanPlace(itemB.Definition, oldAx, oldAy, rot))
            {
                itemB.GridX = oldAx;
                itemB.GridY = oldAy;
                itemB.RotationIndex = rot;
                StampItem(itemB);
                OnGridChanged?.Invoke();
                return true;
            }
        }

        // 스왑 실패: 전체 롤백
        LiftItem(itemA);

        itemA.GridX = oldAx;
        itemA.GridY = oldAy;
        itemA.RotationIndex = oldArot;
        StampItem(itemA);

        itemB.GridX = oldBx;
        itemB.GridY = oldBy;
        itemB.RotationIndex = oldBrot;
        StampItem(itemB);

        return false;
    }

    // --- 자동 배치 (Auto-Pick) ---

    public bool TryAutoPick(ItemInstance item)
    {
        if (item == null || item.Definition == null) return false;

        int maxRot = item.Definition.maxRotations;

        for (int rot = 0; rot < maxRot; rot++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (CanPlace(item.Definition, x, y, rot))
                    {
                        return TryPlace(item, x, y, rot);
                    }
                }
            }
        }

        return false;
    }

    // --- 자동 정렬 (Auto-Sort) ---

    public List<ItemInstance> AutoSort()
    {
        // 모든 아이템 수거
        var items = new List<ItemInstance>(placedItems);
        ClearAll();

        // 정렬: 셀 수 DESC → 무게 DESC → 이름 ASC
        var sorted = items
            .OrderByDescending(item => GetCellCount(item))
            .ThenByDescending(item => item.Definition != null ? item.Definition.weight : 0f)
            .ThenBy(item => item.Definition != null ? item.Definition.itemName : "")
            .ToList();

        // 하나씩 배치
        var overflow = sorted.Where(item => !TryAutoPick(item)).ToList();

        OnGridChanged?.Invoke();
        return overflow;
    }

    // --- 유틸리티 ---

    public void ClearAll()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                cellMap[x, y] = null;

        foreach (var item in placedItems)
        {
            item.GridX = -1;
            item.GridY = -1;
        }

        placedItems.Clear();
    }

    public void RebuildCellMap()
    {
        cellMap = new string[Width, Height];

        for (int i = 0; i < placedItems.Count; i++)
        {
            if (placedItems[i].IsPlaced)
                StampItem(placedItems[i]);
        }
    }

    private static int GetCellCount(ItemInstance item)
    {
        if (item?.Definition?.shapeData == null) return 1;
        return item.Definition.shapeData.CellCount;
    }

    private static Vector2Int[] GenerateKickOffsets(int maxDist)
    {
        var list = new List<Vector2Int>();

        for (int dist = 1; dist <= maxDist; dist++)
        {
            for (int dx = -dist; dx <= dist; dx++)
            {
                for (int dy = -dist; dy <= dist; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) == dist)
                        list.Add(new Vector2Int(dx, dy));
                }
            }
        }

        return list.ToArray();
    }
}
