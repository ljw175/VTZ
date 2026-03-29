using UnityEngine;

[CreateAssetMenu(fileName = "New Driftage", menuName = "ScriptableObjects/DriftageDefinition")]
public class DriftageDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public string promptText = "E: 조사하기";

    [Header("Loot")]
    public LootTableDefinition lootTable;

    [Header("Loot Container Grid")]
    [Min(1)] public int lootGridWidth = 4;
    [Min(1)] public int lootGridHeight = 3;
}
 