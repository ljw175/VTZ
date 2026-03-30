using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PortFacilityButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image iconImage;

    public PortFacilityType FacilityType { get; private set; }

    public void Setup(PortFacilityType type, Action onClick)
    {
        FacilityType = type;

        if (labelText != null)
            labelText.text = GetFacilityDisplayName(type);

        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private string GetFacilityDisplayName(PortFacilityType type)
    {
        return type switch
        {
            PortFacilityType.Dock => "부두",
            PortFacilityType.FishMarket => "어시장",
            PortFacilityType.Shipyard => "조선소",
            PortFacilityType.Tavern => "술집",
            PortFacilityType.TradePost => "교역소",
            _ => type.ToString(),
        };
    }
}
