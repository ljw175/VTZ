using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGridUI : MonoBehaviour
{
    [Header("Cell Setup")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private float cellSize = 64f;
    [SerializeField] private float cellSpacing = 2f;

    private IInventoryContainer container;
    private InventoryCellUI[,] cells;
    private Dictionary<string, InventoryItemUI> itemUIMap = new Dictionary<string, InventoryItemUI>();

    [SerializeField] private GameObject itemUIPrefab;

    public IInventoryContainer Container => container;
    public float CellSize => cellSize;
    public float CellSpacing => cellSpacing;

    public void Initialize(IInventoryContainer container)
    {
        this.container = container;
        BuildGrid();
        RefreshItems();

        container.GridState.OnGridChanged += RefreshItems;
    }

    private void OnDestroy()
    {
        if (container != null)
            container.GridState.OnGridChanged -= RefreshItems;
    }

    private void BuildGrid()
    {
        int w = container.GridState.Width;
        int h = container.GridState.Height;
        cells = new InventoryCellUI[w, h];

        float totalCellSize = cellSize + cellSpacing;
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w * totalCellSize, h * totalCellSize);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                GameObject cellObj = Instantiate(cellPrefab, transform);
                RectTransform cellRt = cellObj.GetComponent<RectTransform>();
                cellRt.sizeDelta = new Vector2(cellSize, cellSize);
                cellRt.anchorMin = new Vector2(0, 1);
                cellRt.anchorMax = new Vector2(0, 1);
                cellRt.pivot = new Vector2(0, 1);
                cellRt.anchoredPosition = new Vector2(
                    x * totalCellSize,
                    -y * totalCellSize
                );

                var cellUI = cellObj.GetComponent<InventoryCellUI>();
                if (cellUI == null) cellUI = cellObj.AddComponent<InventoryCellUI>();
                cellUI.Initialize(x, y);
                cells[x, y] = cellUI;
            }
        }
    }

    public void RefreshItems()
    {
        // 기존 아이템 UI 제거
        foreach (var kvp in itemUIMap)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        itemUIMap.Clear();

        // 셀 하이라이트 리셋
        ResetAllHighlights();

        if (container == null) return;

        // 배치된 아이템 UI 생성
        var items = container.GridState.PlacedItems;
        for (int i = 0; i < items.Count; i++)
        {
            CreateItemUI(items[i]);
        }

        // 점유된 셀 표시
        UpdateOccupiedCells();
    }

    private void CreateItemUI(ItemInstance item)
    {
        if (itemUIPrefab == null || item == null || !item.IsPlaced) return;

        GameObject obj = Instantiate(itemUIPrefab, transform);
        var itemUI = obj.GetComponent<InventoryItemUI>();
        if (itemUI == null) itemUI = obj.AddComponent<InventoryItemUI>();

        float totalCellSize = cellSize + cellSpacing;
        Vector2Int bounds = item.Definition.shapeData.GetBounds(item.RotationIndex);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(
            item.GridX * totalCellSize,
            -item.GridY * totalCellSize
        );
        rt.sizeDelta = new Vector2(
            bounds.x * totalCellSize - cellSpacing,
            bounds.y * totalCellSize - cellSpacing
        );

        itemUI.Initialize(item, this);
        itemUIMap[item.InstanceId] = itemUI;
    }

    private void UpdateOccupiedCells()
    {
        var items = container.GridState.PlacedItems;
        for (int i = 0; i < items.Count; i++)
        {
            Vector2Int[] occupiedCells = items[i].GetOccupiedCells();
            for (int j = 0; j < occupiedCells.Length; j++)
            {
                int cx = occupiedCells[j].x;
                int cy = occupiedCells[j].y;
                if (cx >= 0 && cx < cells.GetLength(0) && cy >= 0 && cy < cells.GetLength(1))
                    cells[cx, cy].SetHighlight(CellHighlightState.Occupied);
            }
        }
    }

    // --- 드래그 피드백 ---

    public void ShowPlacementPreview(ItemDefinition def, int anchorX, int anchorY, int rotation, string ignoreId = null)
    {
        ResetAllHighlights();
        UpdateOccupiedCells();

        if (def == null || def.shapeData == null) return;

        Vector2Int[] offsets = def.shapeData.GetRotatedOffsets(rotation);
        var overlaps = container.GridState.GetOverlappingItems(def, anchorX, anchorY, rotation, ignoreId);
        bool isSwap = overlaps.Count == 1;
        bool isInvalid = overlaps.Count > 1;

        for (int i = 0; i < offsets.Length; i++)
        {
            int cx = anchorX + offsets[i].x;
            int cy = anchorY + offsets[i].y;

            if (!container.GridState.IsInBounds(cx, cy))
                continue;

            if (isInvalid)
            {
                cells[cx, cy].SetHighlight(CellHighlightState.InvalidPlacement);
            }
            else if (isSwap && cellIsOccupiedByOverlap(cx, cy, overlaps))
            {
                cells[cx, cy].SetHighlight(CellHighlightState.SwapCandidate);
            }
            else if (container.GridState.GetCellOccupant(cx, cy) != null && container.GridState.GetCellOccupant(cx, cy) != ignoreId)
            {
                cells[cx, cy].SetHighlight(CellHighlightState.InvalidPlacement);
            }
            else
            {
                cells[cx, cy].SetHighlight(CellHighlightState.ValidPlacement);
            }
        }
    }

    private bool cellIsOccupiedByOverlap(int cx, int cy, HashSet<string> overlaps)
    {
        string occupant = container.GridState.GetCellOccupant(cx, cy);
        return occupant != null && overlaps.Contains(occupant);
    }

    public void ResetAllHighlights()
    {
        if (cells == null) return;

        for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
                cells[x, y].ResetHighlight();
    }

    // --- 좌표 변환 ---

    public bool ScreenToGridPosition(Vector2 screenPos, out int gridX, out int gridY)
    {
        gridX = -1;
        gridY = -1;

        RectTransform rt = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, cam, out Vector2 localPos))
            return false;

        float totalCellSize = cellSize + cellSpacing;
        gridX = Mathf.FloorToInt(localPos.x / totalCellSize);
        gridY = Mathf.FloorToInt(-localPos.y / totalCellSize);

        return container.GridState.IsInBounds(gridX, gridY);
    }
}
