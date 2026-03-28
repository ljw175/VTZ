using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Grid UIs")]
    [SerializeField] private InventoryGridUI shipCargoGridUI;
    [SerializeField] private InventoryGridUI playerBackpackGridUI;

    [Header("Tab Buttons")]
    [SerializeField] private Button shipCargoTabButton;
    [SerializeField] private Button backpackTabButton;

    [Header("Auto-Sort Button")]
    [SerializeField] private Button autoSortButton;

    [Header("Weight Display")]
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private Slider weightBar;

    private IInventoryContainer activeContainer;
    private bool isOpen;

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (shipCargoTabButton != null)
            shipCargoTabButton.onClick.AddListener(() => SwitchTab(InventoryContainerType.ShipCargo));

        if (backpackTabButton != null)
            backpackTabButton.onClick.AddListener(() => SwitchTab(InventoryContainerType.PlayerBackpack));

        if (autoSortButton != null)
            autoSortButton.onClick.AddListener(OnAutoSort);

        // 그리드 초기화는 InventoryManager가 준비된 후
        InitializeGrids();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += UpdateWeightDisplay;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= UpdateWeightDisplay;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);

        if (isOpen)
            UpdateWeightDisplay();
    }

    private void InitializeGrids()
    {
        if (InventoryManager.Instance == null) return;

        var shipCargo = InventoryManager.Instance.GetShipCargo();
        if (shipCargo != null && shipCargoGridUI != null)
        {
            shipCargoGridUI.Initialize(shipCargo);
            activeContainer = shipCargo;
        }

        var backpack = InventoryManager.Instance.GetPlayerBackpack();
        if (backpack != null && playerBackpackGridUI != null)
        {
            playerBackpackGridUI.Initialize(backpack);
        }
    }

    private void SwitchTab(InventoryContainerType type)
    {
        if (InventoryManager.Instance == null) return;

        switch (type)
        {
            case InventoryContainerType.ShipCargo:
                activeContainer = InventoryManager.Instance.GetShipCargo();
                if (shipCargoGridUI != null) shipCargoGridUI.gameObject.SetActive(true);
                if (playerBackpackGridUI != null) playerBackpackGridUI.gameObject.SetActive(false);
                break;

            case InventoryContainerType.PlayerBackpack:
                activeContainer = InventoryManager.Instance.GetPlayerBackpack();
                if (shipCargoGridUI != null) shipCargoGridUI.gameObject.SetActive(false);
                if (playerBackpackGridUI != null) playerBackpackGridUI.gameObject.SetActive(true);
                break;
        }

        UpdateWeightDisplay();
    }

    private void OnAutoSort()
    {
        if (activeContainer == null) return;

        var overflow = activeContainer.AutoSort();

        if (overflow.Count > 0)
            Debug.LogWarning($"[Inventory] Auto-sort overflow: {overflow.Count} items could not be placed");
    }

    private void UpdateWeightDisplay()
    {
        if (activeContainer == null) return;

        float currentWeight = activeContainer.GetTotalWeight();
        float maxWeight = activeContainer.GetWeightCapacity();

        if (weightText != null)
        {
            if (maxWeight < 0)
                weightText.text = $"{currentWeight:F1} kg";
            else
                weightText.text = $"{currentWeight:F1} / {maxWeight:F1} kg";
        }

        if (weightBar != null)
        {
            if (maxWeight > 0)
            {
                weightBar.gameObject.SetActive(true);
                weightBar.value = Mathf.Clamp01(currentWeight / maxWeight);
            }
            else
            {
                weightBar.gameObject.SetActive(false);
            }
        }
    }
}
