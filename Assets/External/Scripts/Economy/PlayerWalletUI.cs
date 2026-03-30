using UnityEngine;
using TMPro;

public class PlayerWalletUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void OnEnable()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnGoldChanged += UpdateDisplay;
            UpdateDisplay(PlayerWallet.Instance.CurrentGold);
        }
    }

    private void OnDisable()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnGoldChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int gold)
    {
        if (goldText != null)
            goldText.text = $"{gold} G";
    }
}
