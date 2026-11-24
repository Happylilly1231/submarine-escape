using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UnderwaterPlayerMove : PlayerMoveBase
{
    [Header("참조")]
    [SerializeField] private Transform modelTransform; // 실제 3D 모델의 트랜스폼
    [SerializeField] private CapsuleCollider capsuleCollider; // 콜라이더 (모델)
    public CapsuleCollider modelCollider => capsuleCollider;
    [SerializeField] private UnderwaterCameraController underwaterCameraController;
    [SerializeField] private DeepSeaAreaController deepSeaAreaController;

    [Header("오디오")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip suitMotorSound;

    [Header("산소")]
    [SerializeField] private float fastMovingOxygenCost = 6f; // 빠른 이동 시 산소 소모량

    // 참조
    private UnderwaterInput _underwaterInput;
    private UnderwaterPlayerStat _underwaterPlayerStat;
    private Rigidbody _rb;

    // 기본 이동
    private float _moveAscentSpeed = 2f;
    public float MoveAscentSpeed => _moveAscentSpeed;
    public Vector3 BasePosition { get; private set; }

    // 좌우 회피
    public bool IsDodging { get; private set; } = false;
    private float _dodgeDistance = 5f;

    // 위로 대시
    public bool IsUpwardDashing { get; private set; } = false;
    private float _dashDistance = 5f;

    // 넉백
    public bool IsKnockedBack { get; private set; } // 넉백 중 여부
    private Sequence _actionSequence; // 회피, 대시, 넉백 시퀀스

    // 애니메이션
    private readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

    public bool IsActionLocked => IsDodging || IsUpwardDashing || IsKnockedBack; // 플레이어 액션 락된 상태 변수

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

    #region 생명주기

    private void Awake()
    {
        _underwaterInput = GetComponent<UnderwaterInput>();
        _underwaterPlayerStat = GetComponent<UnderwaterPlayerStat>();
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (GameManager.instance.IsAbilityActive)
            _moveAscentSpeed = 4f;

        enabled = false; // 임시
    }

    private void OnEnable()
    {
        BasePosition = _rb.position;

        UnsubscriveInputEvents();
        SubscriveInputEvents();
    }

    private void OnDisable()
    {
        UnsubscriveInputEvents();
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing || !CanMove) return;

        // 스탯 업데이트
        if (!GameManager.instance.IsAbilityActive) // 능력이 없는 일반 게임일 때
        {
            if (!_underwaterPlayerStat.IsSurvivalUpdateStopped) // 체온, 산소 업데이트 중단 상태가 아닐 때만
            {
                if (!_underwaterInput.IsStopping)
                {
                    AudioManager.Instance.PlaySoundSafe(audioSource, suitMotorSound);
                }
                _underwaterPlayerStat.ConsumeBaseOxgen(); // 기본적으로 초당 산소 소모
                _underwaterPlayerStat.UpdateTemperature(); // 체온 저하
            }
        }
        else // 능력 사용 게임일 때
        {
            // 정신력 업데이트
            PlayerManager.Instance.playerStat.UpdateSanityInDeapSea(_underwaterPlayerStat.SurvivalTime);
        }

        // 생존 경고 상태 전달
        DeepSeaUIManager.Instance.UpdateSurvivalWarning(_underwaterPlayerStat.IsOxygenLow, _underwaterPlayerStat.IsTemperatureLow);

        // 수영 애니메이션 속도 결정
        float speedMultiplier = 1f;
        if (IsDodging || IsUpwardDashing)
            speedMultiplier = 2f;
        else if (_underwaterInput.IsStopping)
            speedMultiplier = 0.3f;
        animator.SetFloat(_swimSpeedHash, speedMultiplier);

        if (IsDodging || IsUpwardDashing || IsKnockedBack) return;
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

    #endregion

    #region 기본 함수들

    public override void SetMoveable(bool isMoveable)
    {
        base.SetMoveable(isMoveable);

        if (isMoveable)
        {
            BasePosition = _rb.position;

            UnsubscriveInputEvents();
            SubscriveInputEvents();
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();

            UnsubscriveInputEvents();
        }
    }

    public void SubscriveInputEvents()
    {
        if (_underwaterInput == null)
        {
            Debug.Log("입력 컴포넌트 참조 필요");
            return;
        }

        _underwaterInput.horizontalDodgeEvent += OnHorizontalDodge;
        _underwaterInput.upwardDashEvent += OnUpwardDash;
        // _underwaterInput.glanceBackEvent += OnGlanceBack;
        // _underwaterInput.glanceBackCancelEvent += FinishGlanceBack;
    }

    public void UnsubscriveInputEvents()
    {
        if (_underwaterInput == null)
        {
            Debug.Log("입력 컴포넌트 참조 필요");
            return;
        }

        _underwaterInput.horizontalDodgeEvent -= OnHorizontalDodge;
        _underwaterInput.upwardDashEvent -= OnUpwardDash;
        // _underwaterInput.glanceBackEvent -= OnGlanceBack;
        // _underwaterInput.glanceBackCancelEvent -= FinishGlanceBack;
    }

    #endregion

    #region 좌우 회피

    /// <summary>
    /// 좌우로 회피
    /// </summary>
    /// <param name="dir">-1: 좌 / 1: 우</param>
    private void OnHorizontalDodge(float dir)
    {
        if (IsDodging || IsUpwardDashing || IsKnockedBack) return;

        if (!GameManager.instance.IsAbilityActive)
        {
            if (_underwaterPlayerStat.CurrentOxygen < fastMovingOxygenCost)
            {
                Debug.Log("회피에 필요한 산소가 부족합니다! 회피 실패!");
                return;
            }

            _underwaterPlayerStat.ConsumeOxygen(fastMovingOxygenCost); // 산소 소모
        }

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

    #endregion

    #region 위로 대시

    /// <summary>
    /// 위로 대시
    /// </summary>
    private void OnUpwardDash()
    {
        if (IsUpwardDashing || IsDodging || IsKnockedBack) return;

        if (!GameManager.instance.IsAbilityActive)
        {
            if (_underwaterPlayerStat.CurrentOxygen < fastMovingOxygenCost)
            {
                Debug.Log("대시에 필요한 산소가 부족합니다! 대시 실패!");
                return;
            }
            _underwaterPlayerStat.ConsumeOxygen(fastMovingOxygenCost); // 산소 소모
        }

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

    #endregion

    #region 넉백

    /// <summary>
    /// 넉백 (회피/대시 중이었다면 강제 캔슬 후 실행)
    /// </summary>
    /// <param name="distance">밀려날 거리</param>
    /// <param name="duration">넉백 지속 시간</param>
    public void ApplyKnockback(bool isBehindAttack = false, float distance = 20f, float knockbackDuration = 0.25f, float holdDuration = 0.1f, float returnDuration = 0.3f)
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
        Vector3 knockbackDir = isBehindAttack ? transform.forward : -transform.forward; // 뒤 공격일 때 -> 앞으로 넉백
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

    #endregion
}
