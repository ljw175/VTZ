using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite itemIcon;

    [Header("Classification")]
    public ItemCategory categories;
    public ItemTag tags;

    [Header("Shape")]
    public ItemShapeData shapeData;
    [Range(1, 4)] public int maxRotations = 1;

    [Header("Freshness")]
    [Tooltip("-1 = 부패하지 않는 아이템")]
    public float maxFreshness = -1f;
    [Tooltip("하루당 감소하는 신선도")]
    public float freshnessDecayPerDay = 1f;

    [Header("Weight & Value")]
    public float weight = 1f;
    [Tooltip("전당포 기본 판매 가격")]
    public int baseValue;

    [Header("Effects")]
    [Tooltip("아이템 종류에 따라 다른 효과 할당")]
    public ItemEffectDefinition[] effects;

    public bool IsPerishable => maxFreshness > 0f;
}
