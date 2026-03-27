using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelGaugeUI : MonoBehaviour 
{
    [Header("Shader / Material")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject fuelValue;

    [Header("UI Text")]
    [SerializeField] private TMP_Text fuelValueText;

    private Slider fuelSlider;
    private bool wasPressing;

    private void Awake()
    {
        fuelSlider = GetComponent<Slider>();

        // GameManager에서 Percentage(0.0 ~ 1.0)를 가져올 것이므로 Slider의 범위를 1~0로 설정
        fuelSlider.minValue = 0f;
        fuelSlider.maxValue = 1f;
        
        // Image 및 Material 자동 할당 처리
        if (fillImage == null && fuelSlider.fillRect != null)
        {
            fillImage = fuelSlider.fillRect.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

    float currentGaugePercent = GameManager.Instance.GetGaugePercentage();
    
        // 기존의 1 - currentGaugePercent 방식을 버리고 직관적으로 매핑
        fuelSlider.value = currentGaugePercent;
        UpdateFuelText(currentGaugePercent);
    }

    private void UpdateFuelText(float percent)
    {
        if (fuelValueText == null) return;

        // 0.0 ~ 1.0 비율을 0 ~ 100 퍼센트로 변환하여 출력
        int displayValue = Mathf.RoundToInt(percent * 100f);
        fuelValueText.text = $"{displayValue}%";
    }
}