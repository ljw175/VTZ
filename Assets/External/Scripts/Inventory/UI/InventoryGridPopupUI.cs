using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryGridPopupUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private DraggableWindow window;
    [SerializeField] private InventoryGridUI gridUI;
    [SerializeField] private Button autoSortButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI weightText;

    [Header("Layout")]
    [SerializeField] private float titleBarHeight = 32f;
    [SerializeField] private float footerHeight = 32f;
    [SerializeField] private float padding = 8f;

    public IInventoryContainer Container { get; private set; }
    public InventoryGridUI GridUI => gridUI;

    public void Setup(IInventoryContainer container)
    {
        Container = container;

        if (titleText != null)
            titleText.text = container.DisplayName;
        else
            Debug.LogWarning($"[GridPopup] titleText 참조가 null — 프리팹에서 TitleText가 연결되지 않음");

        if (gridUI != null)
            gridUI.Initialize(container);

        if (autoSortButton != null)
            autoSortButton.onClick.AddListener(OnAutoSort);

        container.OnContainerChanged += UpdateWeightDisplay;
        ResizeToFitGrid();
        UpdateWeightDisplay();
    }

    private void OnDestroy()
    {
        if (Container != null)
            Container.OnContainerChanged -= UpdateWeightDisplay;
    }

    private void ResizeToFitGrid()
    {
        if (gridUI == null || Container == null) return;

        float cellTotal = gridUI.CellSize + gridUI.CellSpacing;
        float gridWidth = Container.GridState.Width * cellTotal;
        float gridHeight = Container.GridState.Height * cellTotal;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(
            gridWidth + padding * 2,
            gridHeight + titleBarHeight + footerHeight + padding * 2
        );
    }

    private void UpdateWeightDisplay()
    {
        if (weightText == null || Container == null) return;

        float current = Container.GetTotalWeight();
        float max = Container.GetWeightCapacity();

        weightText.text = max < 0
            ? $"{current:F1} kg"
            : $"{current:F1} / {max:F1} kg";
    }

    private void OnAutoSort()
    {
        if (Container == null) return;

        var overflow = Container.AutoSort();
        if (overflow.Count > 0)
            Debug.LogWarning($"[Inventory] Auto-sort overflow: {overflow.Count} items");
    }
}
