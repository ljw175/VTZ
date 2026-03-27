using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("추적할 함선의 Transform")]
    [SerializeField] private Transform playerTarget;

    [Header("Map Bounds Settings")]
    [Tooltip("맵의 경계를 정의하는 Box Collider")]
    [SerializeField] private BoxCollider2D mapBounds;
    private Vector3 minBound;
    private Vector3 maxBound;
    private float halfWidth;
    private float halfHeight;

    [Header("Zoom Settings")]
    [Tooltip("계획 페이즈(해도 그리기) 시의 카메라 크기 (Zoom-Out)")]
    [SerializeField] private float planningOrthoSize = 15f;
    [Tooltip("실행 페이즈(항해) 시의 카메라 크기 (Zoom-In)")]
    [SerializeField] private float executionOrthoSize = 7f;
    [Tooltip("줌 전환 속도 (낮을수록 빠름)")]
    [SerializeField] private float zoomSmoothTime = 0.3f;

    [Header("Follow Settings")]
    [Tooltip("타겟과의 오프셋 (Z축 거리 유지)")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    [Tooltip("카메라 추적 속도 (낮을수록 빠름)")]
    [SerializeField] private float moveSmoothTime = 0.2f;

    private Camera cam;
    
    // SmoothDamp 연산을 위한 내부 속도 캐싱 변수
    private Vector3 moveVelocity;
    private float zoomVelocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        // 타겟이 비어있다면 자동 할당 시도
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    private void Start()
    {
        // 맵 경계 계산 (Box Collider의 중심과 크기를 기반으로)
        minBound = mapBounds.bounds.min;
        maxBound = mapBounds.bounds.max;
    }

    private void LateUpdate()
    {
        if (playerTarget == null || GameManager.Instance == null) return;

        // 1. 상태에 따른 목표 줌(Zoom) 설정
        bool isPlanning = GameManager.Instance.CurrentPhase == GamePhase.Paused;
        float targetSize = isPlanning ? planningOrthoSize : executionOrthoSize;

        // 2. 줌(Orthographic Size) 부드러운 보간
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, 
            targetSize, 
            ref zoomVelocity, 
            zoomSmoothTime
        );

        // 3. 위치(Position) 부드러운 추적
        // 카메라가 맵 밖으로 나가지 않도록 보정 
        // 플레이어 중심으로 줌아웃 되지만 월드맵 경계를 벗어나지 않도록 구현합니다.
        Vector3 targetPos = playerTarget.position + offset;

        // 카메라 뷰포트 크기에 따른 보정값 계산
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * Screen.width / Screen.height; // 가로 절반 크기

        // 항해 모드라면 카메라 위치를 맵 경계 내로 제한
        if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minBound.x + halfWidth, maxBound.x - halfWidth);
            targetPos.y = Mathf.Clamp(targetPos.y, minBound.y + halfHeight, maxBound.y - halfHeight);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPos, 
            ref moveVelocity, 
            moveSmoothTime
        );
    }
}