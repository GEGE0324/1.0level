using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player_Walk : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset InputActions;
    private InputAction m_moveAction;
    private InputAction m_lookAction;
    private InputAction m_jumpAction;

    [Header("Movement Settings")]
    public float WalkSpeed = 7f;
    public float JumpForce = 5f;
    [Range(0.01f, 1f)]
    public float RotationSmoothTime = 0.1f;

    [Header("Look Settings")]
    public float MouseSensitivityX = 15f;
    public float MouseSensitivityY = 15f;
    public float GamepadSensitivityX = 150f;
    public float GamepadSensitivityY = 150f;
    public float MinPitch = -30f;
    public float MaxPitch = 60f;

    [Header("References")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform cameraRoot;

    private Rigidbody m_rigidbody;
    private Animator m_animator;
    private Vector2 m_moveInput;
    private Vector2 m_lookInput;
    private float m_cameraPitch;
    private float m_cameraYaw;
    private float m_visualTurnVelocity;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_animator = GetComponent<Animator>();

        m_moveAction = InputActions.FindAction("Move");
        m_lookAction = InputActions.FindAction("Look");
        m_jumpAction = InputActions.FindAction("Jump");

        m_rigidbody.freezeRotation = true;
        // 核心：Rigidbody 插值是解决位移重影的唯一物理手段
        m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        Cursor.lockState = CursorLockMode.Locked;

        Vector3 angles = cameraRoot.eulerAngles;
        m_cameraYaw = angles.y;
        m_cameraPitch = angles.x;
    }

    private void OnEnable() => InputActions.FindActionMap("Player").Enable();
    private void OnDisable() => InputActions.FindActionMap("Player").Disable();

    private void Update()
    {
        m_moveInput = m_moveAction.ReadValue<Vector2>();
        m_lookInput = m_lookAction.ReadValue<Vector2>();

        if (m_jumpAction.WasPressedThisFrame()) Jump();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void LateUpdate()
    {
        // 关键点：在 LateUpdate 中旋转 cameraRoot。
        // 因为 Cinemachine Brain 在 Smart Update 模式下会在 LateUpdate 渲染前执行。
        RotateCamera();
        RotateVisualModel();
    }

    private void ApplyMovement()
    {
        // 确保移动方向是绝对水平的
        Vector3 camForward = Vector3.ProjectOnPlane(cameraRoot.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraRoot.right, Vector3.up).normalized;

        Vector3 moveDir = camForward * m_moveInput.y + camRight * m_moveInput.x;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        // 直接设置水平速度，保留重力影响
        m_rigidbody.linearVelocity = new Vector3(moveDir.x * WalkSpeed, m_rigidbody.linearVelocity.y, moveDir.z * WalkSpeed);

        if (m_animator != null) m_animator.SetFloat("Speed", m_moveInput.magnitude);
    }

    private void RotateCamera()
    {
        float currentSensX = MouseSensitivityX;
        float currentSensY = MouseSensitivityY;

        if (m_lookAction.activeControl != null && m_lookAction.activeControl.device is Gamepad)
        {
            currentSensX = GamepadSensitivityX;
            currentSensY = GamepadSensitivityY;
        }

        // 针对 LateUpdate 旋转，使用 unscaledDeltaTime 避免物理卡顿影响相机
        float dt = Time.unscaledDeltaTime;
        m_cameraYaw += m_lookInput.x * currentSensX * dt;
        m_cameraPitch -= m_lookInput.y * currentSensY * dt;
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, MinPitch, MaxPitch);

        cameraRoot.rotation = Quaternion.Euler(m_cameraPitch, m_cameraYaw, 0f);
    }

    private void RotateVisualModel()
    {
        if (m_moveInput.sqrMagnitude < 0.01f) return;

        Vector3 camForward = Vector3.ProjectOnPlane(cameraRoot.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraRoot.right, Vector3.up).normalized;
        Vector3 moveDir = (camForward * m_moveInput.y + camRight * m_moveInput.x).normalized;

        float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(visual.eulerAngles.y, targetAngle, ref m_visualTurnVelocity, RotationSmoothTime);

        visual.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }

    private void Jump()
    {
        m_rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
}