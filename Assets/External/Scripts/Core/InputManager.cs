using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System 기반 입력 관리 싱글톤.
/// GameInputActions.inputactions 에셋을 로드하여 모든 입력 액션을 제공한다.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap playerMap;

    // --- Mouse ---
    public InputAction MousePosition { get; private set; }
    public InputAction Click { get; private set; }
    public InputAction RightClick { get; private set; }

    // --- Ship ---
    public InputAction Accelerate { get; private set; }
    public InputAction Decelerate { get; private set; }
    public InputAction Steer { get; private set; }
    public InputAction SteerMode { get; private set; }

    // --- Game ---
    public InputAction Pause { get; private set; }
    public InputAction StartPhase { get; private set; }
    public InputAction Restart { get; private set; }
    public InputAction Continue { get; private set; }

    // --- Inventory ---
    public InputAction InventoryToggle { get; private set; }
    public InputAction InventoryRotate { get; private set; }
    public InputAction InventoryCancel { get; private set; }

    // --- Interaction ---
    public InputAction Interact { get; private set; }

    // --- Container Shortcuts ---
    public InputAction CargoToggle { get; private set; }
    public InputAction BackpackToggle { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playerMap = inputActions.FindActionMap("Player");

        MousePosition = playerMap.FindAction("MousePosition");
        Click = playerMap.FindAction("Click");
        RightClick = playerMap.FindAction("RightClick");

        Accelerate = playerMap.FindAction("Accelerate");
        Decelerate = playerMap.FindAction("Decelerate");
        Steer = playerMap.FindAction("Steer");
        SteerMode = playerMap.FindAction("SteerMode");

        Pause = playerMap.FindAction("Pause");
        StartPhase = playerMap.FindAction("StartPhase");
        Restart = playerMap.FindAction("Restart");
        Continue = playerMap.FindAction("Continue");

        InventoryToggle = playerMap.FindAction("InventoryToggle");
        InventoryRotate = playerMap.FindAction("InventoryRotate");
        InventoryCancel = playerMap.FindAction("InventoryCancel");

        Interact = playerMap.FindAction("Interact");

        CargoToggle = playerMap.FindAction("CargoToggle");
        BackpackToggle = playerMap.FindAction("BackpackToggle");

        playerMap.Enable();
    }

    private void OnDestroy()
    {
        playerMap?.Disable();
    }

    /// <summary>
    /// 마우스 커서 위치 (Screen Space).
    /// </summary>
    public Vector2 MousePos => MousePosition.ReadValue<Vector2>();

    /// <summary>
    /// 현재 마우스가 UI 위에 있는지 확인.
    /// New Input System의 InputSystemUIInputModule과 올바르게 연동.
    /// </summary>
    public bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
