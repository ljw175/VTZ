using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDragHandler : MonoBehaviour
{
    public static InventoryDragHandler Instance { get; private set; }

    [SerializeField] private Canvas parentCanvas;

    private InventoryItemUI draggedItemUI;
    private ItemInstance draggedItem;
    private InventoryGridUI sourceGrid;
    private RectTransform ghostTransform;
    private int dragRotation;

    // 드래그 시작 전 원래 상태 (취소용)
    private int origX, origY, origRot;

    public bool IsDragging => draggedItem != null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!IsDragging) return;

        // R키로 드래그 중 회전 (InputAction이 드래그 중 소비될 수 있으므로 직접 체크)
        if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("R pressed");
            TryRotateDragged();
        }

        // 우클릭으로 드래그 취소
        if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelDrag();
        }

        UpdatePreview();
    }

    public void BeginDrag(InventoryItemUI itemUI, PointerEventData eventData)
    {
        draggedItemUI = itemUI;
        draggedItem = itemUI.ItemInstance;
        sourceGrid = itemUI.ParentGrid;
        dragRotation = draggedItem.RotationIndex;

        origX = draggedItem.GridX;
        origY = draggedItem.GridY;
        origRot = draggedItem.RotationIndex;

        // cellMap에서 임시 제거 (이동 검증을 위해)
        sourceGrid.Container.GridState.LiftItem(draggedItem);

        // 고스트 생성
        CreateGhost(itemUI);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (!IsDragging || ghostTransform == null) return;

        // 고스트를 커서 위치로
        if (parentCanvas != null)
        {
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                cam,
                out Vector2 localPos
            );
            ghostTransform.localPosition = localPos;
        }
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        bool placed = false;

        // 현재 커서 아래의 그리드 검색
        InventoryGridUI targetGrid = FindGridUnderCursor(eventData);

        if (targetGrid != null && !targetGrid.Container.IsReadOnly)
        {
            if (targetGrid.ScreenToGridPosition(eventData.position, out int gx, out int gy))
            {
                if (targetGrid == sourceGrid)
                {
                    // 같은 그리드 내 이동
                    placed = targetGrid.Container.TryPlaceItem(draggedItem, gx, gy, dragRotation);
                }
                else
                {
                    // 다른 그리드로 이동 (컨테이너 간 전송)
                    // 1. 소스에서 완전 제거 (Lift 상태이므로 Stamp 후 Remove)
                    sourceGrid.Container.GridState.StampItem(draggedItem);
                    sourceGrid.Container.GridState.RemoveItem(draggedItem);

                    // 2. 타겟에 커서 위치로 배치 시도 → 실패 시 자동 배치
                    placed = targetGrid.Container.TryPlaceItem(draggedItem, gx, gy, dragRotation);
                    if (!placed)
                        placed = targetGrid.Container.TryAddItem(draggedItem);

                    if (placed)
                    {
                        DestroyGhost();
                        CleanupDragState();
                        return;
                    }

                    // 3. 실패: 소스에 직접 복원 (ReadOnly 우회)
                    sourceGrid.Container.GridState.TryPlace(draggedItem, origX, origY, origRot);
                }
            }
        }

        if (!placed)
        {
            // 원래 위치로 복원
            draggedItem.GridX = origX;
            draggedItem.GridY = origY;
            draggedItem.RotationIndex = origRot;
            sourceGrid.Container.GridState.StampItem(draggedItem);
        }

        DestroyGhost();
        ResetAllVisibleGridHighlights();
        CleanupDragState();
    }

    private void CancelDrag()
    {
        if (!IsDragging) return;

        draggedItem.GridX = origX;
        draggedItem.GridY = origY;
        draggedItem.RotationIndex = origRot;
        sourceGrid.Container.GridState.StampItem(draggedItem);

        DestroyGhost();
        ResetAllVisibleGridHighlights();
        CleanupDragState();
    }

    /// <summary>
    /// 외부에서 드래그를 강제 취소 (팝업 닫힘 등)
    /// </summary>
    public void ForceCancelDrag()
    {
        CancelDrag();
    }

    private void UpdatePreview()
    {
        if (!IsDragging || sourceGrid == null) return;

        // 그리드 위에 있으면 배치 미리보기
        InventoryGridUI hoverGrid = FindGridUnderMouse();
        if (hoverGrid != null && hoverGrid.ScreenToGridPosition(InputManager.Instance.MousePos, out int gx, out int gy))
        {
            hoverGrid.ShowPlacementPreview(draggedItem.Definition, gx, gy, dragRotation, draggedItem.InstanceId);
        }
        else
        {
            sourceGrid.ResetAllHighlights();
        }
    }

    private void CreateGhost(InventoryItemUI itemUI)
    {
        if (parentCanvas == null) return;

        GameObject ghost = new GameObject("DragGhost");
        ghost.transform.SetParent(parentCanvas.transform, false);

        var image = ghost.AddComponent<UnityEngine.UI.Image>();
        if (itemUI.ItemInstance.Definition != null && itemUI.ItemInstance.Definition.itemIcon != null)
        {
            image.sprite = itemUI.ItemInstance.Definition.itemIcon;
            image.preserveAspect = true;
        }
        image.color = new Color(1f, 1f, 1f, 0.6f);
        image.raycastTarget = false;

        ghostTransform = ghost.GetComponent<RectTransform>();
        UpdateGhostSize();
    }

    private void TryRotateDragged()
    {
        if (draggedItem == null) return;

        int maxRot = draggedItem.Definition.maxRotations;
        if (maxRot <= 1) return;

        dragRotation = (dragRotation + 1) % maxRot;
        UpdateGhostSize();
        UpdatePreview();
    }

    private void UpdateGhostSize()
    {
        if (ghostTransform == null || draggedItem == null) return;

        Vector2Int bounds = draggedItem.Definition.shapeData.GetBounds(dragRotation);
        float totalCellSize = sourceGrid.CellSize + sourceGrid.CellSpacing;
        ghostTransform.sizeDelta = new Vector2(
            bounds.x * totalCellSize - sourceGrid.CellSpacing,
            bounds.y * totalCellSize - sourceGrid.CellSpacing
        );
    }

    private void DestroyGhost()
    {
        if (ghostTransform != null)
            Destroy(ghostTransform.gameObject);
        ghostTransform = null;
    }

    private void CleanupDragState()
    {
        draggedItemUI = null;
        draggedItem = null;
        sourceGrid = null;
    }

    private InventoryGridUI FindGridUnderCursor(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            var grid = results[i].gameObject.GetComponentInParent<InventoryGridUI>();
            if (grid != null) return grid;
        }

        return null;
    }

    /// <summary>
    /// 열려있는 모든 그리드 팝업의 하이라이트를 초기화
    /// </summary>
    private void ResetAllVisibleGridHighlights()
    {
        if (sourceGrid != null)
            sourceGrid.ResetAllHighlights();

        // 마우스가 다른 그리드 위에 있었을 수 있으므로 해당 그리드도 초기화
        InventoryGridUI hoverGrid = FindGridUnderMouse();
        if (hoverGrid != null && hoverGrid != sourceGrid)
            hoverGrid.ResetAllHighlights();
    }

    private InventoryGridUI FindGridUnderMouse()
    {
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = InputManager.Instance.MousePos
        };
        return FindGridUnderCursor(eventData);
    }
}
