using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

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
    [SerializeField] private DeepSeaCameraController deepSeaCameraController;
    [SerializeField] private Volume damagedVolume;

    [Header("이동")]
    [SerializeField] private float normalSpeed = 6f; // 기본 이동 속도
    [SerializeField] private float sprintSpeed = 9.5f; // 가속 이동 속도
    [SerializeField] private float slowSpeed = 4.2f; // 느려질 때 속도
    [SerializeField] private float dragDownSpeed = 4f; // 끌려 내려갈 때 속도
    [SerializeField] private float meshRotationSpeed = 4f; // 메쉬(모델) 회전 속도
    private Vector3 _moveDir2D; // 2차원(X, Z) 이동 방향
    private Vector3 _moveDir3D; // 3차원(X, Y, Z) 이동 방향
    private float _xRotation = 0f; // 카메라 상하 회전값 (Pitch)

    [Header("회피")]
    [SerializeField] private float dashForce = 12f; // 회피 속도
    [SerializeField] private float dashDuration = 0.3f; // 회피하는 시간
    private Vector3 _dashDirection; // 회피 방향
    private bool _isSprintPressed = false; // 가속 이동 Shift 키가 눌리고 있는지 여부 (실제 가속 이동 중 여부와 다름, 정지해있으면 가속이 아니라 기본 초당 산소 소모량 소모)

    [Header("산소")]
    [SerializeField] private float baseOxygenCostPerSec = 0.1f; // 기본 초당 산소 소모량
    [SerializeField] private float sprintOxygenCostPerSec = 0.8f; // 가속 이동 시 산소 소모량
    [SerializeField] private float abnormalOxygenCostPerSec = 5f; // 비정상 산소 소모량
    [SerializeField] private float dashOxygenCost = 6f; // 회피 시 산소 소모량

    [SerializeField] private GameObject dragDownUI;
    [SerializeField] private Image leftTimeImage;
    [SerializeField] private TextMeshProUGUI spaceCountText;

    // 입력
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;
    private InputAction _ascendAction;
    private Vector2 _moveInput; // 2차원 이동 입력
    private float _verticalInput = 0f; // 상하 이동 입력

    // 애니메이션
    private readonly int _isCrawlSwimmingHash = Animator.StringToHash("IsCrawlSwimming");
    private readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

    // 붙잡힘
    private bool _isGrabbed = false;
    private float _qteTimeLimit = 3f; // QTE 제한 시간
    private int _requiredSpaceCount = 10; // 필요한 Space 연타 횟수
    private int _currentSpaceCount = 0; // 현재 스페이스 개수
    private float _qteTimer = 0f; // 현재 QTE 타이머
    public event Action OnGrabQTESuccess; // 잡혔을 때 탈출 QTE 성공 이벤트
    public event Action OnGrabQTEFailed; // 잡혔을 때 탈출 QTE 실패 이벤트

    // 대미지
    private bool _isKnockedBack = false; // 넉백 상태 플래그

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
    public bool IsOxgenNonSafe { get; set; } = false; // 산소 비정상 소모 중인지 여부

    public bool CanVerticalMove { get; set; } = true;
    public bool CanMove { get; set; } = true;

    public float CurrentSpeedMultiplier { get; private set; } = 1f;

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

    // 이벤트
    public event Action OnReachedFinalPatternDepth; // 최종 패턴 시작 수심에 도달했을 시



    #region 생명주기

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCamTransform = Camera.main.transform;

        // 입력 관련 변수 가져오기
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions.FindAction("DeepSea_Move");
        _lookAction = _playerInput.actions.FindAction("DeepSea_Look");
        _sprintAction = _playerInput.actions.FindAction("DeepSea/Sprint");
        _ascendAction = _playerInput.actions.FindAction("DeepSea/Ascend");
    }

    private void OnEnable()
    {
        // Shift 키 입력 이벤트 구독
        _sprintAction.performed += OnSprintActionPerformed;
        _sprintAction.canceled += OnSprintActionCanceled;
    }

    private void OnDisable()
    {
        // Shift 키 입력 이벤트 구독 해제
        _sprintAction.performed -= OnSprintActionPerformed;
        _sprintAction.canceled -= OnSprintActionCanceled;
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing) return;

        // 산소 소모
        if (IsSprinting)
        {
            ConsumeOxygen(sprintOxygenCostPerSec * Time.fixedDeltaTime);
        }
        else if (IsOxgenNonSafe)
        {
            ConsumeOxygen(abnormalOxygenCostPerSec * Time.fixedDeltaTime);
        }
        else
        {
            // 평소에도 지속해서 기본 산소 감소
            ConsumeOxygen(baseOxygenCostPerSec * Time.deltaTime);
        }

        // 괴물에게 잡힌 상태일 때 -> QTE 입력만 받음 & 이동 불가
        if (_isGrabbed)
        {
            HandleGrabQTE();
            return;
        }

        if (_isKnockedBack) return; // 넉백 중에는 회전 및 조작 입력 무시

        ReadInputs(); // 입력 가져오기
        Rotate(); // 좌우 회전
        UpdateAnimationAndCollider(); // 애니메이션 & 콜라이더 업데이트
        UpdateModelRotation(); // 모델 회전 업데이트
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        // 잡혔을 때 -> 괴물과 똑같은 속도로 아래로 이동
        if (_isGrabbed)
        {
            Vector3 nextPos = _rb.position + Vector3.down * dragDownSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(nextPos);
            return;
        }

        if (_isKnockedBack) return; // 넉백 중에는 Rigidbody 물리 이동 중단

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
        if (!CanMove)
            _moveInput = Vector2.zero;
        else
            _moveInput = _moveAction.ReadValue<Vector2>();

        // 상승/하강 현재 프레임에 눌려있는지 여부
        bool ascendNow = _ascendAction.IsPressed();

        // 수면 위인지 체크
        bool isAtSurface = transform.position.y >= deepSeaAreaController.SurfaceY;

        if (!CanVerticalMove || !CanMove || isAtSurface) // 상하 이동 불가 or 수면 위 -> 상하 이동 불가
        {
            _verticalInput = 0f;
        }
        else // 수면 아래
        {
            if (ascendNow)
            {
                _verticalInput = 1f;
            }
            else
            {
                _verticalInput = 0f;
            }
        }

        if (!CanMove)
        {
            MouseX = 0f;
            MouseY = 0f;
        }
        else
        {
            // 시야 입력 처리
            Vector2 lookInput = _lookAction.ReadValue<Vector2>();
            float sensitivity = GameManager.instance.MouseSensitivity;
            MouseX = lookInput.x * sensitivity;
            MouseY = lookInput.y * sensitivity;
        }
    }

    #endregion

    #region Shift - 회피/가속

    /// <summary>
    /// Tap(0.25초 전 뗌) 또는 Hold(0.25초 꾹 누름) 조건이 달성되었을 때 실행
    /// </summary>
    private void OnSprintActionPerformed(InputAction.CallbackContext context)
    {
        // Case 1: 0.25초가 되기 전에 손을 떼서 'Tap(회피)'이 발동한 경우
        if (context.interaction is UnityEngine.InputSystem.Interactions.TapInteraction)
        {
            // 수면에 도달하면 회피 불가 (기존 예외 처리 그대로 유지)
            if (deepSeaAreaController.CurrentZone == SeaZone.Surface)
            {
                Debug.Log("이미 수면에 도달했으므로 회피할 수 없습니다!");
                return;
            }

            TryExecuteDash(); // 회피 함수 실행
        }

        // Case 2: 손을 떼지 않고 0.25초를 채워서 'Hold(가속)'가 발동한 경우
        else if (context.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
        {
            _isSprintPressed = true; // 가속 이동 활성화 플래그 ON
            Debug.Log("0.25초 경과: 가속 이동 시작!");
        }
    }

    /// <summary>
    /// 유저가 키에서 손을 떼는 순간 실행
    /// </summary>
    private void OnSprintActionCanceled(InputAction.CallbackContext context)
    {
        // Hold(가속) 상태에서 손을 떼었거나, 애매한 타이밍(0.25초 경계선)에 떼어졌을 때 안전하게 가속 OFF
        _isSprintPressed = false;
        Debug.Log("Shift 키 입력 종료: 가속 이동 중지");
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
        // 3차원 이동 방향
        if (_verticalInput == 1f)
            _moveDir3D = (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized; // 트랜스폼 기준
        else
            _moveDir3D = (_mainCamTransform.forward * _moveInput.y + _mainCamTransform.right * _moveInput.x).normalized; // 카메라 정면 방향

        // 2차원 방향
        _moveDir2D = _moveDir3D;
        _moveDir2D.y = 0f;
        _moveDir2D = _moveDir2D.normalized;

        // 상승 중일 때 무조건 상승하게 만듦
        if (_verticalInput == 1f)
        {
            _moveDir3D.y = 1f;
            _moveDir3D = _moveDir3D.normalized;
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
            currentSpeed *= CurrentSpeedMultiplier;
            targetVelocity = _moveDir3D * currentSpeed;
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
        speedMultiplier *= CurrentSpeedMultiplier;
        animator.SetFloat(_swimSpeedHash, speedMultiplier);
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
        if (Mathf.Abs(_moveInput.x) < 0.1f)
        {
            if (_verticalInput == 1f)
            {
                if (_moveInput.y > 0.1f) // 앞으로 가고 있으면
                {
                    targetXRot = 30f;
                }
            }
            else if (_moveDir3D.sqrMagnitude > 0.01f)
            {
                // targetXRot = _mainCamTransform.eulerAngles.x;

                _xRotation -= MouseY;
                _xRotation = Mathf.Clamp(_xRotation, deepSeaCameraController.MinPitch, deepSeaCameraController.MaxPitch);
                targetXRot = _xRotation;
            }
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

    #region 붙잡힘 (탈출 QTE)

    /// <summary>
    /// 괴물이 플레이어를 붙잡았을 때 호출 -> 탈출 위한 QTE 시작
    /// </summary>
    public void StartGrabQTE()
    {
        _isGrabbed = true;
        _currentSpaceCount = 0;
        _qteTimer = 0f;

        dragDownUI.SetActive(true); // UI 활성화
        leftTimeImage.fillAmount = 1f;
        spaceCountText.text = "0";
    }

    /// <summary>
    /// 붙잡혔을 때 탈출하는 QTE 입력 받기
    /// </summary>
    private void HandleGrabQTE()
    {
        // 타이머 증가
        _qteTimer += Time.deltaTime;
        leftTimeImage.fillAmount = Mathf.Clamp01((_qteTimeLimit - _qteTimer) / _qteTimeLimit);

        // Space 키 입력 감지
        if (_ascendAction != null && _ascendAction.WasPressedThisFrame())
        {
            _currentSpaceCount++; // 눌렀으므로 카운트 증가
            spaceCountText.text = $"{_currentSpaceCount}";
            Debug.Log("스페이스 횟수: " + _currentSpaceCount + " / 남은 시간: " + (_qteTimeLimit - _qteTimer));

            // 제한 시간 내 목표 횟수 달성 -> 성공
            if (_currentSpaceCount >= _requiredSpaceCount)
            {
                _isGrabbed = false; // 붙잡힘 상태 해제
                deepSeaCameraController.SetInputEnabled(true);
                dragDownUI.SetActive(false); // UI 비활성화
                OnGrabQTESuccess?.Invoke(); // 괴물에게 QTE 성공 알림
                return;
            }
        }

        // 제한 시간 초과 -> 실패
        if (_qteTimer >= _qteTimeLimit)
        {
            deepSeaCameraController.SetInputEnabled(true);
            dragDownUI.SetActive(false); // UI 비활성화

            // 붙잡힘 상태 해제하지 않음
            OnGrabQTEFailed?.Invoke(); // 괴물에게 QTE 실패 알림
        }
    }

    #endregion

    #region 대미지 받았을 때

    public void TakeDamage(float amount)
    {
        Debug.Log("대미지 입음!");
        ConsumeOxygen(amount);
        DOTween.To(() => damagedVolume.weight, x => damagedVolume.weight = x, 1f, 0.5f)
            .OnComplete(() =>
            {
                DOTween.To(() => damagedVolume.weight, x => damagedVolume.weight = x, 0f, 0.5f);
            });
    }

    public void ApplyKnockback(Vector3 hitDirection, float distance = 2.5f, float duration = 0.25f)
    {
        _isKnockedBack = true;

        transform.DOKill();

        // 밀려날 목표 위치 계산
        Vector3 targetPos = transform.position + (hitDirection.normalized * distance);

        // DOTween을 사용한 수중 넉백 (OutCubic으로 밀리다가 스르륵 멈춤)
        transform.DOMove(targetPos, duration)
            .SetEase(Ease.OutBack, overshoot: 1.2f)
            .OnComplete(() => _isKnockedBack = false) // 넉백 종료 시 조작 복구
            .OnKill(() => _isKnockedBack = false);
    }

    public void TriggerHitStop(float duration)
    {
        StartCoroutine(HitStopCoroutine(duration));
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(duration); // Realtime 사용 필수
        Time.timeScale = 1.0f;
    }

    #endregion

    public void SetSpeedMultiplier(float value)
    {
        CurrentSpeedMultiplier = value;
    }
}