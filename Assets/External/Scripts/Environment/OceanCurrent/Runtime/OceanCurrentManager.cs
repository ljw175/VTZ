using System.Collections.Generic;
using UnityEngine;

public class OceanCurrentManager : MonoBehaviour
{
    public static OceanCurrentManager Instance { get; private set; }

    private List<OceanCurrentController> registeredCurrents = new List<OceanCurrentController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged -= HandleDayChanged;
    }

    public void RegisterCurrent(OceanCurrentController current)
    {
        if (!registeredCurrents.Contains(current))
            registeredCurrents.Add(current);
    }

    public void UnregisterCurrent(OceanCurrentController current)
    {
        registeredCurrents.Remove(current);
    }

    private void HandleDayChanged(int day)
    {
        if (GameTimer.Instance == null) return;

        int month = GameTimer.Instance.Months;

        for (int i = registeredCurrents.Count - 1; i >= 0; i--)
        {
            if (registeredCurrents[i] == null)
            {
                registeredCurrents.RemoveAt(i);
                continue;
            }

            var def = registeredCurrents[i].Definition;
            if (def == null) continue;

            bool shouldBeActive = IsDateInRange(
                month, day,
                def.activeStartMonth, def.activeStartDay,
                def.activeEndMonth, def.activeEndDay
            );

            registeredCurrents[i].SetActiveState(shouldBeActive);
        }
    }

    /// <summary>
    /// 날짜가 범위 내에 있는지 확인. 연도 경계 wrap-around 지원.
    /// </summary>
    private bool IsDateInRange(int month, int day, int startMonth, int startDay, int endMonth, int endDay)
    {
        int current = month * 100 + day;
        int start = startMonth * 100 + startDay;
        int end = endMonth * 100 + endDay;

        if (start <= end)
            return current >= start && current <= end;
        else
            return current >= start || current <= end;
    }
}
