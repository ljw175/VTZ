using System.Collections.Generic;
using UnityEngine;

public class PortController : MonoBehaviour
{
    [SerializeField] private PortDefinition definition;
    [SerializeField] private float entryRange = 5f;
    [SerializeField] private GameObject entryRangeIndicator;
    [SerializeField] private GameObject dockPromptUI;

    private CircleCollider2D entryTrigger;
    private bool playerInRange = false;

    public PortDefinition Definition => definition;

    private void Awake()
    {
        entryTrigger = GetComponent<CircleCollider2D>();
        if (entryTrigger == null)
            entryTrigger = gameObject.AddComponent<CircleCollider2D>();
        entryTrigger.isTrigger = true;
        entryTrigger.radius = entryRange;

        if (entryRangeIndicator != null)
            entryRangeIndicator.transform.localScale = Vector3.one * entryRange * 2f;

        if (dockPromptUI != null)
            dockPromptUI.SetActive(false);
    }

    private void OnEnable()
    {
        if (PortManager.Instance != null)
            PortManager.Instance.RegisterPort(this);
    }

    private void OnDisable()
    {
        if (PortManager.Instance != null)
            PortManager.Instance.UnregisterPort(this);
    }

    private void Update()
    {
        if (playerInRange && InputManager.Instance.Interact.WasPressedThisFrame())
        {
            if (PortManager.Instance != null)
                PortManager.Instance.OpenPort(definition);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (dockPromptUI != null)
            dockPromptUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (dockPromptUI != null)
            dockPromptUI.SetActive(false);
    }

    /// <summary>
    /// 현재 날짜에 활성화된 이벤트 목록 반환.
    /// </summary>
    public List<PortEventSchedule> GetActiveEvents(int year, int month, int week, int day)
    {
        var activeEvents = new List<PortEventSchedule>();
        if (definition == null || definition.eventSchedules == null) return activeEvents;

        for (int i = 0; i < definition.eventSchedules.Length; i++)
        {
            if (definition.eventSchedules[i].IsActiveOn(year, month, week, day))
                activeEvents.Add(definition.eventSchedules[i]);
        }

        return activeEvents;
    }
}
