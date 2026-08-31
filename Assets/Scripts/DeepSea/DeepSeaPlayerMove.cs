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
    [SerializeField] private float normalSpeed = 6f; // 기본 이동 속도
    [SerializeField] private float sprintSpeed = 9.5f; // 가속 이동 속도
    [SerializeField] private float slowSpeed = 4.2f; // 느려질 때 속도
    private float meshRotationSpeed = 8f; // 메쉬(모델) 회전 속도
    private Vector3 _moveDir2D; // 2차원(X, Z) 이동 방향
    private Vector3 _moveDir3D; // 3차원(X, Y, Z) 이동 방향

    [Header("회피")]
    [SerializeField] private float dashForce = 12f; // 회피 속도
    [SerializeField] private float dashDuration = 0.3f; // 회피하는 시간
    private Vector3 _dashDirection; // 회피 방향
    private bool _isSprintPressed = false; // 가속 이동 Shift 키가 눌리고 있는지 여부 (실제 가속 이동 중 여부와 다름, 정지해있으면 가속이 아니라 기본 초당 산소 소모량 소모)

    [Header("산소")]
    [SerializeField] private float baseOxygenCostPerSec = 0.1f; // 기본 초당 산소 소모량
    [SerializeField] private float sprintOxygenCostPerSec = 0.8f; // 가속 이동 시 산소 소모량
    [SerializeField] private float dashOxygenCost = 6f; // 회피 시 산소 소모량

    // 입력
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;
    private InputAction _dodgeAction;
    private InputAction _ascendAction;
    private InputAction _descendAction;
    private Vector2 _moveInput; // 2차원 이동 입력
    private float _verticalInput = 0f; // 상하 이동 입력

    // 애니메이션
    private readonly int _isCrawlSwimmingHash = Animator.StringToHash("IsCrawlSwimming");
    private readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

    // 기타
    private Rigidbody _rb;
    private Transform _mainCamTransform;
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

    /// <summary>
    /// 게임 표기용 현재 수심 (1/2배 적용된 Y좌표 - 수심 350 ~ 0m(수면))
    /// 외부 UI나 다른 스크립트에서 playerMove.DisplayDepth 로 접근
    /// </summary>
    public float DisplayDepth
    {
        get
        {
            // 700 - (현재 y - (-50))
            float rawDepthY = deepSeaAreaController.MaxDepthY - (transform.position.y - deepSeaAreaController.BottomY);

            // 수심 계산: 반으로 나누기
            float calculatedDepth = Mathf.Max(0f, rawDepthY) * 0.5f;

            return calculatedDepth;
        }
    }

    #endregion


    #region 생명주기

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCamTransform = Camera.main.transform;

        // 입력 관련 변수 가져오기
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions.FindAction("DeepSea/Move");
        _lookAction = _playerInput.actions.FindAction("DeepSea/Look");
        _sprintAction = _playerInput.actions.FindAction("DeepSea/Sprint");
        _dodgeAction = _playerInput.actions.FindAction("DeepSea/Dodge");
        _ascendAction = _playerInput.actions.FindAction("DeepSea/Ascend");
        _descendAction = _playerInput.actions.FindAction("DeepSea/Descend");
    }

    private void OnEnable()
    {
        // Shift 키 입력 이벤트 구독
        _sprintAction.started += OnShiftStarted;
        _sprintAction.canceled += OnShiftCanceled;
        _dodgeAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        // Shift 키 입력 이벤트 구독 해제
        _sprintAction.started -= OnShiftStarted;
        _sprintAction.canceled -= OnShiftCanceled;
        _dodgeAction.performed -= OnDashPerformed;
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
        _isSprintPressed = true;
    }

    /// <summary>
    /// Shift를 떼는 순간 실행
    /// </summary>
    /// <param name="context"></param>
    private void OnShiftCanceled(InputAction.CallbackContext context)
    {
        _isSprintPressed = false;
    }

    /// <summary>
    /// Q키 -> 회피
    /// </summary>
    /// <param name="context"></param>
    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        // 수면에 도달하면 회피 불가
        if (deepSeaAreaController.CurrentZone == SeaZone.Surface)
        {
            Debug.Log("이미 수면에 도달했으므로 회피할 수 없습니다!");
            return;
        }

        TryExecuteDash();
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
        else // 정지 -> 카메라 정면 방향
        {
            _dashDirection = _mainCamTransform.forward;
        }

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
    }

    #endregion
}