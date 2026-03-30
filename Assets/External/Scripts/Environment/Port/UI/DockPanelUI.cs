using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DockPanelUI : MonoBehaviour
{
    [Header("Dock Buttons")]
    [SerializeField] private Button inspectionButton;
    [SerializeField] private Button junkmanButton;
    [SerializeField] private Button tavernButton;
    [SerializeField] private Button fishMarketButton;
    [SerializeField] private Button shipyardButton;
    [SerializeField] private Button tradePostButton;

    [Header("References")]
    [SerializeField] private CargoInspectionHandler inspectionHandler;
    [SerializeField] private JunkmanHandler junkmanHandler;
    [SerializeField] private PortUIController portUIController;

    private PortDefinition currentDefinition;
    private bool inspectionCompleted;
    private bool hasInspection;

    public void Setup(PortDefinition definition)
    {
        currentDefinition = definition;
        inspectionCompleted = false;

        var facilities = definition.availableFacilities;
        hasInspection = definition.dockConfig != null && definition.dockConfig.hasCargoInspection;

        // 시설 이동 버튼 표시 여부
        SetButtonVisible(tavernButton, facilities.Contains(PortFacilityType.Tavern));
        SetButtonVisible(fishMarketButton, facilities.Contains(PortFacilityType.FishMarket));
        SetButtonVisible(shipyardButton, facilities.Contains(PortFacilityType.Shipyard));
        SetButtonVisible(tradePostButton, facilities.Contains(PortFacilityType.TradePost));

        // 검문 버튼
        SetButtonVisible(inspectionButton, hasInspection);

        // 고물상 버튼은 항상 표시 (Dock이 있으면)
        SetButtonVisible(junkmanButton, true);

        // 검문이 있으면 검문 완료 전까지 다른 버튼 비활성화
        if (hasInspection)
        {
            SetNonInspectionButtonsInteractable(false);
            if (GameTimer.Instance != null)
                GameTimer.Instance.PauseTimer();
        }
        else
        {
            SetNonInspectionButtonsInteractable(true);
        }

        BindButtons();
    }

    private void BindButtons()
    {
        BindButton(inspectionButton, OnInspectionClicked);
        BindButton(junkmanButton, OnJunkmanClicked);
        BindButton(tavernButton, () => portUIController?.NavigateToFacility(PortFacilityType.Tavern));
        BindButton(fishMarketButton, () => portUIController?.NavigateToFacility(PortFacilityType.FishMarket));
        BindButton(shipyardButton, () => portUIController?.NavigateToFacility(PortFacilityType.Shipyard));
        BindButton(tradePostButton, () => portUIController?.NavigateToFacility(PortFacilityType.TradePost));
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void OnInspectionClicked()
    {
        if (inspectionHandler == null || currentDefinition?.dockConfig == null) return;

        inspectionHandler.Execute(currentDefinition.dockConfig, OnInspectionComplete);
    }

    private void OnInspectionComplete()
    {
        inspectionCompleted = true;

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResumeTimer();

        SetNonInspectionButtonsInteractable(true);
        if (inspectionButton != null)
            inspectionButton.interactable = false;
    }

    private void OnJunkmanClicked()
    {
        if (junkmanHandler != null)
            junkmanHandler.Open();
    }

    private void SetNonInspectionButtonsInteractable(bool interactable)
    {
        if (junkmanButton != null) junkmanButton.interactable = interactable;
        if (tavernButton != null) tavernButton.interactable = interactable;
        if (fishMarketButton != null) fishMarketButton.interactable = interactable;
        if (shipyardButton != null) shipyardButton.interactable = interactable;
        if (tradePostButton != null) tradePostButton.interactable = interactable;

        portUIController?.SetFacilityButtonsInteractable(interactable);
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void OnDisable()
    {
        // 패널 닫힐 때 검문 중이었다면 타이머 복구
        if (hasInspection && !inspectionCompleted)
        {
            if (GameTimer.Instance != null)
                GameTimer.Instance.ResumeTimer();
        }
    }
}
