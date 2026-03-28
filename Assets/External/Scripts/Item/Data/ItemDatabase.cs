using System.Collections.Generic;
using UnityEngine;

public static class ItemDatabase
{
    private static Dictionary<string, ItemDefinition> registry;

    public static void Initialize(ItemDefinition[] allItems)
    {
        registry = new Dictionary<string, ItemDefinition>();

        if (allItems == null) return;

        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] == null || string.IsNullOrEmpty(allItems[i].itemId))
            {
                Debug.LogWarning($"[ItemDatabase] Skipping null or unnamed item at index {i}");
                continue;
            }

            if (registry.ContainsKey(allItems[i].itemId))
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate itemId '{allItems[i].itemId}', skipping");
                continue;
            }

            registry[allItems[i].itemId] = allItems[i];

            if (allItems[i].shapeData != null)
                allItems[i].shapeData.BuildRotationCache(allItems[i].maxRotations);
        }

        Debug.Log($"[ItemDatabase] Initialized with {registry.Count} items");
    }

    public static ItemDefinition GetDefinition(string itemId)
    {
        if (registry != null && registry.TryGetValue(itemId, out var def))
            return def;

        Debug.LogWarning($"[ItemDatabase] Item '{itemId}' not found");
        return null;
    }

    public static bool IsInitialized => registry != null;
}
