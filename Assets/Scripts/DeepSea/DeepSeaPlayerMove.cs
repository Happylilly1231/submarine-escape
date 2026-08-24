using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DeepSeaPlayerMove : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Transform modelTransform; // 3D 캐릭터 메쉬 (자식)
    [SerializeField] private Animator animator;
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("=== Camera Positions ===")]
    [SerializeField] private Transform cameraPosEye;      // 눈 앞 카메라 피벗 (기본/상승/하강/정지)
    [SerializeField] private Transform cameraPosTopHead;  // 정수리 카메라 피벗 (순수 평지 이동 시)
    [SerializeField] private Transform activeCameraPos;   // 실제 카메리가 따라다니는 피벗

    [Header("=== Move Settings ===")]
    private float normalSpeed = 5f;
    private float sprintSpeed = 15f;
    private float verticalSpeed = 6f;
    private float meshRotationSpeed = 8f; // 기존 반응성 유지
    private float cameraPosLerpSpeed = 8f;

    [Header("=== Oxygen Settings ===")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float currentOxygen = 100f;
    [SerializeField] private float sprintOxygenCostPerSec = 7f;

    #region External Accessors
    public float MouseX { get; private set; }
    public float MouseY { get; private set; }
    public float CurrentOxygen => currentOxygen;
    #endregion

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private bool _isSprintPressed;
    private bool _isAscendPressed;  // Space
    private bool _isDescendPressed; // Left Ctrl / C
    private float _verticalInput = 0f; // +1: 상승, -1: 하강, 0: 정지

    private Vector3 standingCenter = new Vector3(0, 1f, 0); // 서 있을 때 Center
    private Vector3 swimmingCenter = new Vector3(0, 0.65f, 0); // 엎드렸을 때 Center

    private readonly int _isSwimmingHash = Animator.StringToHash("IsSwimming");
    private readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.drag = 3f;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (modelTransform == null && transform.childCount > 0) modelTransform = transform.GetChild(0);
        if (animator == null && modelTransform != null) animator = modelTransform.GetComponentInParent<Animator>();
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.IsPausing) return;

        ReadInputs();
        HandleRotation();
        HandleAnimationAndMeshRotation();
        UpdateCameraPivot();
    }

    private void FixedUpdate()
    {
        if (GameManager.instance != null && GameManager.instance.IsPausing) return;

        HandleMovement();
    }

    private void ReadInputs()
    {
        var moveAction = playerInput.actions.FindAction("DeepSea/Move");
        var lookAction = playerInput.actions.FindAction("DeepSea/Look");
        var sprintAction = playerInput.actions.FindAction("DeepSea/Sprint");
        var ascendAction = playerInput.actions.FindAction("DeepSea/Ascend");
        var descendAction = playerInput.actions.FindAction("DeepSea/Descend");

        _moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        _isSprintPressed = sprintAction != null && sprintAction.IsPressed();

        // 1. 단순 키 할당 (기존 방식 유지)
        bool ascendNow = ascendAction != null ? ascendAction.IsPressed() : Keyboard.current.spaceKey.isPressed;
        bool descendNow = descendAction != null ? descendAction.IsPressed() : (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed);

        // 2. 입력 변화 감지 및 최신 입력 우선 처리
        if (ascendNow && !_isAscendPressed)
        {
            // 상승 키를 방금 새로 눌렀음 -> 상승 우선
            _verticalInput = 1f;
        }
        else if (descendNow && !_isDescendPressed)
        {
            // 하강 키를 방금 새로 눌렀음 -> 하강 우선
            _verticalInput = -1f;
        }
        else if (!ascendNow && !descendNow)
        {
            // 둘 다 안 누름
            _verticalInput = 0f;
        }
        else if (ascendNow && !descendNow)
        {
            _verticalInput = 1f;
        }
        else if (!ascendNow && descendNow)
        {
            _verticalInput = -1f;
        }

        _isAscendPressed = ascendNow;
        _isDescendPressed = descendNow;

        // 마우스 회전값 등 기존 로직...
        float sensitivity = GameManager.instance != null ? GameManager.instance.MouseSensitivity : 1f;
        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        MouseX = lookInput.x * sensitivity;
        MouseY = lookInput.y * sensitivity;
    }

    private void HandleRotation()
    {
        transform.Rotate(Vector3.up * MouseX);
    }

    private void HandleMovement()
    {
        Transform mainCam = Camera.main != null ? Camera.main.transform : transform;

        Vector3 camForward = Vector3.ProjectOnPlane(mainCam.forward, Vector3.up).normalized;
        Vector3 camRight = mainCam.right;

        Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        bool isVerticalMoving = _isAscendPressed || _isDescendPressed;
        bool isSprinting = _isSprintPressed && currentOxygen > 0f && (_moveInput.sqrMagnitude > 0.1f || isVerticalMoving);
        float currentSpeed = isSprinting ? sprintSpeed : normalSpeed;

        if (isSprinting)
        {
            ConsumeOxygen(sprintOxygenCostPerSec * Time.fixedDeltaTime);
        }

        Vector3 targetVelocity = moveDir * currentSpeed;

        // _verticalInput 값을 직접 적용 (1: 상승, -1: 하강)
        targetVelocity.y = _verticalInput * verticalSpeed;

        _rb.AddForce(targetVelocity - _rb.velocity, ForceMode.VelocityChange);
    }

    private void HandleAnimationAndMeshRotation()
    {
        Vector3 velocity = _rb.velocity;
        // 오직 W(전진) 입력이 있을 때만 전진 수영 판단!
        bool isForwardMoving = _moveInput.y > 0.1f; // W 전진 중인가?

        // 1. 메쉬 회전 보정 (상승/하강 Pitch & Roll)
        if (modelTransform != null)
        {
            float targetPitch = 0f;
            float targetRoll = 0f;

            // 1. 상승/하강 Pitch
            if (_verticalInput > 0f) targetPitch = -35f;
            else if (_verticalInput < 0f) targetPitch = 35f;

            // 2. A/D Roll (몸 좌우 기울임)
            if (_moveInput.x < -0.1f) targetRoll = 15f;
            else if (_moveInput.x > 0.1f) targetRoll = -15f;

            Quaternion targetLocalRotation;

            // W(전진) + 상승/하강 대각선 이동 시 진행 방향을 바라봄
            if (isForwardMoving && !Mathf.Approximately(_verticalInput, 0f))
            {
                Vector3 localVel = transform.InverseTransformDirection(velocity);
                targetLocalRotation = Quaternion.LookRotation(localVel);
            }
            else
            {
                // 그 외(S, A, D, 단독 상승/하강)는 정면 고정 후 Pitch/Roll 오프셋 적용
                targetLocalRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
            }

            modelTransform.localRotation = Quaternion.Slerp(
                modelTransform.localRotation,
                targetLocalRotation,
                Time.deltaTime * meshRotationSpeed
            );
        }

        // 2. [핵심] 애니메이션 엎드림 포즈에 맞춘 콜라이더 축/Center 보정
        if (capsuleCollider != null)
        {
            if (isForwardMoving)
            {
                // W 전진 수영 중일 때: Z축 방향(2)으로 콜라이더를 누움
                capsuleCollider.direction = 2; // 0: X-Axis, 1: Y-Axis, 2: Z-Axis
                capsuleCollider.center = swimmingCenter;
            }
            else
            {
                // 서 있을 때 (Idle, S, A, D): Y축 방향(1)으로 세움
                capsuleCollider.direction = 1;
                capsuleCollider.center = standingCenter;
            }
        }

        // 3. 애니메이션 제어
        if (animator != null)
        {
            animator.SetBool(_isSwimmingHash, isForwardMoving);

            bool isSprinting = _isSprintPressed && currentOxygen > 0f;
            float speedMultiplier = isSprinting ? 1.5f : 1.0f;
            animator.SetFloat(_swimSpeedHash, speedMultiplier);
        }
    }

    private void UpdateCameraPivot()
    {
        if (activeCameraPos == null || cameraPosEye == null || cameraPosTopHead == null) return;

        bool isAscending = _verticalInput > 0f;      // 상승 중 (Space)
        bool isDescending = _verticalInput < 0f;     // 하강 중 (Ctrl/C)
        bool isForwardMoving = _moveInput.y > 0.1f; // W(전진) 키 입력 중

        bool needsTopHeadPivot;

        if (isAscending)
        {
            // 상승 중일 때는 W를 같이 누르더라도 예외 없이 눈 앞(Eye) 피벗 사용!
            needsTopHeadPivot = false;
        }
        else
        {
            // 상승이 아닐 때만 W 전진 또는 하강일 때 정수리(TopHead) 피벗 사용
            needsTopHeadPivot = isForwardMoving || isDescending;
        }

        Transform targetPivot = needsTopHeadPivot ? cameraPosTopHead : cameraPosEye;

        activeCameraPos.position = Vector3.Lerp(
            activeCameraPos.position,
            targetPivot.position,
            Time.deltaTime * cameraPosLerpSpeed
        );
    }

    public void ConsumeOxygen(float amount)
    {
        currentOxygen = Mathf.Max(0f, currentOxygen - amount);
    }

    public void RechargeOxygen(float amount)
    {
        currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
    }
}