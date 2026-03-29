using UnityEngine;
using UnityEngine.UI;

public class InventoryShortcutUI : MonoBehaviour
{
    [SerializeField] private Button cargoButton;
    [SerializeField] private Button backpackButton;

    private void Start()
    {
        if (cargoButton != null)
        {
            cargoButton.onClick.AddListener(() =>
            {
                if (InventoryPopupManager.Instance != null && InventoryManager.Instance != null)
                {
                    var cargo = InventoryManager.Instance.GetShipCargo();
                    if (cargo != null)
                        InventoryPopupManager.Instance.TogglePopup(cargo);
                }
            });
        }

        if (backpackButton != null)
        {
            backpackButton.onClick.AddListener(() =>
            {
                if (InventoryPopupManager.Instance != null && InventoryManager.Instance != null)
                {
                    var backpack = InventoryManager.Instance.GetPlayerBackpack();
                    if (backpack != null)
                        InventoryPopupManager.Instance.TogglePopup(backpack);
                }
            });
        }
    }
}
