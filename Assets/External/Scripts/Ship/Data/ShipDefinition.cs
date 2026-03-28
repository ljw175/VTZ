using UnityEngine;

[System.Serializable]
public struct ShipPartSlotConfig
{
    public string slotId;
    public PartSlotType slotType;
    public bool isRequired;
}

[CreateAssetMenu(fileName = "New Ship Definition", menuName = "ScriptableObjects/ShipDefinition", order = 2)]
public class ShipDefinition : ScriptableObject
{
    [Header("Identity")]
    public string shipName;
    [TextArea] public string description;
    public Sprite shipSprite;
    public GameObject shipPrefab;

    [Header("Classification")]
    public ShipSize size;
    public ShipPurpose purpose;

    [Header("Base Stats - Movement")]
    public float baseSpeed = 0f;
    public float baseAcceleration = 2f;
    public float baseTurnSpeed = 0.5f;
    public float baseWaterFriction = 1.5f;

    [Header("Base Stats - Durability")]
    public int baseHullHp = 10;

    [Header("Base Stats - Capacity")]
    public float baseCargoCapacity = 10f;
    public int baseCrewCapacity = 5;
    public float baseFuelEfficiency = 1f;

    [Header("Fate Sync")]
    public float baseDetachThreshold = 3.0f;
    public float baseAttachThreshold = 2.0f;
    public float baseMaxFateDistance = 15f;
    public float baseSlipstreamRadius = 2.0f;
    public float baseSlipstreamMultiplier = 1.2f;

    [Header("Combat")]
    public int baseCannonDamage = 1;
    public float baseCannonCooldown = 0.5f;
    public float baseCannonRange = 15f;

    [Header("Visibility")]
    public float baseEnemyVisibilityRadius = 10f;

    [Header("Part Slots")]
    public ShipPartSlotConfig[] partSlots;

    public float GetBaseStat(ShipStatType statType)
    {
        switch (statType)
        {
            case ShipStatType.Speed:                return baseSpeed;
            case ShipStatType.Acceleration:         return baseAcceleration;
            case ShipStatType.TurnSpeed:            return baseTurnSpeed;
            case ShipStatType.HullHp:               return baseHullHp;
            case ShipStatType.CargoCapacity:        return baseCargoCapacity;
            case ShipStatType.CrewCapacity:         return baseCrewCapacity;
            case ShipStatType.FuelEfficiency:       return baseFuelEfficiency;
            case ShipStatType.CannonDamage:         return baseCannonDamage;
            case ShipStatType.CannonCooldown:       return baseCannonCooldown;
            case ShipStatType.CannonRange:          return baseCannonRange;
            case ShipStatType.DetachThreshold:      return baseDetachThreshold;
            case ShipStatType.AttachThreshold:      return baseAttachThreshold;
            case ShipStatType.MaxFateDistance:       return baseMaxFateDistance;
            case ShipStatType.SlipstreamRadius:     return baseSlipstreamRadius;
            case ShipStatType.SlipstreamMultiplier: return baseSlipstreamMultiplier;
            case ShipStatType.WaterFriction:        return baseWaterFriction;
            case ShipStatType.EnemyVisibilityRadius: return baseEnemyVisibilityRadius;
            default:                                return 0f;
        }
    }
}
