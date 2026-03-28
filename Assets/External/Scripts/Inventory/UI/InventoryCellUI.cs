using UnityEngine;
using UnityEngine.UI;

public enum CellHighlightState
{
    None,
    ValidPlacement,
    InvalidPlacement,
    SwapCandidate,
    Occupied,
}

public class InventoryCellUI : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color validColor = new Color(0f, 0.8f, 0f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(0.8f, 0f, 0f, 0.5f);
    [SerializeField] private Color swapColor = new Color(0.8f, 0.8f, 0f, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);

    private Image image;
    private CellHighlightState currentState = CellHighlightState.None;

    public int GridX { get; private set; }
    public int GridY { get; private set; }

    public void Initialize(int gridX, int gridY)
    {
        GridX = gridX;
        GridY = gridY;

        image = GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();

        SetHighlight(CellHighlightState.None);
    }

    public void SetHighlight(CellHighlightState state)
    {
        currentState = state;
        if (image == null) return;

        image.color = state switch
        {
            CellHighlightState.ValidPlacement => validColor,
            CellHighlightState.InvalidPlacement => invalidColor,
            CellHighlightState.SwapCandidate => swapColor,
            CellHighlightState.Occupied => occupiedColor,
            _ => defaultColor,
        };
    }

    public void ResetHighlight()
    {
        SetHighlight(CellHighlightState.None);
    }
}
