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

    [Header("=== Area Reference ===")]
    [SerializeField] private DeepSeaAreaController areaController; // 에디터에서 연결

    [Header("=== 1인칭 회피 연출 ===")]
    [SerializeField] private ParticleSystem cameraDashBubbleFX; // Main Camera 자식으로 둔 파티클

    [Header("=== Camera Positions ===")]
    [SerializeField] private Transform cameraPosEye;      // 눈 앞 카메라 피벗 (기본/상승/하강/정지)
    [SerializeField] private Transform cameraPosTopHead;  // 정수리 카메라 피벗 (순수 평지 이동 시)
    [SerializeField] private Transform activeCameraPos;   // 실제 카메리가 따라다니는 피벗

    [Header("=== Move Settings ===")]
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float sprintSpeed = 15f;
    [SerializeField] private float verticalSpeed = 6f;
    private float meshRotationSpeed = 8f;
    private float cameraPosLerpSpeed = 8f;

    [Header("=== Dash (Evasion) Settings ===")]
    [SerializeField] private float dashForce = 22f;          // 회피 순간 속도 (Impulse)
    [SerializeField] private float dashDuration = 0.2f;      // 회피 유지 시간
    [SerializeField] private float dashCooldown = 0.8f;      // 회피 재사용 대기시간
    [SerializeField] private float holdThreshold = 0.2f;     // 이 시간(초) 이상 누르면 가속(Sprint)으로 판정

    [Header("=== Oxygen Settings ===")]
    [SerializeField] private float maxOxygen = 100f;
    public float MaxOxygen => maxOxygen;         // 기존 최대 산소 변수명에 맞게 연결
    [SerializeField] private float currentOxygen = 100f;
    public float CurrentOxygen => currentOxygen; // 기존 산소 변수명에 맞게 연결
    [SerializeField] private float baseOxygenCostPerSec = 0.2f;   // 기본 초당 산소 소모 (약 8분 분량)
    [SerializeField] private float sprintOxygenCostPerSec = 1.3f; // 가속 이동 시 추가 소모 (총 1.5/s)
    [SerializeField] private float dashOxygenCost = 8.0f;         // 회피 1회당 산소 소모

    #region External Accessors
    public float MouseX { get; private set; }
    public float MouseY { get; private set; }
    public bool IsDashing => _isDashing;
    #endregion

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private bool _isSprintPressed;
    private bool _isAscendPressed;  // Space
    private bool _isDescendPressed; // Left Ctrl / C
    private float _verticalInput = 0f; // +1: 상승, -1: 하강, 0: 정지

    // Shift 입력 버퍼 및 회피 상태 변수
    private float _shiftPressStartTime;
    private bool _isShiftHeld;
    private bool _isDashing;
    private float _lastDashTime = -999f;
    private Vector3 _dashDirection;

    private Vector3 standingCenter = new Vector3(0, 1f, 0);
    private Vector3 swimmingCenter = new Vector3(0, 0.65f, 0);

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
        HandleShiftInputLogic();
        HandleBaseOxygenConsumption();
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
        _isShiftHeld = sprintAction != null && sprintAction.IsPressed();

        // 상승 / 하강 최신 입력 처리
        bool ascendNow = ascendAction != null ? ascendAction.IsPressed() : Keyboard.current.spaceKey.isPressed;
        bool descendNow = descendAction != null ? descendAction.IsPressed() : (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed);

        // 수면 위인지 체크
        bool isAtSurface = areaController != null && transform.position.y >= areaController.SurfaceY;

        if (isAtSurface)
        {
            // 수면에 도달하면 위/아래 키 입력을 무시하여 수직 이동을 막음
            _verticalInput = 0f;
        }
        else
        {
            // 기존 상승/하강 최신 입력 처리 로직
            if (ascendNow && !_isAscendPressed) _verticalInput = 1f;
            else if (descendNow && !_isDescendPressed) _verticalInput = -1f;
            else if (!ascendNow && !descendNow) _verticalInput = 0f;
            else if (ascendNow && !descendNow) _verticalInput = 1f;
            else if (!ascendNow && descendNow) _verticalInput = -1f;
        }

        _isAscendPressed = ascendNow;
        _isDescendPressed = descendNow;

        // 마우스 감도 처리
        float sensitivity = GameManager.instance != null ? GameManager.instance.MouseSensitivity : 1f;
        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        MouseX = lookInput.x * sensitivity;
        MouseY = lookInput.y * sensitivity;
    }

    // Shift 클릭(회피)과 길게 누르기(가속) 분기 로직
    private void HandleShiftInputLogic()
    {
        if (_isShiftHeld)
        {
            if (_shiftPressStartTime == 0f)
            {
                _shiftPressStartTime = Time.time;
            }

            // 지정한 시간(0.2초) 이상 누르고 있으면 가속 이동 활성화
            if (Time.time - _shiftPressStartTime >= holdThreshold)
            {
                _isSprintPressed = true;
                Debug.Log("가속 이동 시작!");
            }
        }
        else
        {
            // Shift를 뗐을 때: 누른 시간이 threshold 미만이었고 쿨타임이 지났다면 회피(Dash) 실행
            if (_shiftPressStartTime > 0f)
            {
                float pressDuration = Time.time - _shiftPressStartTime;
                if (pressDuration < holdThreshold && Time.time >= _lastDashTime + dashCooldown)
                {
                    TryExecuteDash();
                }
            }

            _shiftPressStartTime = 0f;
            _isSprintPressed = false;
        }
    }

    // 회피 실행 함수
    private void TryExecuteDash()
    {
        if (currentOxygen < dashOxygenCost || _isDashing) return;

        ConsumeOxygen(dashOxygenCost);
        _lastDashTime = Time.time;
        StartCoroutine(DashCoroutine());
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;

        Transform mainCam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 camForward = Vector3.ProjectOnPlane(mainCam.forward, Vector3.up).normalized;
        Vector3 camRight = mainCam.right;

        Vector3 inputDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
        if (inputDir.sqrMagnitude < 0.01f)
        {
            inputDir = transform.forward;
        }

        inputDir.y = _verticalInput;
        _dashDirection = inputDir.normalized;

        // 1. 카메라 FOV 연출 (속도감)
        if (Camera.main != null)
        {
            StartCoroutine(DashFOVCoroutine(Camera.main));
        }

        // 2. 1인칭 시야 기포 연출 재생
        if (cameraDashBubbleFX != null)
        {
            cameraDashBubbleFX.Play();
        }

        Debug.Log("회피! " + _dashDirection);

        // 3. dashDuration 동안 회피 속도 강제 유지 (물리 감쇄 방지)
        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            _rb.velocity = _dashDirection * dashForce;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _isDashing = false;
    }

    private IEnumerator DashFOVCoroutine(Camera mainCam)
    {
        float startFOV = mainCam.fieldOfView;
        float targetFOV = startFOV + 8f; // 순간적으로 8도 넓혀 속도감 연출

        // 순간 확장
        float t = 0f;
        while (t < 0.08f)
        {
            mainCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t / 0.08f);
            t += Time.deltaTime;
            yield return null;
        }

        // 복구
        t = 0f;
        while (t < 0.15f)
        {
            mainCam.fieldOfView = Mathf.Lerp(targetFOV, startFOV, t / 0.15f);
            t += Time.deltaTime;
            yield return null;
        }

        mainCam.fieldOfView = startFOV;
    }

    private void HandleBaseOxygenConsumption()
    {
        // 평소에도 지속해서 기본 산소 감소
        ConsumeOxygen(baseOxygenCostPerSec * Time.deltaTime);
    }

    private void HandleRotation()
    {
        transform.Rotate(Vector3.up * MouseX);
    }

    private void HandleMovement()
    {
        // 회피 중일 때는 회피 물리력이 움직임을 제어함
        if (_isDashing) return;

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
        targetVelocity.y = _verticalInput * verticalSpeed;

        _rb.AddForce(targetVelocity - _rb.velocity, ForceMode.VelocityChange);
    }

    private void HandleAnimationAndMeshRotation()
    {
        bool isForwardMoving = _moveInput.y > 0.1f;

        if (modelTransform != null)
        {
            float targetPitch = 0f;
            float targetRoll = 0f;

            if (_verticalInput > 0f) targetPitch = -35f;
            else if (_verticalInput < 0f) targetPitch = 35f;

            if (_moveInput.x < -0.1f) targetRoll = 15f;
            else if (_moveInput.x > 0.1f) targetRoll = -15f;

            Quaternion targetLocalRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);

            modelTransform.localRotation = Quaternion.Slerp(
                modelTransform.localRotation,
                targetLocalRotation,
                Time.deltaTime * meshRotationSpeed
            );
        }

        if (capsuleCollider != null)
        {
            if (isForwardMoving)
            {
                capsuleCollider.direction = 2; // Z-Axis
                capsuleCollider.center = swimmingCenter;
            }
            else
            {
                capsuleCollider.direction = 1; // Y-Axis
                capsuleCollider.center = standingCenter;
            }
        }

        if (animator != null)
        {
            animator.SetBool(_isSwimmingHash, isForwardMoving);

            bool isSprinting = _isSprintPressed && currentOxygen > 0f;
            float speedMultiplier = isSprinting ? 1.5f : (_isDashing ? 2.0f : 1.0f);
            animator.SetFloat(_swimSpeedHash, speedMultiplier);
        }
    }

    private void UpdateCameraPivot()
    {
        if (activeCameraPos == null || cameraPosEye == null || cameraPosTopHead == null) return;

        bool isAscending = _verticalInput > 0f;
        bool isDescending = _verticalInput < 0f;
        bool isForwardMoving = _moveInput.y > 0.1f;

        bool needsTopHeadPivot = !isAscending && (isForwardMoving || isDescending);

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