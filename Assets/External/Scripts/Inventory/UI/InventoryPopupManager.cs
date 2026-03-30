using System.Collections.Generic;
using UnityEngine;

public class InventoryPopupManager : MonoBehaviour
{
    public static InventoryPopupManager Instance { get; private set; }

    [Header("Popup Settings")]
    [SerializeField] private GameObject gridPopupPrefab;
    [SerializeField] private Transform popupParent;

    [Header("Spawn Offset")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(50f, -50f);

    private Dictionary<string, InventoryGridPopupUI> openPopups = new Dictionary<string, InventoryGridPopupUI>();
    private int spawnCount;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (InputManager.Instance == null || InventoryManager.Instance == null) return;

        if (InputManager.Instance.CargoToggle.WasPressedThisFrame())
        {
            var cargo = InventoryManager.Instance.GetShipCargo();
            if (cargo != null)
                TogglePopup(cargo);
        }

        if (InputManager.Instance.BackpackToggle.WasPressedThisFrame())
        {
            var backpack = InventoryManager.Instance.GetPlayerBackpack();
            if (backpack != null)
                TogglePopup(backpack);
        }
    }

    public InventoryGridPopupUI OpenPopup(IInventoryContainer container)
    {
        if (container == null || gridPopupPrefab == null) return null;

        if (openPopups.TryGetValue(container.ContainerId, out var existing))
        {
            existing.transform.SetAsLastSibling();
            return existing;
        }

        Transform parent = popupParent != null ? popupParent : transform;
        GameObject obj = Instantiate(gridPopupPrefab, parent);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = spawnOffset * spawnCount;
            spawnCount++;
        }

        var popup = obj.GetComponent<InventoryGridPopupUI>();
        if (popup == null)
        {
            Destroy(obj);
            return null;
        }

        popup.Setup(container);

        var window = obj.GetComponent<DraggableWindow>();
        if (window != null)
            window.OnClose += () => ClosePopup(container.ContainerId);

        openPopups[container.ContainerId] = popup;
        return popup;
    }

    public void ClosePopup(string containerId)
    {
        if (!openPopups.TryGetValue(containerId, out var popup)) return;

        openPopups.Remove(containerId);
        if (popup != null)
            Destroy(popup.gameObject);
    }

    public void TogglePopup(IInventoryContainer container)
    {
        if (container == null) return;

        if (openPopups.ContainsKey(container.ContainerId))
            ClosePopup(container.ContainerId);
        else
            OpenPopup(container);
    }

    public bool IsOpen(string containerId)
    {
        return openPopups.ContainsKey(containerId);
    }

    /// <summary>
    /// 현재 열린 팝업 중 excludeId가 아니고 ReadOnly가 아닌 첫 번째 컨테이너를 반환한다.
    /// Ctrl+클릭 퀵 전송 대상을 찾는 용도.
    /// </summary>
    public IInventoryContainer FindTransferTarget(string excludeContainerId)
    {
        foreach (var kvp in openPopups)
        {
            if (kvp.Key == excludeContainerId) continue;

            var container = kvp.Value.Container;
            if (container != null && !container.IsReadOnly)
                return container;
        }
        return null;
    }
}
