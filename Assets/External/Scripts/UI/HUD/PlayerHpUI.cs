using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어의 체력을 UI(Slider 및 Text)에 중계하는 클래스입니다.
/// </summary>
public class PlayerHpUI : MonoBehaviour
{
    [Tooltip("플레이어의 체력을 표시할 슬라이더")]
    [SerializeField] private Slider hpSlider;
    
    [Tooltip("체력을 텍스트로 표시할 경우 할당 (예: 10/10)")]
    [SerializeField] private TextMeshProUGUI hpText;

    private ShipController playerShip;

    private void Start()
    {
        // 씬 내의 플레이어 함선을 찾습니다.
        playerShip = Object.FindFirstObjectByType<ShipController>();

        if (playerShip != null)
        {
            // 이벤트 구독: 체력이 변할 때마다 UI 업데이트 함수 호출
            playerShip.OnHpChanged += UpdateHpUI;
            
            // 초기 체력 세팅
            UpdateHpUI(playerShip.CurrentHp, 10); // 기본 maxHp가 10이므로 임시 할당, 실제로는 이벤트가 덮어씌움
        }
        else
        {
            Debug.LogWarning("[PlayerHpUI] 플레이어 함선(ShipController)을 찾을 수 없습니다.");
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 구독 해제
        if (playerShip != null)
        {
            playerShip.OnHpChanged -= UpdateHpUI;
        }
    }

    /// <summary>
    /// 체력 변경 이벤트를 수신하여 UI를 갱신합니다.
    /// </summary>
    private void UpdateHpUI(int currentHp, int maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (hpText != null)
        {
            hpText.text = $"{currentHp} / {maxHp}";
        }

        // TODO: (서사적 피드백) 체력이 30% 이하일 때 슬라이더가 붉게 점멸하게 만드는 등의 연출을 이곳에 추가할 수 있습니다.
    }
}