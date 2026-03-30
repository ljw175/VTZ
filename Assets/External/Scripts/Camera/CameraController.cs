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
    [SerializeField] private float planningOrthoSize = 300f;
    [Tooltip("실행 페이즈(항해) 시의 기본 카메라 크기")]
    [SerializeField] private float executionOrthoSize = 10f;
    [Tooltip("줌 전환 속도 (낮을수록 빠름)")]
    [SerializeField] private float zoomSmoothTime = 2f;

    [Header("Scroll Zoom Settings (RealTime Phase)")]
    [Tooltip("마우스 휠 줌 인 최소 크기")]
    [SerializeField] private float minExecutionZoom = 10f;
    [Tooltip("마우스 휠 줌 아웃 최대 크기")]
    [SerializeField] private float maxExecutionZoom = 30f;
    [Tooltip("스크롤 한 칸 당 줌 변화량")]
    [SerializeField] private float scrollZoomSensitivity = 2f;

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
        if (mapBounds != null)
        {
            // 맵 경계 계산 (Box Collider의 중심과 크기를 기반으로)
            minBound = mapBounds.bounds.min;
            maxBound = mapBounds.bounds.max;
        }
        else
        {
            Debug.LogWarning("[CameraController] Map Bounds가 설정되지 않았습니다. 카메라 이동 제한이 제대로 작동하지 않을 수 있습니다.");
        }
    }

    private void Update()
    {
        // 항해(RealTime) 페이즈에서만 마우스 휠 스크롤을 통한 줌 조절 허용
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            HandleScrollZoom();
        }
    }

    private void HandleScrollZoom()
    {
        // UI 위에 마우스가 있을 경우 줌 조절을 방지하는 것이 좋습니다.
        if (InputManager.Instance != null && InputManager.Instance.IsPointerOverUI()) return;

        // New Input System을 통한 마우스 스크롤 값 읽기
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                // 스크롤을 위로 올리면(양수) 줌 인(사이즈 감소), 아래로 내리면(음수) 줌 아웃(사이즈 증가)
                float direction = Mathf.Sign(scrollY);
                executionOrthoSize -= direction * scrollZoomSensitivity;
                
                // 설정된 범위 내로 클램핑
                executionOrthoSize = Mathf.Clamp(executionOrthoSize, minExecutionZoom, maxExecutionZoom);
            }
        }
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
        Vector3 targetPos = playerTarget.position + offset;

        // 카메라 뷰포트 크기에 따른 보정값 계산
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect; // Screen.width / Screen.height 보다 Camera.aspect가 더 정확합니다.

        // 항해 모드라면 카메라 위치를 맵 경계 내로 제한
        if (GameManager.Instance.CurrentPhase == GamePhase.RealTime && mapBounds != null)
        {
            // 월드맵 크기보다 카메라 시야가 더 커지는 역전 현상 방어 로직이 필요합니다.
            // 만약 맵이 시야보다 작다면, 중심에 카메라를 고정시킵니다.
            float clampX = Mathf.Max(minBound.x + halfWidth, Mathf.Min(targetPos.x, maxBound.x - halfWidth));
            float clampY = Mathf.Max(minBound.y + halfHeight, Mathf.Min(targetPos.y, maxBound.y - halfHeight));

            targetPos = new Vector3(clampX, clampY, targetPos.z);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPos, 
            ref moveVelocity, 
            moveSmoothTime
        );
    }
}