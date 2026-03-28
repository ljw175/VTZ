using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory Grid", menuName = "ScriptableObjects/InventoryGridDefinition")]
public class InventoryGridDefinition : ScriptableObject
{
    [Header("Identity")]
    public string gridId;
    public string displayName;
    public InventoryContainerType containerType;

    [Header("Grid Size")]
    [Min(1)] public int width = 8;
    [Min(1)] public int height = 6;
}
