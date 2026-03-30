using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JunkmanHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject junkmanPanel;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI totalValueText;

    private InventoryContainer sellContainer;
    private bool isOpen;

    private void Awake()
    {
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        // 12x12 판매용 임시 컨테이너 생성
        sellContainer = new InventoryContainer(
            "junkman_sell", "판매", InventoryContainerType.Sell, 12, 12
        );

        // 판매 그리드 팝업 열기
        InventoryPopupManager.Instance?.OpenPopup(sellContainer);

        // 카고 팝업도 함께 열기
        var cargo = InventoryManager.Instance?.GetShipCargo();
        if (cargo != null)
            InventoryPopupManager.Instance?.OpenPopup(cargo);

        sellContainer.OnContainerChanged += UpdateTotalValue;

        if (junkmanPanel != null)
            junkmanPanel.SetActive(true);

        UpdateTotalValue();
    }

    public void Close()
    {
        if (!isOpen) return;

        // 미판매 아이템을 카고로 반환
        ReturnUnsoldItems();

        if (sellContainer != null)
        {
            sellContainer.OnContainerChanged -= UpdateTotalValue;
            InventoryPopupManager.Instance?.ClosePopup(sellContainer.ContainerId);
            sellContainer = null;
        }

        isOpen = false;

        if (junkmanPanel != null)
            junkmanPanel.SetActive(false);
    }

    private void OnSellClicked()
    {
        if (sellContainer == null) return;

        var items = sellContainer.GridState.PlacedItems.ToList();
        int totalValue = 0;

        foreach (var item in items)
        {
            if (item.Definition != null && item.Definition.baseValue > 0)
                totalValue += item.Definition.baseValue;

            sellContainer.RemoveItem(item);
        }

        if (totalValue > 0 && PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddGold(totalValue);

        UpdateTotalValue();
    }

    private void ReturnUnsoldItems()
    {
        if (sellContainer == null) return;

        var cargo = InventoryManager.Instance?.GetShipCargo();
        if (cargo == null) return;

        var items = sellContainer.GridState.PlacedItems.ToList();
        foreach (var item in items)
        {
            sellContainer.RemoveItem(item);
            if (!cargo.TryAddItem(item))
            {
                // 카고에 공간이 없으면 다시 판매 그리드에 넣기
                sellContainer.TryAddItem(item);
                Debug.LogWarning("[Junkman] 카고 공간 부족으로 일부 아이템 반환 실패");
                break;
            }
        }
    }

    private void UpdateTotalValue()
    {
        if (totalValueText == null || sellContainer == null) return;

        int total = sellContainer.GridState.PlacedItems
            .Where(i => i.Definition != null && i.Definition.baseValue > 0)
            .Sum(i => i.Definition.baseValue);

        totalValueText.text = $"판매 예상: {total} G";
    }

    private void OnDisable()
    {
        if (isOpen)
            Close();
    }
}
