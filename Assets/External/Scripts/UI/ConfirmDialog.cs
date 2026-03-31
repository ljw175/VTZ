using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 범용 확인/취소 팝업 다이얼로그.
/// ConfirmDialog.Show("메시지", onYes, onNo) 로 호출한다.
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    public static ConfirmDialog Instance { get; private set; }

    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onYesCallback;
    private Action onNoCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (dialogPanel != null) dialogPanel.SetActive(false);

        yesButton?.onClick.AddListener(OnYesClicked);
        noButton?.onClick.AddListener(OnNoClicked);
    }

    /// <summary>
    /// 확인 팝업을 표시한다.
    /// </summary>
    public void Show(string message, Action onYes, Action onNo = null)
    {
        if (messageText != null) messageText.text = message;
        onYesCallback = onYes;
        onNoCallback = onNo;

        if (dialogPanel != null) dialogPanel.SetActive(true);
    }

    private void OnYesClicked()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        onYesCallback?.Invoke();
        ClearCallbacks();
    }

    private void OnNoClicked()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        onNoCallback?.Invoke();
        ClearCallbacks();
    }

    private void ClearCallbacks()
    {
        onYesCallback = null;
        onNoCallback = null;
    }
}
