using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 2D UI 프리팹을 마우스 커서로 활용하기 위한 컴포넌트입니다.
/// 반드시 최상단 Canvas 하위에 배치되어야 하며, Image의 Raycast Target은 꺼져 있어야 합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DynamicUICursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("커서가 속한 캔버스를 연결하세요. (필수)")]
    public Canvas parentCanvas;

    [Tooltip("커서 이미지의 오프셋 (마우스 끝점과 이미지 중심을 맞추기 위함)")]
    public Vector2 hotspotOffset = Vector2.zero;

    private RectTransform cursorRectTransform;

    private void Awake()
    {
        cursorRectTransform = GetComponent<RectTransform>();
        
        // 치명적 오류 방지: 자신과 자식들의 Raycast Target 강제 해제
        Graphic[] graphics = GetComponentsInChildren<Graphic>();
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        if (parentCanvas == null)
        {
            Debug.LogError("[DynamicUICursor] Parent Canvas가 할당되지 않았습니다! 정상 작동하지 않습니다.");
        }
    }

    // 커서 좌표 갱신은 다른 렌더링 파이프라인이 모두 끝난 후(LateUpdate)에 수행해야 가장 부드럽습니다.
    private void LateUpdate()
    {
        if (parentCanvas == null) return;

        Vector2 localCursorPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition,
            parentCanvas.worldCamera,
            out localCursorPoint
        );

        cursorRectTransform.localPosition = localCursorPoint + hotspotOffset;
    }
}