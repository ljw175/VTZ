using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Image iconImage;
    private ItemInstance itemInstance;
    private InventoryGridUI parentGrid;

    public ItemInstance ItemInstance => itemInstance;
    public InventoryGridUI ParentGrid => parentGrid;

    public void Initialize(ItemInstance item, InventoryGridUI grid)
    {
        itemInstance = item;
        parentGrid = grid;

        iconImage = GetComponent<Image>();
        if (iconImage == null)
            iconImage = gameObject.AddComponent<Image>();

        if (item.Definition != null && item.Definition.itemIcon != null)
        {
            iconImage.sprite = item.Definition.itemIcon;
            iconImage.type = Image.Type.Sliced;
            iconImage.preserveAspect = true;
        }

        UpdateFreshnessVisual();
        item.OnFreshnessStateChanged += OnFreshnessChanged;
    }

    private void OnDestroy()
    {
        if (itemInstance != null)
            itemInstance.OnFreshnessStateChanged -= OnFreshnessChanged;
    }

    private void OnFreshnessChanged(FreshnessState state)
    {
        UpdateFreshnessVisual();
    }

    private void UpdateFreshnessVisual()
    {
        if (iconImage == null || itemInstance == null) return;

        FreshnessState state = itemInstance.GetFreshnessState();
        Color tint = state switch
        {
            FreshnessState.Fresh => Color.white,
            FreshnessState.Okay => new Color(0.9f, 0.9f, 0.7f),
            FreshnessState.Aging => new Color(0.8f, 0.7f, 0.4f),
            FreshnessState.Rotten => new Color(0.5f, 0.4f, 0.3f),
            _ => Color.white,
        };

        iconImage.color = tint;
    }

    // --- Ctrl+클릭 퀵 전송 ---

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!eventData.currentInputModule || !UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed) return;
        if (itemInstance == null || parentGrid == null) return;

        var sourceContainer = parentGrid.Container;
        if (sourceContainer == null) return;

        var target = InventoryPopupManager.Instance?.FindTransferTarget(sourceContainer.ContainerId);
        if (target == null) return;

        InventoryManager.Instance?.TransferItem(itemInstance, sourceContainer, target);
    }

    // --- 드래그 ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        InventoryDragHandler.Instance?.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        InventoryDragHandler.Instance?.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        InventoryDragHandler.Instance?.EndDrag(eventData);
    }

}
