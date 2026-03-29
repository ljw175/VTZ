using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LootEntry
{
    public ItemDefinition item;
    public float weight;
    public int minCount;
    public int maxCount;
}

[CreateAssetMenu(fileName = "New LootTable", menuName = "ScriptableObjects/LootTableDefinition")]
public class LootTableDefinition : ScriptableObject
{
    public LootEntry[] entries;

    [Tooltip("테이블 최소 굴림 횟수")]
    [Min(0)] public int minRollCount = 1;

    [Tooltip("테이블 최대 굴림 횟수")]
    [Min(0)] public int maxRollCount = 1;

    [Tooltip("같은 아이템 중복 등장 허용")]
    public bool allowDuplicates = true;

    public List<ItemInstance> Roll()
    {
        var results = new List<ItemInstance>();
        if (entries == null || entries.Length == 0) return results;

        float totalWeight = 0f;
        for (int i = 0; i < entries.Length; i++)
            totalWeight += entries[i].weight;

        if (totalWeight <= 0f) return results;

        HashSet<int> usedIndices = allowDuplicates ? null : new HashSet<int>();

        int rollCount = UnityEngine.Random.Range(minRollCount, maxRollCount + 1);

        for (int r = 0; r < rollCount; r++)
        {
            int picked = PickWeighted(totalWeight, usedIndices);
            if (picked < 0) break;

            var entry = entries[picked];
            if (entry.item == null) continue;

            int count = UnityEngine.Random.Range(
                Mathf.Max(1, entry.minCount),
                Mathf.Max(1, entry.maxCount) + 1
            );

            for (int c = 0; c < count; c++)
                results.Add(new ItemInstance(entry.item));

            if (usedIndices != null)
                usedIndices.Add(picked);
        }

        return results;
    }

    private int PickWeighted(float totalWeight, HashSet<int> excluded)
    {
        float adjustedTotal = totalWeight;
        if (excluded != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (excluded.Contains(i))
                    adjustedTotal -= entries[i].weight;
            }
        }

        if (adjustedTotal <= 0f) return -1;

        float roll = UnityEngine.Random.Range(0f, adjustedTotal);
        float cumulative = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            if (excluded != null && excluded.Contains(i)) continue;

            cumulative += entries[i].weight;
            if (roll < cumulative)
                return i;
        }

        return entries.Length - 1;
    }
}
