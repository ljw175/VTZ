using System;

public interface IShipStatsProvider
{
    float GetStat(ShipStatType statType);
    int CurrentHp { get; }
    int MaxHp { get; }
    event Action<int, int> OnHpChanged;
    event Action OnStatsRecalculated;
}
