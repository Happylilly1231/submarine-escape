using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DeepSeaPlayerMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform modelTransform; // 실제 3D 모델의 트랜스폼
    public Transform ModelTransform => modelTransform;
    [SerializeField] private Animator animator; // 애니메이터 (모델)
    [SerializeField] private CapsuleCollider capsuleCollider; // 콜라이더 (모델)
    [SerializeField] private DeepSeaAreaController deepSeaAreaController; // 심해 구역 컨트롤러 (수면 높이 값 필요해서)
    [SerializeField] private ParticleSystem cameraDashBubbleFX; // 버블 파티클
    [SerializeField] private Transform cameraPosHead; // 머리 카메라 위치

    [Header("이동")]
    [SerializeField] private float normalSpeed = 5f; // 기본 이동 속도
    [SerializeField] private float sprintSpeed = 15f; // 가속 이동 속도
    private float meshRotationSpeed = 8f; // 메쉬(모델) 회전 속도
    private Vector3 _moveDir2D; // 2차원(X, Z) 이동 방향
    private Vector3 _moveDir3D; // 3차원(X, Y, Z) 이동 방향

    [Header("회피")]
    [SerializeField] private float dashForce = 35f; // 회피 속도
    [SerializeField] private float dashDuration = 0.3f; // 회피하는 시간
    [SerializeField] private float holdSprintThreshold = 0.2f; // 가속 이동으로 전환되는 임계 시간 (이 이상 Shift 키 누르면 회피가 아닌 가속 이동)
    private Coroutine _currentCheckSprintHoldCoroutine; // 현재 Shift 키 hold 시간 체크 코루틴 (가속 이동 여부 판단)
    private float _shiftPressStartTime = 0f; // Shift키를 딱 누른 시간 
    private Vector3 _dashDirection; // 회피 방향
    private bool _isSprintPressed = false; // 가속 이동 Shift 키가 눌리고 있는지 여부 (실제 가속 이동 중 여부와 다름, 정지해있으면 가속이 아니라 기본 초당 산소 소모량 소모)
    private bool _hasSprintTriggered = false; // 이번 Shift 입력 동안 가속으로 전환되었는지 여부 플래그

    [Header("산소")]
    [SerializeField] private float baseOxygenCostPerSec = 0.2f; // 기본 초당 산소 소모량 (약 8분 분량)
    [SerializeField] private float sprintOxygenCostPerSec = 1.5f; // 가속 이동 시 산소 소모량
    [SerializeField] private float dashOxygenCost = 8.0f; // 회피 시 산소 소모량

    // 입력
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _shiftAction;
    private InputAction _ascendAction;
    private InputAction _descendAction;
    private Vector2 _moveInput; // 2차원 이동 입력
    private float _verticalInput = 0f; // 상하 이동 입력

    // 애니메이션
    // private readonly int _isSwimmingHash = Animator.StringToHash("IsSwimming");
    private readonly int _isCrawlSwimmingHash = Animator.StringToHash("IsCrawlSwimming");
    private readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

    // 기타
    private Rigidbody _rb;
    private Vector3 _standingCenter = new Vector3(0, 1f, 0);
    private Vector3 _swimmingCenter = new Vector3(0, 0.65f, 0);


    #region 다른 데서 사용할 수 있는 변수들 (추가 가능)

    public float MaxOxygen { get; private set; } = 100f; // 최대 산소량
    public float CurrentOxygen { get; private set; } = 100f; // 현재 산소량
    public Vector3 MoveDir2D => _moveDir2D; // 2차원(X, Z) 이동 방향
    public Vector3 MoveDir3D => _moveDir3D; // 3차원(X, Y, Z) 이동 방향
    public Vector2 MoveInput => _moveInput; // 2차원 이동 입력
    public float VerticalInput => _verticalInput; // 상하 이동 입력
    public float MouseX { get; private set; } // 마우스 좌우 값
    public float MouseY { get; private set; } // 마우스 상하 값
    public bool IsDashing { get; private set; } = false; // 회피 중인지 여부
    public bool IsSprinting { get; private set; } = false; // 가속 이동 중인지 여부

    #endregion


    #region 생명주기

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // 입력 관련 변수 가져오기
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions.FindAction("DeepSea/Move");
        _lookAction = _playerInput.actions.FindAction("DeepSea/Look");
        _shiftAction = _playerInput.actions.FindAction("DeepSea/Sprint");
        _ascendAction = _playerInput.actions.FindAction("DeepSea/Ascend");
        _descendAction = _playerInput.actions.FindAction("DeepSea/Descend");
    }

    private void OnEnable()
    {
        // Shift 키 입력 이벤트 구독
        _shiftAction.started += OnShiftStarted;
        _shiftAction.canceled += OnShiftCanceled;
    }

    private void OnDisable()
    {
        // Shift 키 입력 이벤트 구독 해제
        _shiftAction.started -= OnShiftStarted;
        _shiftAction.canceled -= OnShiftCanceled;
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing) return;

        ReadInputs(); // 입력 가져오기
        Rotate(); // 좌우 회전
        UpdateAnimationAndCollider(); // 애니메이션 & 콜라이더 업데이트
        UpdateModelRotation(); // 모델 회전 업데이트
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        Move(); // Rigidbody를 이용한 이동
    }

    #endregion

    #region 산소

    /// <summary>
    /// 산소 소모
    /// </summary>
    /// <param name="amount">소모량</param>
    public void ConsumeOxygen(float amount)
    {
        CurrentOxygen = Mathf.Max(0f, CurrentOxygen - amount);
        if (CurrentOxygen == 0f)
        {
            Debug.Log("=== Game Over ===");
            // GameManager.instance.GameOver(EEndingType.MonsterDeath); // 임시로 괴물한테 죽음 엔딩으로 해놓음
        }
    }

    /// <summary>
    /// 산소 충전
    /// </summary>
    /// <param name="amount">충전량</param>
    public void RechargeOxygen(float amount)
    {
        CurrentOxygen = Mathf.Min(MaxOxygen, CurrentOxygen + amount);
    }

    #endregion

    #region 입력

    /// <summary>
    /// 입력 가져오기
    /// </summary>
    private void ReadInputs()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        // 상승/하강 현재 프레임에 눌려있는지 여부
        bool ascendNow = _ascendAction.IsPressed();
        bool descendNow = _descendAction.IsPressed();

        // 수면 위인지 체크
        bool isAtSurface = transform.position.y >= deepSeaAreaController.SurfaceY;

        if (isAtSurface) // 수면 위 -> 상하 이동 불가
        {
            _verticalInput = 0f;
        }
        else // 수면 아래
        {
            // 1. 둘 다 누르고 있을 때: 기존에 잡혀있던 입력(_verticalInput)을 계속 유지 (나중에 누른 키 무시)
            if (ascendNow && descendNow)
            {
                // 만약 아무것도 안 누른 상태에서 두 키가 완전히 동시 입력되었다면 상승 기본값 적용
                if (_verticalInput == 0f)
                    _verticalInput = 1f;
            }
            // 2. 상승 키만 누르고 있거나, (둘 다 누르다가 하강 키를 떼서 상승 키만 남았을 때)
            else if (ascendNow)
            {
                _verticalInput = 1f;
            }
            // 3. 하강 키만 누르고 있거나, (둘 다 누르다가 상승 키를 떼서 하강 키만 남았을 때)
            else if (descendNow)
            {
                _verticalInput = -1f;
            }
            // 4. 아무것도 안 누르고 있을 때
            else
            {
                _verticalInput = 0f;
            }
        }

        // 시야 입력 처리
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();
        float sensitivity = GameManager.instance.MouseSensitivity;
        MouseX = lookInput.x * sensitivity;
        MouseY = lookInput.y * sensitivity;
    }

    #endregion

    #region Shift - 회피/가속

    /// <summary>
    /// Shift를 누르는 순간 실행
    /// </summary>
    /// <param name="context"></param>
    private void OnShiftStarted(InputAction.CallbackContext context)
    {
        _hasSprintTriggered = false; // 새로 누를 때 초기화

        // 가속 전환 코루틴 시작
        if (_currentCheckSprintHoldCoroutine != null) StopCoroutine(_currentCheckSprintHoldCoroutine);
        _currentCheckSprintHoldCoroutine = StartCoroutine(CheckSprintHoldCoroutine());
    }

    /// <summary>
    /// Shift를 떼는 순간 실행
    /// </summary>
    /// <param name="context"></param>
    private void OnShiftCanceled(InputAction.CallbackContext context)
    {
        // 코루틴 중단
        if (_currentCheckSprintHoldCoroutine != null)
        {
            StopCoroutine(_currentCheckSprintHoldCoroutine);
            _currentCheckSprintHoldCoroutine = null;
        }

        // [핵심] 뗄 때까지 가속(_isSprintPressed)이 켜진 적이 없다면 = '톡' 짧게 누른 것 -> 회피 실행!
        if (!_hasSprintTriggered)
        {
            TryExecuteDash();
        }

        // 뗐으므로 가속 해제
        _isSprintPressed = false;
    }

    /// <summary>
    /// 가속 임계 시간 도달 시 가속 이동 활성화 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckSprintHoldCoroutine()
    {
        yield return new WaitForSeconds(holdSprintThreshold);

        // 지정된 시간(0.25초) 동안 꾹 누르고 있었다면 가속 상태로 전환됨을 마킹
        _isSprintPressed = true;
        _hasSprintTriggered = true;
        Debug.Log("가속 이동 시작!");
    }

    /// <summary>
    /// 회피 시도 (회피 산소 소모량만큼 남아있고, 회피 중이 아닐 때 실행)
    /// </summary>
    private void TryExecuteDash()
    {
        if (IsDashing)
        {
            Debug.Log("이미 회피 중");
            return;
        }

        if (CurrentOxygen < dashOxygenCost)
        {
            Debug.Log("회피에 필요한 산소가 부족합니다! 회피 실패!");
            return;
        }

        ConsumeOxygen(dashOxygenCost);
        StartCoroutine(DashCoroutine());
    }

    /// <summary>
    /// 회피 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator DashCoroutine()
    {
        Debug.Log("회피!");
        IsDashing = true; // 회피 중으로 설정

        // 회피 방향 결정
        if (_moveDir3D.sqrMagnitude > 0.01f) // 이동 중 -> 그대로 이동 방향 사용
            _dashDirection = _moveDir3D;
        else // 정지 -> 뒤 방향
            _dashDirection = -transform.forward;

        // 버블 연출을 실제 회피 방향으로 회전 후 재생
        // cameraDashBubbleFX.transform.rotation = Quaternion.LookRotation(_dashDirection);
        cameraDashBubbleFX.Play();

        // FOV 연출
        StartCoroutine(DashFOVCoroutine(Camera.main));

        // 물리 시간(FixedUpdate) 기준으로 정확히 대기
        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 회피 종료
        IsDashing = false;
    }

    /// <summary>
    /// 회피 시 FOV 연출 코루틴 
    /// </summary>
    /// <param name="mainCam"></param>
    /// <returns></returns>
    private IEnumerator DashFOVCoroutine(Camera mainCam)
    {
        float startFOV = mainCam.fieldOfView;
        float targetFOV = startFOV + 8f; // 순간적으로 8도 넓혀 속도감 연출

        // 순간 확장
        float t = 0f;
        while (t < 0.15f)
        {
            mainCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t / 0.08f);
            t += Time.deltaTime;
            yield return null;
        }

        // 복구
        t = 0f;
        while (t < 0.05f)
        {
            mainCam.fieldOfView = Mathf.Lerp(targetFOV, startFOV, t / 0.15f);
            t += Time.deltaTime;
            yield return null;
        }

        mainCam.fieldOfView = startFOV;
    }

    #endregion

    #region 이동 & 회전

    /// <summary>
    /// 좌우 회전
    /// </summary>
    private void Rotate()
    {
        transform.Rotate(Vector3.up * MouseX);
    }

    /// <summary>
    /// 이동
    /// </summary>
    private void Move()
    {
        // // 카메라 방향 가져오기
        // Vector3 camForward = Vector3.ProjectOnPlane(_mainCam.forward, Vector3.up).normalized; // 상하 성분 제거 (그냥 앞뒤좌우 이동할 때는 카메라 정면 방향에 따라 상하 이동이 불가해야 하므로)
        // Vector3 camRight = _mainCam.right;

        // 2차원 이동 방향 - 카메라 정면 방향 기준 앞뒤좌우 (상하 X)
        _moveDir2D = (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized;

        // 3차원 이동 방향 - 2차원 이동 방향 + 수직 이동 방향
        _moveDir3D = _moveDir2D;
        _moveDir3D.y = _verticalInput;
        if (_moveDir3D.sqrMagnitude > 0.01f)
        {
            _moveDir3D.Normalize();
        }

        // 현재 가속 이동 중인지 여부 갱신
        IsSprinting = _isSprintPressed && CurrentOxygen > 0f && _moveDir3D.sqrMagnitude > 0.01f;

        // 이동 속도 정하기 - 회피 속도 / 일반 속도
        Vector3 targetVelocity;
        if (IsDashing)
        {
            // 회피 중일 때는 회피 전용 방향과 속도 사용
            targetVelocity = _dashDirection * dashForce;
        }
        else
        {
            float currentSpeed = IsSprinting ? sprintSpeed : normalSpeed;
            targetVelocity = _moveDir3D * currentSpeed;
        }

        // 산소 소모
        if (IsSprinting)
        {
            ConsumeOxygen(sprintOxygenCostPerSec * Time.fixedDeltaTime);
        }
        else
        {
            // 평소에도 지속해서 기본 산소 감소
            ConsumeOxygen(baseOxygenCostPerSec * Time.deltaTime);
        }

        // MovePosition으로 변경 (현재 위치 + (속도 * 시간 delta))
        Vector3 nextPos = _rb.position + targetVelocity * Time.fixedDeltaTime;
        _rb.MovePosition(nextPos);
    }

    #endregion

    #region 방향에 따른 업데이트

    /// <summary>
    /// 애니메이션 & 콜라이더 갱신
    /// </summary>
    private void UpdateAnimationAndCollider()
    {
        bool isCrawlSwimming = _moveDir3D.sqrMagnitude >= 0.01f;
        animator.SetBool(_isCrawlSwimmingHash, isCrawlSwimming);

        // 가속 이동, 회피 여부에 따라 애니메이션 Multiplier 변수값 지정
        float speedMultiplier = IsSprinting ? 1.5f : (IsDashing ? 2.0f : 1.0f);
        animator.SetFloat(_swimSpeedHash, speedMultiplier);

        // // 콜라이더 변경
        // if (isSwimming)
        // {
        //     capsuleCollider.direction = 2; // Z-Axis (수영 상태)
        //     capsuleCollider.center = _swimmingCenter;
        // }
        // else
        // {
        //     capsuleCollider.direction = 1; // Y-Axis (서있는 상태)
        //     capsuleCollider.center = _standingCenter;
        // }
    }

    /// <summary>
    /// 모델 회전
    /// </summary>
    private void UpdateModelRotation()
    {
        float targetXRot = 0f;
        float targetYRot = 0f; // Y축 회전 값 변수 추가
        float targetZRot = 0f;

        // x축 회전 값 계산
        if (_verticalInput > 0f) // 위
        {
            if (_moveInput.y > 0.1f) targetXRot = 30f;  // 위 + 앞
            else if (_moveInput.y < -0.1f) targetXRot = -30f; // 위 + 뒤
            else targetXRot = 0f;  // 위로만
        }
        else if (_verticalInput < 0f) // 아래
        {
            if (_moveInput.y > 0.1f) targetXRot = 120f;   // 아래 + 앞
            else if (_moveInput.y < -0.1f) targetXRot = 120f;  // 아래 + 뒤
            else targetXRot = 90f;   // 아래로만
        }

        // // y축 회전 값 계산 (아래를 볼 때 180도, 아니면 0도)
        // if (_verticalInput < 0f)
        // {
        //     targetYRot = 180f;
        // }
        // else
        // {
        //     targetYRot = 0f;
        // }

        // z축 회전 값 계산
        if (_moveInput.x < -0.1f) targetZRot = 45f;
        else if (_moveInput.x > 0.1f) targetZRot = -45f;

        // 모델 회전
        // 오일러 변환을 거치지 않고 X축 및 Z축 회전 쿼터니언을 각각 생성
        Quaternion xRot = Quaternion.AngleAxis(targetXRot, Vector3.right);   // X축 회전 (Pitch)
        Quaternion yRot = Quaternion.AngleAxis(targetYRot, Vector3.up);      // Y축 회전 (Yaw)
        Quaternion zRot = Quaternion.AngleAxis(targetZRot, Vector3.forward); // Z축 회전 (Roll)

        // 세 회전을 결합 (Y축 회전 -> Z축 회전 -> X축 회전 적용 순서)
        Quaternion targetLocalRotation = yRot * zRot * xRot;

        modelTransform.localRotation = Quaternion.Slerp(
            modelTransform.localRotation,
            targetLocalRotation,
            Time.deltaTime * meshRotationSpeed
        );

        // if (_verticalInput < 0f)
        // {
        //     // Y축 회전을 180도로 설정 (X, Z축은 기존 회전 유지)
        //     cameraPosHead.localRotation = Quaternion.Euler(
        //         cameraPosHead.localRotation.eulerAngles.x,
        //         180f,
        //         cameraPosHead.localRotation.eulerAngles.z
        //     );
        // }
        // else
        // {
        //     // Y축 회전을 0도로 설정 (X, Z축은 기존 회전 유지)
        //     cameraPosHead.localRotation = Quaternion.Euler(
        //         cameraPosHead.localRotation.eulerAngles.x,
        //         0f,
        //         cameraPosHead.localRotation.eulerAngles.z
        //     );
        // }

        // // 위/아래 키를 눌른 그 순간에는 즉시 -70도로 고정 (애니메이션 튀는 현상 차단)
        // if (_ascendAction.WasPressedThisFrame() || _descendAction.WasPressedThisFrame())
        // {
        //     modelTransform.localRotation = targetLocalRotation;
        // }
        // else
        // {
        //     // 키를 뗐거나 이미 누르고 있는 상태에서는 0도 또는 목표 회전으로 부드럽게(Slerp) 보간
        //     modelTransform.localRotation = Quaternion.Slerp(
        //         modelTransform.localRotation,
        //         targetLocalRotation,
        //         Time.deltaTime * meshRotationSpeed
        //     );
        // }

        // // modelTransform.localRotation = targetLocalRotation;

        // // // [핵심 해결책] 위/아래 키를 '처음 누른 프레임'이라면 보간(Slerp) 없이 즉시 회전 적용!
        // // // 애니메이션이 Idle -> Swim으로 바뀌면서 발생하는 역방향 튀는 현상을 완전 차단합니다.
        // // if (_ascendAction.WasPressedThisFrame() || _descendAction.WasPressedThisFrame())
        // // {
        // //     modelTransform.localRotation = targetLocalRotation;
        // // }
        // // else
        // // {
        // //     // 이동 중이거나 가만히 있을 때는 기존처럼 부드럽게 보간
        // //     modelTransform.localRotation = Quaternion.Slerp(
        // //         modelTransform.localRotation,
        // //         targetLocalRotation,
        // //         Time.deltaTime * meshRotationSpeed
        // //     );
        // // }
    }

    #endregion
}