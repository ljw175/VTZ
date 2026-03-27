using UnityEngine;

public class SpinSteeringUI : MonoBehaviour
{
    [Header("Steering Settings")]
    [Tooltip("회전시킬 조타수(Steering) 이미지의 Transform")]
    [SerializeField] private Transform steeringImage;
    [Tooltip("추적할 플레이어 함선의 Transform")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("회전 속도")]
    [SerializeField] private float spinSpeed = 200f;

    private float previousShipAngle;

    private void Start()
    {
        if (playerTransform != null)
        {
            previousShipAngle = playerTransform.eulerAngles.z;
        }
    }

    void Update()
    {
        if (steeringImage == null || playerTransform == null) return;

        // 함선의 현재 Z축 각도를 가져옵니다.
        float currentShipAngle = playerTransform.eulerAngles.z;
        
        // 이전 프레임과 비교하여 회전량과 방향을 계산합니다. (-180 ~ 180도 범위로 보정)
        float deltaAngle = Mathf.DeltaAngle(previousShipAngle, currentShipAngle);

        // 회전이 감지되었을 때만 스티어링 휠을 회전시킵니다. (오차 범위 0.01도)
        if (Mathf.Abs(deltaAngle) > 0.01f)
        {
            // 회전하는 방향으로 스티어링 이미지를 설정한 속도(spinSpeed)만큼 돌립니다.
            float spinDirection = Mathf.Sign(deltaAngle);
            steeringImage.Rotate(0f, 0f, spinDirection * spinSpeed * Time.deltaTime);
        }

        // 다음 프레임 계산을 위해 현재 각도를 저장합니다.
        previousShipAngle = currentShipAngle;
    }
}
