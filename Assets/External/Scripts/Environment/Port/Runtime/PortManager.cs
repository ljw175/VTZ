using System;
using System.Collections.Generic;
using UnityEngine;

public class PortManager : MonoBehaviour
{
    public static PortManager Instance { get; private set; }

    [SerializeField] private GameObject portUIPanel;

    private List<PortController> registeredPorts = new List<PortController>();

    public bool IsDocked { get; private set; }

    public event Action<PortDefinition> OnPortOpened;
    public event Action OnPortClosed;

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
            GameTimer.Instance.OnDayChanged += CheckPortEvents;
    }

    private void OnDestroy()
    {
        if (GameTimer.Instance != null)
            GameTimer.Instance.OnDayChanged -= CheckPortEvents;
    }

    public void RegisterPort(PortController port)
    {
        if (!registeredPorts.Contains(port))
            registeredPorts.Add(port);
    }

    public void UnregisterPort(PortController port)
    {
        registeredPorts.Remove(port);
    }

    public void OpenPort(PortDefinition definition)
    {
        if (IsDocked) return;

        IsDocked = true;
        if (portUIPanel != null)
            portUIPanel.SetActive(true);

        OnPortOpened?.Invoke(definition);
    }

    public void ClosePort()
    {
        if (!IsDocked) return;

        IsDocked = false;
        if (portUIPanel != null)
            portUIPanel.SetActive(false);

        OnPortClosed?.Invoke();
    }

    private void CheckPortEvents(int day)
    {
        if (GameTimer.Instance == null) return;

        int year = GameTimer.Instance.CurrentYear;
        int month = GameTimer.Instance.Months;
        int week = GameTimer.Instance.Weeks;

        for (int i = registeredPorts.Count - 1; i >= 0; i--)
        {
            if (registeredPorts[i] == null)
            {
                registeredPorts.RemoveAt(i);
                continue;
            }

            var activeEvents = registeredPorts[i].GetActiveEvents(year, month, week, day);
            // 활성화된 이벤트가 있을 경우 향후 이벤트 시스템에서 처리
            // 현재는 구조만 수립, 실제 이벤트 처리 로직은 향후 확장
        }
    }
}
