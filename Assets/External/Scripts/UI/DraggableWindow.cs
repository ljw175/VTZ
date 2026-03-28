using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    [SerializeField] private RectTransform titleBar;
    [SerializeField] private Button closeButton;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Vector2 dragOffset;

    public event Action OnClose;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (closeButton != null)
            closeButton.onClick.AddListener(() => OnClose?.Invoke());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDragOnTitleBar(eventData)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            parentCanvas != null ? parentCanvas.worldCamera : null,
            out dragOffset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragOnTitleBar(eventData) && dragOffset == Vector2.zero) return;

        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            cam,
            out Vector2 localPoint
        );

        rectTransform.localPosition = localPoint - dragOffset;
        ClampToScreen();
    }

    private bool IsDragOnTitleBar(PointerEventData eventData)
    {
        if (titleBar == null) return true;
        return RectTransformUtility.RectangleContainsScreenPoint(
            titleBar,
            eventData.pressPosition,
            parentCanvas != null ? parentCanvas.worldCamera : null
        );
    }

    private void ClampToScreen()
    {
        if (parentCanvas == null) return;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector3[] canvasCorners = new Vector3[4];
        Vector3[] windowCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        rectTransform.GetWorldCorners(windowCorners);

        Vector3 pos = rectTransform.position;
        float minX = canvasCorners[0].x - windowCorners[0].x + pos.x;
        float maxX = canvasCorners[2].x - windowCorners[2].x + pos.x;
        float minY = canvasCorners[0].y - windowCorners[0].y + pos.y;
        float maxY = canvasCorners[2].y - windowCorners[2].y + pos.y;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        rectTransform.position = pos;
    }
}
