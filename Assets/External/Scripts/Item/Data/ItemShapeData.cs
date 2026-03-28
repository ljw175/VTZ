using System;
using UnityEngine;

[Serializable]
public class ItemShapeData
{
    [Tooltip("아이템 형태를 구성하는 셀 오프셋 (앵커 기준, 좌상단 = 0,0)")]
    public Vector2Int[] baseOffsets = new Vector2Int[] { Vector2Int.zero };

    [NonSerialized] private Vector2Int[][] rotationCache;

    public void BuildRotationCache(int maxRotations)
    {
        maxRotations = Mathf.Clamp(maxRotations, 1, 4);
        rotationCache = new Vector2Int[maxRotations][];

        Vector2Int[] current = NormalizeOffsets(baseOffsets);
        rotationCache[0] = current;

        for (int i = 1; i < maxRotations; i++)
        {
            current = RotateCW(current);
            rotationCache[i] = current;
        }
    }

    public Vector2Int[] GetRotatedOffsets(int rotationIndex)
    {
        if (rotationCache == null || rotationCache.Length == 0)
            BuildRotationCache(1);

        rotationIndex = Mathf.Clamp(rotationIndex, 0, rotationCache.Length - 1);
        return rotationCache[rotationIndex];
    }

    public Vector2Int GetBounds(int rotationIndex)
    {
        Vector2Int[] offsets = GetRotatedOffsets(rotationIndex);
        int maxX = 0, maxY = 0;

        for (int i = 0; i < offsets.Length; i++)
        {
            if (offsets[i].x > maxX) maxX = offsets[i].x;
            if (offsets[i].y > maxY) maxY = offsets[i].y;
        }

        return new Vector2Int(maxX + 1, maxY + 1);
    }

    public int CellCount => baseOffsets != null ? baseOffsets.Length : 0;

    /// <summary>
    /// 90도 시계 방향 회전: (x, y) → (y, -x) 후 정규화.
    /// </summary>
    private static Vector2Int[] RotateCW(Vector2Int[] offsets)
    {
        Vector2Int[] rotated = new Vector2Int[offsets.Length];

        for (int i = 0; i < offsets.Length; i++)
        {
            rotated[i] = new Vector2Int(offsets[i].y, -offsets[i].x);
        }

        return NormalizeOffsets(rotated);
    }

    /// <summary>
    /// 모든 오프셋의 최소 좌표를 (0, 0)으로 이동.
    /// </summary>
    private static Vector2Int[] NormalizeOffsets(Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0)
            return new Vector2Int[] { Vector2Int.zero };

        int minX = int.MaxValue, minY = int.MaxValue;

        for (int i = 0; i < offsets.Length; i++)
        {
            if (offsets[i].x < minX) minX = offsets[i].x;
            if (offsets[i].y < minY) minY = offsets[i].y;
        }

        Vector2Int[] normalized = new Vector2Int[offsets.Length];

        for (int i = 0; i < offsets.Length; i++)
        {
            normalized[i] = new Vector2Int(offsets[i].x - minX, offsets[i].y - minY);
        }

        return normalized;
    }
}
