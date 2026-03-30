using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class PortUIController : MonoBehaviour
{
    [Header("Port Info")]
    [SerializeField] private TextMeshProUGUI portNameText;
    [SerializeField] private TextMeshProUGUI portDescriptionText;

    [Header("Facility Buttons")]
    [SerializeField] private GameObject facilityButtonPrefab;
    [SerializeField] private Transform buttonContainer;

    [Header("Facility Panels")]
    [SerializeField] private DockPanelUI dockPanel;
    [SerializeField] private GameObject tavernPanel;
    [SerializeField] private GameObject fishMarketPanel;
    [SerializeField] private GameObject shipyardPanel;
    [SerializeField] private GameObject tradePostPanel;

    private List<PortFacilityButtonUI> spawnedButtons = new List<PortFacilityButtonUI>();
    private PortDefinition currentDefinition;

    private void OnEnable()
    {
        if (PortManager.Instance != null)
            PortManager.Instance.OnPortOpened += OnPortOpened;
    }

    private void OnDisable()
    {
        if (PortManager.Instance != null)
            PortManager.Instance.OnPortOpened -= OnPortOpened;
    }

    private void OnPortOpened(PortDefinition definition)
    {
        currentDefinition = definition;

        if (portNameText != null)
            portNameText.text = definition.portName;
        if (portDescriptionText != null)
            portDescriptionText.text = definition.description;

        BuildFacilityButtons(definition);
        HideAllPanels();

        // Dock이 있으면 자동 선택
        if (definition.availableFacilities.Contains(PortFacilityType.Dock))
            ShowFacility(PortFacilityType.Dock);
    }

    public void OnDepartButtonClicked()
    {
        PortManager.Instance?.ClosePort();
    }

    private void BuildFacilityButtons(PortDefinition definition)
    {
        // 기존 버튼 제거
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();

        if (facilityButtonPrefab == null || buttonContainer == null) return;

        foreach (var facilityType in definition.availableFacilities)
        {
            // Dock은 별도 패널로 관리되므로 시설 목록 버튼에는 표시하지 않음
            if (facilityType == PortFacilityType.Dock) continue;

            GameObject obj = Instantiate(facilityButtonPrefab, buttonContainer);
            var buttonUI = obj.GetComponent<PortFacilityButtonUI>();
            if (buttonUI == null)
            {
                Destroy(obj);
                continue;
            }

            var type = facilityType;
            buttonUI.Setup(type, () => ShowFacility(type));
            spawnedButtons.Add(buttonUI);
        }
    }

    public void ShowFacility(PortFacilityType type)
    {
        HideAllPanels();

        switch (type)
        {
            case PortFacilityType.Dock:
                if (dockPanel != null)
                {
                    dockPanel.gameObject.SetActive(true);
                    dockPanel.Setup(currentDefinition);
                }
                break;
            case PortFacilityType.Tavern:
                if (tavernPanel != null) tavernPanel.SetActive(true);
                break;
            case PortFacilityType.FishMarket:
                if (fishMarketPanel != null) fishMarketPanel.SetActive(true);
                break;
            case PortFacilityType.Shipyard:
                if (shipyardPanel != null) shipyardPanel.SetActive(true);
                break;
            case PortFacilityType.TradePost:
                if (tradePostPanel != null) tradePostPanel.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// DockPanel에서 시설 이동 버튼이 눌렸을 때 호출
    /// </summary>
    public void NavigateToFacility(PortFacilityType type)
    {
        ShowFacility(type);
    }

    /// <summary>
    /// 하위 시설 패널에서 Dock으로 돌아갈 때 호출
    /// </summary>
    public void ReturnToDock()
    {
        ShowFacility(PortFacilityType.Dock);
    }

    private void HideAllPanels()
    {
        if (dockPanel != null) dockPanel.gameObject.SetActive(false);
        if (tavernPanel != null) tavernPanel.SetActive(false);
        if (fishMarketPanel != null) fishMarketPanel.SetActive(false);
        if (shipyardPanel != null) shipyardPanel.SetActive(false);
        if (tradePostPanel != null) tradePostPanel.SetActive(false);
    }

    /// <summary>
    /// 시설 버튼의 활성/비활성 제어 (검문 완료 전후)
    /// </summary>
    public void SetFacilityButtonsInteractable(bool interactable)
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                btn.SetInteractable(interactable);
        }
    }
}
