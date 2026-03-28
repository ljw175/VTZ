using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipStatusUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject statusPanel;

    [Header("Container Buttons")]
    [SerializeField] private Button openCargoButton;
    [SerializeField] private Button openBackpackButton;

    [Header("Stat Display")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI hullHpText;
    [SerializeField] private TextMeshProUGUI cargoCapacityText;
    [SerializeField] private TextMeshProUGUI cannonDamageText;
    [SerializeField] private TextMeshProUGUI cannonCooldownText;
    [SerializeField] private TextMeshProUGUI fuelEfficiencyText;

    private ShipRuntimeState shipState;
    private bool isOpen;

    private void Start()
    {
        if (statusPanel != null)
            statusPanel.SetActive(false);

        var shipController = FindObjectOfType<ShipController>();
        if (shipController != null)
            shipState = shipController.RuntimeState;

        if (openCargoButton != null)
        {
            openCargoButton.onClick.AddListener(() =>
            {
                if (InventoryPopupManager.Instance != null && InventoryManager.Instance != null)
                {
                    var cargo = InventoryManager.Instance.GetShipCargo();
                    if (cargo != null)
                        InventoryPopupManager.Instance.TogglePopup(cargo, "화물칸");
                }
            });
        }

        if (openBackpackButton != null)
        {
            openBackpackButton.onClick.AddListener(() =>
            {
                if (InventoryPopupManager.Instance != null && InventoryManager.Instance != null)
                {
                    var backpack = InventoryManager.Instance.GetPlayerBackpack();
                    if (backpack != null)
                        InventoryPopupManager.Instance.TogglePopup(backpack, "배낭");
                }
            });
        }
    }

    private void Update()
    {
        if (InputManager.Instance.InventoryToggle.WasPressedThisFrame())
            ToggleStatus();
    }

    public void ToggleStatus()
    {
        isOpen = !isOpen;

        if (statusPanel != null)
            statusPanel.SetActive(isOpen);

        if (isOpen)
            RefreshStats();
    }

    private void RefreshStats()
    {
        if (shipState == null) return;

        if (speedText != null)
            speedText.text = $"속도: {shipState.GetStat(ShipStatType.Speed):F1}";

        if (hullHpText != null)
            hullHpText.text = $"선체: {shipState.CurrentHp} / {shipState.MaxHp}";

        if (cargoCapacityText != null)
            cargoCapacityText.text = $"화물 용량: {shipState.GetStat(ShipStatType.CargoCapacity):F1}";

        if (cannonDamageText != null)
            cannonDamageText.text = $"포격력: {shipState.GetStat(ShipStatType.CannonDamage):F0}";

        if (cannonCooldownText != null)
            cannonCooldownText.text = $"재장전: {shipState.GetStat(ShipStatType.CannonCooldown):F1}s";

        if (fuelEfficiencyText != null)
            fuelEfficiencyText.text = $"연비: {shipState.GetStat(ShipStatType.FuelEfficiency):F1}";
    }
}
