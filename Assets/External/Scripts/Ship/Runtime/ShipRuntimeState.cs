using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipRuntimeState : IShipStatsProvider, IPartSlotProvider, IFateSyncModifier
{
    public ShipDefinition Definition { get; private set; }

    // --- Stat Cache ---
    private Dictionary<ShipStatType, float> cachedStats = new Dictionary<ShipStatType, float>();
    private bool isDirty = true;

    // --- Part Slots ---
    private Dictionary<string, ShipPartInstance> equippedParts = new Dictionary<string, ShipPartInstance>();

    // --- Environmental Modifiers (weather, ocean currents, etc.) ---
    private Dictionary<object, List<StatModifier>> envModifierSources = new Dictionary<object, List<StatModifier>>();

    // --- HP ---
    public int CurrentHp { get; private set; }
    public int MaxHp => Mathf.RoundToInt(GetStat(ShipStatType.HullHp));

    // --- IFateSyncModifier ---
    public float DetachThreshold => GetStat(ShipStatType.DetachThreshold);
    public float AttachThreshold => GetStat(ShipStatType.AttachThreshold);
    public float MaxFateDistance => GetStat(ShipStatType.MaxFateDistance);
    public float SlipstreamRadius => GetStat(ShipStatType.SlipstreamRadius);

    // --- Events ---
    public event Action<int, int> OnHpChanged;
    public event Action OnStatsRecalculated;
    public event Action<string, ShipPartInstance> OnPartEquipped;
    public event Action<string, ShipPartInstance> OnPartUnequipped;

    public ShipRuntimeState(ShipDefinition definition)
    {
        Definition = definition;
        RecalculateStats();
        CurrentHp = MaxHp;
    }

    // --- Stat Access ---

    public float GetStat(ShipStatType statType)
    {
        if (isDirty)
        {
            RecalculateStats();
        }

        return cachedStats.TryGetValue(statType, out float value) ? value : 0f;
    }

    // --- Environmental Modifier Access ---

    public void AddEnvironmentalModifiers(object source, List<StatModifier> modifiers)
    {
        envModifierSources[source] = modifiers;
        MarkDirty();
    }

    public void RemoveEnvironmentalModifiers(object source)
    {
        if (envModifierSources.Remove(source))
            MarkDirty();
    }

    private IEnumerable<StatModifier> GatherEnvironmentalModifiers()
    {
        foreach (var kvp in envModifierSources)
        {
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                yield return kvp.Value[i];
            }
        }
    }

    private void RecalculateStats()
    {
        IEnumerable<StatModifier> envMods = envModifierSources.Count > 0 ? GatherEnvironmentalModifiers() : null;
        cachedStats = ShipStatCalculator.CalculateAllStats(Definition, equippedParts.Values, envMods);
        isDirty = false;

        // MaxHp가 변경되었을 수 있으므로, CurrentHp가 새 MaxHp를 초과하지 않도록 클램프
        int newMax = Mathf.RoundToInt(cachedStats.TryGetValue(ShipStatType.HullHp, out float hp) ? hp : 0f);
        if (CurrentHp > newMax)
        {
            CurrentHp = newMax;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        OnStatsRecalculated?.Invoke();
    }

    private void MarkDirty()
    {
        isDirty = true;
    }

    // --- HP Management ---

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    public void SetHpToMax()
    {
        CurrentHp = MaxHp;
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    // --- Part Slot Management ---

    public ShipPartSlotConfig[] GetSlotConfigs()
    {
        return Definition.partSlots ?? System.Array.Empty<ShipPartSlotConfig>();
    }

    public bool TryEquipPart(string slotId, ShipPartInstance part)
    {
        if (part == null || string.IsNullOrEmpty(slotId)) return false;

        // 슬롯 유효성 확인
        var slotConfig = FindSlotConfig(slotId);
        if (slotConfig == null) return false;

        // 슬롯 타입 일치 확인
        if (slotConfig.Value.slotType != part.Definition.slotType) return false;

        // 선박 크기 요구사항 확인
        if (part.Definition.minimumShipSize > Definition.size) return false;

        // 기존 파츠가 있으면 해제
        if (equippedParts.ContainsKey(slotId))
        {
            UnequipPart(slotId);
        }

        part.AssignedSlotId = slotId;
        equippedParts[slotId] = part;
        MarkDirty();
        OnPartEquipped?.Invoke(slotId, part);

        return true;
    }

    public ShipPartInstance UnequipPart(string slotId)
    {
        if (!equippedParts.TryGetValue(slotId, out var part)) return null;

        equippedParts.Remove(slotId);
        part.AssignedSlotId = null;
        MarkDirty();
        OnPartUnequipped?.Invoke(slotId, part);

        return part;
    }

    public ShipPartInstance GetEquippedPart(string slotId)
    {
        return equippedParts.TryGetValue(slotId, out var part) ? part : null;
    }

    public IReadOnlyDictionary<string, ShipPartInstance> GetAllEquippedParts()
    {
        return equippedParts;
    }

    private ShipPartSlotConfig? FindSlotConfig(string slotId)
    {
        if (Definition.partSlots == null) return null;

        for (int i = 0; i < Definition.partSlots.Length; i++)
        {
            if (Definition.partSlots[i].slotId == slotId)
            {
                return Definition.partSlots[i];
            }
        }

        return null;
    }
}
