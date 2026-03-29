using System;
using UnityEngine;

public class DriftageController : LootableObject
{
    [Header("Driftage")]
    [SerializeField] private DriftageDefinition definition;

    private InventoryContainer lootContainer;
    private bool isLooted;
    private int initialItemCount;

    public override string PromptText => definition != null ? definition.promptText : "E: 조사하기";
    public override bool CanLoot => !isLooted && definition != null && definition.lootTable != null;

    public void SetDefinition(DriftageDefinition def)
    {
        definition = def;
    }

    protected override void OnLoot()
    {
        isLooted = true;

        var items = definition.lootTable.Roll();
        if (items.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        string containerId = $"Loot_{Guid.NewGuid().ToString().Substring(0, 8)}";
        lootContainer = new InventoryContainer(
            containerId,
            definition.displayName,
            InventoryContainerType.Loot,
            definition.lootGridWidth,
            definition.lootGridHeight,
            isReadOnly: true
        );

        for (int i = 0; i < items.Count; i++)
            lootContainer.GridState.TryAutoPick(items[i]);

        initialItemCount = lootContainer.GridState.PlacedItems.Count;

        if (InventoryPopupManager.Instance != null)
        {
            var popup = InventoryPopupManager.Instance.OpenPopup(lootContainer);
            if (popup != null)
            {
                var window = popup.GetComponent<DraggableWindow>();
                if (window != null)
                    window.OnClose += OnLootPopupClosed;
            }
        }
    }

    private void OnLootPopupClosed()
    {
        bool itemTaken = lootContainer.GridState.PlacedItems.Count < initialItemCount;
        lootContainer = null;

        if (itemTaken)
        {
            Destroy(gameObject);
        }
        else
        {
            // 아이템을 꺼내지 않았으면 다시 루팅 가능
            isLooted = false;
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        if (!other.CompareTag("Player")) return;

        if (!isLooted || lootContainer == null || InventoryPopupManager.Instance == null) return;

        // 드래그 중이면 먼저 취소하여 고스트/아이템 상태를 정리
        if (InventoryDragHandler.Instance != null && InventoryDragHandler.Instance.IsDragging)
            InventoryDragHandler.Instance.ForceCancelDrag();

        InventoryPopupManager.Instance.ClosePopup(lootContainer.ContainerId);
    }
}
