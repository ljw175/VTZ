using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventoryTooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI freshnessText;
    [SerializeField] private Slider freshnessBar;
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI effectsText;

    private bool _isHovering;
    private bool _isTooltipVisible;

    [Header("Freshness Colors")]
    [SerializeField] private Color freshColor = Color.green;
    [SerializeField] private Color okayColor = Color.yellow;
    [SerializeField] private Color agingColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color rottenColor = Color.red;

    private void Awake()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 호버 중 우클릭 → 토글
        if (eventData.button != PointerEventData.InputButton.Right) return;

        if (_isTooltipVisible)
        {
            HideTooltip();
        }
        else
        {
            var itemUI = GetComponent<InventoryItemUI>();
            if (itemUI == null || itemUI.ItemInstance == null) return;
            ShowTooltip(itemUI.ItemInstance);
        }
    }

    private void Update()
    {
        // 호버 밖에서 좌클릭 또는 우클릭 → 닫기
        if (!_isHovering && _isTooltipVisible)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                HideTooltip();
        }
    }

    public void ShowTooltip(ItemInstance item)
    {
        if (tooltipPanel == null || item == null || item.Definition == null) return;

        var def = item.Definition;
        tooltipPanel.SetActive(true);
        _isTooltipVisible = true;

        if (nameText != null) nameText.text = "이름: " + def.itemName;
        if (descriptionText != null) descriptionText.text = def.description;
        if (categoryText != null) categoryText.text = FormatCategories(def.categories);
        if (weightText != null) weightText.text = $"무게: {def.weight:F1} kg";
        if (valueText != null) valueText.text = $"가격: {def.baseValue} G";

        // 신선도
        if (def.IsPerishable)
        {
            FreshnessState state = item.GetFreshnessState();
            float ratio = item.CurrentFreshness / def.maxFreshness;

            if (freshnessText != null)
            {
                string stateLabel = state switch
                {
                    FreshnessState.Fresh => "Fresh",
                    FreshnessState.Okay => "OK",
                    FreshnessState.Aging => "Aging",
                    FreshnessState.Rotten => "Rotten",
                    _ => ""
                };
                freshnessText.text = "신선도: " + stateLabel;
                freshnessText.color = GetFreshnessColor(state);
            }

            if (freshnessBar != null)
            {
                freshnessBar.gameObject.SetActive(true);
                freshnessBar.value = Mathf.Clamp01(ratio);
                var fill = freshnessBar.fillRect?.GetComponent<Image>();
                if (fill != null) fill.color = GetFreshnessColor(state);
            }
        }
        else
        {
            if (freshnessText != null) freshnessText.text = "신선도: 없음";
            if (freshnessBar != null) freshnessBar.gameObject.SetActive(false);
        }

        // 효과
        if (effectsText != null)
        {
            effectsText.text = def.effects != null && def.effects.Length > 0
                ? string.Join("\n", def.effects
                    .Where(e => e != null && !string.IsNullOrEmpty(e.effectDescription))
                    .Select(e => e.effectDescription))
                : "";
        }

        // 툴팁 위치를 마우스 근처로
        PositionTooltip();
    }

    public void HideTooltip()
    {
        _isTooltipVisible = false;
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void PositionTooltip()
    {
        if (tooltipPanel == null) return;

        RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
        if (rt == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            (Vector3)InputManager.Instance.MousePos + new Vector3(16f, -16f, 0f),
            cam,
            out Vector2 localPos
        );

        rt.localPosition = localPos;
    }

    private Color GetFreshnessColor(FreshnessState state)
    {
        return state switch
        {
            FreshnessState.Fresh => freshColor,
            FreshnessState.Okay => okayColor,
            FreshnessState.Aging => agingColor,
            FreshnessState.Rotten => rottenColor,
            _ => Color.white,
        };
    }

    private string FormatCategories(ItemCategory categories)
    {
        if (categories == ItemCategory.None) return "";

        return string.Join(", ",
            System.Enum.GetValues(typeof(ItemCategory))
                .Cast<ItemCategory>()
                .Where(cat => cat != ItemCategory.None && (categories & cat) != 0));
    }
}
