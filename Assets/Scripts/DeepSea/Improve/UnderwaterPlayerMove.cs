using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UnderwaterPlayerMove : PlayerMoveBase
{
    [SerializeField] private Transform modelTransform; // 실제 3D 모델의 트랜스폼
    [SerializeField] private UnderwaterCameraController underwaterCameraController;
    [SerializeField] private DeepSeaAreaController deepSeaAreaController;

    [SerializeField] private Transform defaultCameraPos;
    [SerializeField] private Transform fastMovingCameraPos;

    [Header("오디오")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip suitMotorSound;

    [Header("산소")]
    [SerializeField] private float baseOxygenCostPerSec = 0.1f; // 기본 초당 산소 소모량
    [SerializeField] private float fastMovingOxygenCost = 6f; // 빠른 이동 시 산소 소모량

    // 참조
    private UnderwaterInput _underwaterInput;
    private UnderwaterPlayerStat _underwaterPlayerStat;
    private Rigidbody _rb;

    // 기본 이동
    private float _moveAscentSpeed = 2f;

    // 원점
    public Vector3 BasePosition { get; private set; }

    // 좌우 회피
    public bool IsDodging { get; private set; } = false;
    private float _dodgeDistance = 5f;

    // 위로 대시
    public bool IsUpwardDashing { get; private set; } = false;
    private float _dashDistance = 5f;

    // 잠깐 뒤돌아보기
    public bool IsGlancingBack { get; private set; } = false;
    private float _targetY;
    public bool _isGlanceBackStarted = false;
    public bool _isRotationEnded = false;
    private Coroutine _glanceTimerCoroutine;

    // 넉백
    public bool IsKnockedBack { get; private set; } // 넉백 중 여부
    private Sequence _actionSequence; // 회피, 대시, 넉백 시퀀스

    // 애니메이션
    private readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

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

    private void Awake()
    {
        _underwaterInput = GetComponent<UnderwaterInput>();
        _underwaterPlayerStat = GetComponent<UnderwaterPlayerStat>();
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        enabled = false; // 임시
    }

    private void OnEnable()
    {
        BasePosition = _rb.position;

        _underwaterInput.horizontalDodgeEvent -= OnHorizontalDodge;
        _underwaterInput.upwardDashEvent -= OnUpwardDash;
        _underwaterInput.glanceBackEvent -= OnGlanceBack;
        _underwaterInput.glanceBackCancelEvent -= FinishGlanceBack;
        _underwaterInput.horizontalDodgeEvent += OnHorizontalDodge;
        _underwaterInput.upwardDashEvent += OnUpwardDash;
        _underwaterInput.glanceBackEvent += OnGlanceBack;
        _underwaterInput.glanceBackCancelEvent += FinishGlanceBack;
    }

    private void OnDisable()
    {
        if (_underwaterInput != null)
        {
            _underwaterInput.horizontalDodgeEvent -= OnHorizontalDodge;
            _underwaterInput.upwardDashEvent -= OnUpwardDash;
            _underwaterInput.glanceBackEvent -= OnGlanceBack;
            _underwaterInput.glanceBackCancelEvent -= FinishGlanceBack;
        }
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing || !CanMove) return;

        if (!_underwaterInput.IsStopping)
        {
            AudioManager.Instance.PlaySoundSafe(audioSource, suitMotorSound);
            _underwaterPlayerStat.ConsumeBaseOxgen(); // 기본적으로 초당 산소 소모
        }
        _underwaterPlayerStat.UpdateTemperature(); // 체온 저하

        // 수영 애니메이션 속도 결정
        float speedMultiplier = 1f;
        if (IsDodging || IsUpwardDashing)
            speedMultiplier = 2f;
        else if (_underwaterInput.IsStopping)
            speedMultiplier = 0.3f;
        animator.SetFloat(_swimSpeedHash, speedMultiplier);

        if (IsDodging || IsUpwardDashing || IsKnockedBack) return;

        // 잠깐 뒤돌아볼 때
        if (IsGlancingBack)
        {
            float currentYaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, _targetY, 180f * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

            if (!_isRotationEnded && Mathf.Approximately(currentYaw, _targetY))
            {
                _isRotationEnded = true;
                transform.rotation = Quaternion.Euler(0f, _targetY, 0f);

                if (_isGlanceBackStarted)
                {
                    _isGlanceBackStarted = false;
                    if (_glanceTimerCoroutine == null)
                        _glanceTimerCoroutine = StartCoroutine(GlanceBackTimerRoutine());
                }
                else
                {
                    IsGlancingBack = false;
                    Debug.Log("잠깐 뒤돌아보기 끝");
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.IsPausing || !CanMove) return;
        if (IsDodging || IsUpwardDashing || IsKnockedBack) return;

        float verticalDir = _underwaterInput.IsStopping ? 0f : 1f;
        Vector3 verticalMovement = Vector3.up * verticalDir * _moveAscentSpeed * Time.fixedDeltaTime; // 수직 이동

        BasePosition += verticalMovement;

        // 움직임 적용
        _rb.MovePosition(_rb.position + verticalMovement);
    }

    public override void SetMoveable(bool isMoveable)
    {
        base.SetMoveable(isMoveable);

        if (isMoveable)
        {
            BasePosition = _rb.position;

            _underwaterInput.horizontalDodgeEvent -= OnHorizontalDodge;
            _underwaterInput.upwardDashEvent -= OnUpwardDash;
            _underwaterInput.glanceBackEvent -= OnGlanceBack;
            _underwaterInput.glanceBackCancelEvent -= FinishGlanceBack;
            _underwaterInput.horizontalDodgeEvent += OnHorizontalDodge;
            _underwaterInput.upwardDashEvent += OnUpwardDash;
            _underwaterInput.glanceBackEvent += OnGlanceBack;
            _underwaterInput.glanceBackCancelEvent += FinishGlanceBack;
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();

            _underwaterInput.horizontalDodgeEvent -= OnHorizontalDodge;
            _underwaterInput.upwardDashEvent -= OnUpwardDash;
            _underwaterInput.glanceBackEvent -= OnGlanceBack;
            _underwaterInput.glanceBackCancelEvent -= FinishGlanceBack;
        }
    }

    #region 입력 처리

    /// <summary>
    /// 좌우로 회피
    /// </summary>
    /// <param name="dir">-1: 좌 / 1: 우</param>
    private void OnHorizontalDodge(float dir)
    {
        if (IsDodging || IsUpwardDashing || IsKnockedBack) return;

        if (_underwaterPlayerStat.CurrentOxygen < fastMovingOxygenCost)
        {
            Debug.Log("회피에 필요한 산소가 부족합니다! 회피 실패!");
            return;
        }
        _underwaterPlayerStat.ConsumeOxygen(fastMovingOxygenCost); // 산소 소모

        IsDodging = true;
        Debug.Log(dir + " 방향 회피");
        BasePosition = _rb.position;

        underwaterCameraController.PlayActionLook(0f, -dir * 35f, 0.5f, 0.15f, 0.25f); // 반대 방향 바라보기

        _actionSequence?.Kill();
        _actionSequence = DOTween.Sequence();
        _actionSequence.Append(_rb.DOMove(BasePosition + transform.right * dir * _dodgeDistance, 0.5f).SetEase(Ease.OutCubic));
        _actionSequence.AppendInterval(0.15f);
        _actionSequence.Append(_rb.DOMove(BasePosition, 0.25f).SetEase(Ease.InOutQuad));
        _actionSequence.OnComplete(() =>
        {
            IsDodging = false;
        });
    }

    /// <summary>
    /// 위로 대시
    /// </summary>
    private void OnUpwardDash()
    {
        if (IsUpwardDashing || IsDodging || IsKnockedBack) return;

        if (_underwaterPlayerStat.CurrentOxygen < fastMovingOxygenCost)
        {
            Debug.Log("대시에 필요한 산소가 부족합니다! 대시 실패!");
            return;
        }
        _underwaterPlayerStat.ConsumeOxygen(fastMovingOxygenCost); // 산소 소모

        IsUpwardDashing = true;
        Debug.Log("위로 대시");

        underwaterCameraController.PlayActionLook(45f, 0f, 0.5f, 0.15f, 0.25f); // 반대 방향 바라보기

        _actionSequence?.Kill();
        _actionSequence = DOTween.Sequence();
        _actionSequence.Append(_rb.DOMove(Vector3.up * _dashDistance, 0.5f).SetRelative().SetEase(Ease.OutCubic));
        _actionSequence.AppendInterval(0.15f);
        _actionSequence.Append(_rb.DOMove(Vector3.down * (_dashDistance / 2), 0.25f).SetRelative().SetEase(Ease.InOutQuad));
        _actionSequence.OnComplete(() =>
        {
            IsUpwardDashing = false;
            BasePosition = _rb.position;
        });
    }

    /// <summary>
    /// 잠깐 뒤돌아보기
    /// </summary>
    private void OnGlanceBack()
    {
        if (IsGlancingBack) return;

        Debug.Log("잠깐 뒤돌아보기 시작");

        IsGlancingBack = true;
        _isGlanceBackStarted = true;
        _targetY = transform.eulerAngles.y + 180f;
        _isRotationEnded = false;
    }

    /// <summary>
    /// 잠깐 뒤돌아보기 종료
    /// </summary>
    private void FinishGlanceBack()
    {
        if (!IsGlancingBack) return;

        Debug.Log("정면으로 돌기 시작");

        if (_glanceTimerCoroutine != null)
        {
            StopCoroutine(_glanceTimerCoroutine);
            _glanceTimerCoroutine = null;
        }

        _targetY = _targetY - 180f;
        _isGlanceBackStarted = false;
        _isRotationEnded = false;
    }

    /// <summary>
    /// 잠깐 뒤돌아보기 타이머 코루틴 (3초 넘으면 뒤돌아보기 자동 종료)
    /// </summary>
    /// <returns></returns>
    private IEnumerator GlanceBackTimerRoutine()
    {
        yield return new WaitForSeconds(3f);
        _glanceTimerCoroutine = null;
        FinishGlanceBack();
    }

    #endregion

    /// <summary>
    /// 넉백 (회피/대시 중이었다면 강제 캔슬 후 실행)
    /// </summary>
    /// <param name="distance">밀려날 거리</param>
    /// <param name="duration">넉백 지속 시간</param>
    public void ApplyKnockback(float distance = 20f, float knockbackDuration = 0.25f, float holdDuration = 0.1f, float returnDuration = 0.3f)
    {
        // 진행 중이던 액션 시퀀스 종료
        _actionSequence?.Kill();

        // 플래그 초기화
        IsDodging = false;
        IsUpwardDashing = false;
        IsKnockedBack = true;

        // 회피/대시로 돌아가 있던 카메라 시선이 있다면 정면으로 복구
        underwaterCameraController.SetInputEnabled(true);

        // 플레이어 기준 뒤쪽 방향 계산
        Vector3 knockbackDir = -transform.forward;
        knockbackDir.y = 0f;
        knockbackDir.Normalize();

        // 목표 위치 계산
        Vector3 targetPos = _rb.position + knockbackDir * distance;

        // 넉백 시퀀스 실행
        _actionSequence = DOTween.Sequence();
        _actionSequence.Append(_rb.DOMove(targetPos, knockbackDuration).SetEase(Ease.OutCubic));
        _actionSequence.AppendInterval(holdDuration);
        _actionSequence.Append(_rb.DOMove(BasePosition, returnDuration).SetEase(Ease.InOutQuad));
        _actionSequence.OnComplete(() =>
        {
            IsKnockedBack = false;
            BasePosition = _rb.position;
        });
    }
}
