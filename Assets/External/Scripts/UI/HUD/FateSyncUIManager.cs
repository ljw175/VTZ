using UnityEngine;
using UnityEngine.UI;

public class FateSyncUIManager : MonoBehaviour
{
    [SerializeField] private Slider fateSyncSlider;
    [SerializeField] private Image fillImage;

    [Header("Colors (HDR 추천)")]
    [SerializeField] [ColorUsage(true, true)] private Color highSyncGlowColor = new Color(0f, 1f, 1f, 2f); // 81~100%
    [SerializeField] [ColorUsage(true, true)] private Color normalColor = new Color(0f, 1f, 1f, 1f);       // 51~80%
    [SerializeField] [ColorUsage(true, true)] private Color dangerColor = new Color(1f, 0f, 0f, 1f);       // 21~50%
    [SerializeField] [ColorUsage(true, true)] private Color criticalGlowColor = new Color(1f, 0f, 0f, 2f); // 0~20%

    private void Update()
    {
        if (GameManager.Instance == null || fateSyncSlider == null || fillImage == null) return;

        float syncPercentage = GameManager.Instance.GetFateSyncPercentage();
        fateSyncSlider.value = syncPercentage;

        Color targetColor = normalColor;

        if (syncPercentage > 0.8f)
        {
            // 81% ~ 100% : 높아질수록 빛남
            float t = Mathf.InverseLerp(0.8f, 1.0f, syncPercentage);
            targetColor = Color.Lerp(normalColor, highSyncGlowColor, t);
        }
        else if (syncPercentage > 0.5f)
        {
            // 51% ~ 80% : 서서히 빛나는 효과 사라짐 (기본 normalColor 유지)
            targetColor = normalColor;
        }
        else if (syncPercentage > 0.2f)
        {
            // 21% ~ 50% : 낮아질수록 붉은색
            float t = Mathf.InverseLerp(0.5f, 0.2f, syncPercentage);
            targetColor = Color.Lerp(normalColor, dangerColor, t);
        }
        else
        {
            // 0% ~ 20% : 낮아질수록 붉게 빛남
            float t = Mathf.InverseLerp(0.2f, 0.0f, syncPercentage);
            targetColor = Color.Lerp(dangerColor, criticalGlowColor, t);
        }

        fillImage.color = targetColor;
    }
}