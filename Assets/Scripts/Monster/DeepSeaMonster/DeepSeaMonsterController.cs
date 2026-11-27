using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeepSeaMonsterStates;
using System;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;

/// <summary>
/// 심해 괴물 움직임 관리
/// </summary>
public class DeepSeaMonsterController : MonoBehaviour, IStateMachineOwner<DeepSeaMonsterController>
{
    private StateMachine<DeepSeaMonsterController> _fsm; // 상태 머신

    public UnderwaterPlayerMove underwaterPlayerMove;
    public UnderwaterInput underwaterInput;
    public UnderwaterPlayerStat underwaterPlayerStat;
    public UnderwaterCameraController underwaterCameraController;
    public ShockwaveTrigger shockwaveTrigger;

    [Header("끌어내리기 UI")]
    public GameObject dragDownUI;
    public Image leftTimeImage;
    public TextMeshProUGUI spaceCountText;
    public TextMeshProUGUI targetSpaceCountText;

    public Light monsterLight;

    public EnergyGunController energyGunController;

    public float AttackCoolDown { get; private set; } = 10f; // 공격 쿨타임
    public bool IsAmbush { get; set; } = false; // 현재 기습 공격인지 여부
    public bool IsGrabbing { get; set; } = false; // 현재 플레이어를 잡고 있는지(끌어내리기 상태) 여부
    public bool IsBehindPlayer { get; set; } = false; // 현재 플레이어 뒤에 있는지 여부

    // 최종 패턴
    public RectTransform topBlackScreen;    // 위쪽 검은 상단 막(UI)
    public RectTransform bottomBlackScreen; // 아래쪽 검은 하단 막(UI)
    public Volume finalPatternVolume; // 최종 패턴 볼륨
    private int _weakenCount = 0; // 현재 약화 수
    public bool IsWeakenFully => _weakenCount >= 3;
    public bool IsFinalPatternPending { get; private set; } = false; // 최종 패턴 예약 여부
    public bool IsExecutingFinalPattern { get; private set; } = false; // 최종 패턴 중 여부
    public int CurrentFinalPatternCount { get; set; } = 0; // 현재 몇 번째 패턴인가
    public int MaxFinalPatternCount { get; private set; } = 5;
    private bool _isFinalPatternClear = false; // 최종 패턴 클리어 여부

    public float shockwaveDuration = 1f;


    [SerializeField] private DeepSeaCameraController deepSeaCameraController;
    public DeepSeaCameraController CameraController => deepSeaCameraController;
    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;
    [SerializeField] private DeepSeaPlayerMove deepSeaPlayerMove;
    public DeepSeaPlayerMove DeepSeaPlayerMove => deepSeaPlayerMove;
    public Transform playerGrabPos;
    public LayerMask obstacleLayerMask;
    public GameObject monsterGeo; // 몬스터 외형 모습
    public Animator animator;
    [SerializeField] private HelicopterRotor helicopterRotor;

    [Header("오디오")]
    public AudioSource audioSource;
    public AudioSource heartBeatAudioSource;
    public AudioSource finalPatternAudioSource;
    public AudioClip spawnSound;
    public AudioClip dragDownAttackCloseSound;
    public AudioClip moveSound;
    public AudioClip shockwaveSound;
    public AudioClip bigAttackSound;
    public AudioClip behindPlayerSound; // 플레이어 뒤에 있을 때 나는 소리

    private Rigidbody _rb;
    public Rigidbody Rb => _rb;

    private float _fsmEndDepth = 100f; // FSM 종료 수심 (90f)

    public List<DeepSeaMonsterAttackBase> AllPatternList { get; private set; } = new List<DeepSeaMonsterAttackBase>();
    public DeepSeaMonsterAttackBase currentPattern = null;
    private Dictionary<DeepSeaMonsterAttackBase, int> _patternCounts = new Dictionary<DeepSeaMonsterAttackBase, int>();

    public bool IsEnded { get; private set; } = false; // FSM 종료 여부
    // public bool IsPlayerTriggered { get; private set; } = false;
    public float MonsterRadius { get; private set; } = 5f;
    public int TorpedoHitCount { get; private set; } = 0; // 어뢰 발사 맞은 횟수
    public float CurrentDamageScale { get; private set; } = 1f; // 현재 대미지 배율
    public float RespawnWaitingTime { get; private set; } = 15f; // 재스폰 대기 시간

    public bool IsFSMPause = false;

    public bool IsIntroDashShowed { get; set; } = false; // 컷씬 이후 이어지는 처음 대시 공격을 보여주었는지 여부

    // 피격
    private float _hp = 100f;
    public float Hp => _hp;

    private void Awake()
    {
        if (underwaterPlayerMove == null) return;

        _fsm = new StateMachine<DeepSeaMonsterController>(this);
        _rb = GetComponent<Rigidbody>();

        monsterLight.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (underwaterPlayerMove == null) return;

        shockwaveTrigger.gameObject.SetActive(false);

        if (IsFSMPause)
            StopFSM(); // FSM 정지
        else
            StartFSM(); // FSM 시작
    }

    /// <summary>
    /// FSM 시작
    /// </summary>
    public void StartFSM()
    {
        Debug.Log("괴물 FSM 시작");

        IsFSMPause = false; // 정지 여부 false로 변경

        monsterLight.gameObject.SetActive(true);

        // 패턴 비우고 추가
        AllPatternList.Clear();
        AllPatternList.Add(new SlamAttack(this));
        AllPatternList.Add(new SweepAttack(this));
        AllPatternList.Add(new DragDownAttack(this));

        // 현재 패턴들의 나온 횟수 0으로 초기화
        foreach (var pattern in AllPatternList)
        {
            _patternCounts[pattern] = 0;
        }

        monsterGeo.SetActive(true); // 외관 활성화
        _rb.isKinematic = false;

        // 능력 사용 게임이라면 -> 어뢰 발사 여부와 상관 없이 일단 대미지 반으로 감소
        if (GameManager.instance.IsAbilityActive)
        {
            CurrentDamageScale *= 0.5f;
        }

        // // 어뢰 맞은 횟수 설정 (로드)
        // SetTorpedoHitCountAndApplyEffect(3);

        // 최종 패턴 테스트용
        Weaken();
        Weaken();
        Weaken();

        // 스폰 상태
        _fsm.ChangeState(new StealthMoveState());
    }

    /// <summary>
    /// FSM 정지
    /// </summary>
    public void StopFSM()
    {
        IsFSMPause = true; // 정지 여부 true로 변경

        _fsm.ExitState(); // 상태 종료

        monsterGeo.SetActive(false); // 외관 비활성화
        _rb.isKinematic = true;
    }

    private void Update()
    {
        if (underwaterPlayerMove == null || IsFSMPause) return;
        if (GameManager.instance.IsPausing) return;
        if (_isFinalPatternClear) return;

        if (underwaterPlayerMove.DisplayDepth <= _fsmEndDepth && !IsFinalPatternPending && !IsExecutingFinalPattern)
        {
            IsFinalPatternPending = true;
        }

        _fsm.Update();
    }

    private void FixedUpdate()
    {
        if (underwaterPlayerMove == null || IsFSMPause) return;
        if (GameManager.instance.IsPausing) return;
        if (_isFinalPatternClear) return;

        _fsm.FixedUpdate();

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    private void OnDisable()
    {
        if (underwaterPlayerMove == null || IsFSMPause) return;

        _fsm.ExitState(); // 비활성화(파괴 직전)될 때 -> 무조건 상태 종료

        if (finalPatternAudioSource != null && finalPatternAudioSource.isPlaying)
            finalPatternAudioSource.Stop();
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (deepSeaPlayerMove == null || IsFSMPause) return;

    //     if (!IsPlayerTriggered && other.gameObject.CompareTag("Player"))
    //     {
    //         IsPlayerTriggered = true;
    //         Debug.Log("플레이어 트리거 발생");
    //     }
    // }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (deepSeaPlayerMove == null || IsFSMPause) return;

    //     if (IsPlayerTriggered && other.gameObject.CompareTag("Player"))
    //     {
    //         IsPlayerTriggered = false;
    //         Debug.Log("플레이어 트리거 발생 X");
    //     }
    // }

    // 상태 전환
    public void ChangeState(IState<DeepSeaMonsterController> newState)
    {
        _fsm.ChangeState(newState); // 상태 변경
    }

    /// <summary>
    /// FSM 종료
    /// </summary>
    public void TerminateFSM()
    {
        _fsm.ExitState();
        monsterGeo.SetActive(false); // 외형 비활성화
        enabled = false; // 현재 컨트롤러 컴포넌트 자체를 비활성화
    }

    public int GetPatternCount(DeepSeaMonsterAttackBase pattern)
    {
        return _patternCounts.ContainsKey(pattern) ? _patternCounts[pattern] : 0;
    }

    public void IncreasePatternCount(DeepSeaMonsterAttackBase pattern)
    {
        if (_patternCounts.ContainsKey(pattern))
        {
            _patternCounts[pattern]++;
        }
    }

    /// <summary>
    /// 어뢰 맞았을 시
    /// </summary>
    public void OnTorpedoHit()
    {
        TorpedoHitCount++;
        Debug.Log("심해 괴물이 어뢰에 맞음! 현재 맞은 횟수: " + TorpedoHitCount);
    }

    /// <summary>
    /// 어뢰 발사 개수 설정 및 효과 적용 (본씬 데이터 로드 및 세이브 파일 불러올 시 사용)
    /// </summary>
    public void SetTorpedoHitCountAndApplyEffect(int count)
    {
        TorpedoHitCount = count;

        if (TorpedoHitCount >= 1)
        {
            CurrentDamageScale *= 0.5f; // 대미지 1/2배
        }

        if (TorpedoHitCount >= 2)
        {
            AttackCoolDown = 15f; // 일반 공격 쿨타임 5초 증가 -> 괴물 출현 횟수 감소
        }
        if (TorpedoHitCount == 3)
        {
            deepSeaPlayerMove.TargetSpaceCount = 5; // 끌어내리기 탈출 스페이스 횟수 10 -> 5로 감소
            shockwaveDuration = 1.5f; // 충격파 1.5배 느리게

            // // 최종 패턴 슬로우 배속 0.04f 줄여줌
            // FinalPatternManager finalPatternManager = FindAnyObjectByType<FinalPatternManager>();
            // finalPatternManager.FirstSlowTimeScale -= 0.04f; // 0.08f -> 0.04f
            // finalPatternManager.FinalSlowTimeScale -= 0.04f; // 0.16f -> 0.12f
        }
    }

    // 애니메이션 이벤트 1 (공격 내지르는 순간)
    public void OnAttackStart()
    {
        // hitBox.EnableHitbox(
        //     onSuccess: () => currentPattern.OnAttackSuccess(), // 적중 시
        //     onMiss: () => currentPattern.OnAttackMiss()       // 빗나갔을 때
        // );
    }

    // 애니메이션 이벤트 2 (공격 휘두르기가 끝나는 순간)
    public void OnAttackEnd()
    {
        // hitBox.DisableHitbox();
    }

    /// <summary>
    /// 애니메이션 이벤트 - 공격 타이밍
    /// </summary>
    public void OnAttack()
    {
        Debug.Log(currentPattern + " - 애니메이션 공격 타이밍");
        if (currentPattern != null)
        {
            currentPattern.OnAnimationAttack();
        }
    }

    /// <summary>
    /// 플레이어에게 대미지 (산소 감소)
    /// </summary>
    public void DamagePlayer(float amount)
    {
        underwaterPlayerStat.TakeDamage(amount * CurrentDamageScale); // 현재 대미지 배율 적용하여 대미지 줌
        underwaterPlayerStat.TriggerHitStop(0.2f); // 히트 스탑 적용
    }

    #region Attacked

    /// <summary>
    /// 대미지 받기 (3번 맞으면 죽음)
    /// </summary>
    public void TakeDamage(float amount)
    {
        _hp -= amount;
        Debug.Log("괴물 체력: " + _hp);
        if (_hp <= 0)
        {
            Die();
        }
        else
        {
            ChangeState(new RetreatState()); // 후퇴 상태로 전환
        }
    }

    /// <summary>
    /// 죽기
    /// </summary>
    public void Die()
    {
        Debug.Log("심해 괴물 사망");
        _hp = 0;
        if (currentPattern != null)
            currentPattern.OnAttackMiss();
        ChangeState(new DeadState()); // 죽은 상태로 전환
    }

    #endregion

    /// <summary>
    /// 충격파 날리기
    /// </summary>
    public void LaunchShockwave(Quaternion rotation, Action onAttackSuccess = null, Action onAttackMiss = null)
    {
        shockwaveTrigger.Fire(underwaterPlayerMove.BasePosition + Vector3.up * underwaterPlayerMove.MoveAscentSpeed / 2f, rotation, shockwaveDuration, onAttackSuccess, onAttackMiss
        );
    }

    /// <summary>
    /// 약화 시
    /// </summary>
    public void Weaken()
    {
        _weakenCount++;
        Debug.Log($"괴물 {_weakenCount}차 약화!");

        switch (_weakenCount)
        {
            case 1:
                monsterLight.color = Color.yellow; // 노랑
                ChangeState(new RetreatState()); // 후퇴
                break;
            case 2:
                monsterLight.color = new Color(1f, 0.5f, 0f); // 주황
                ChangeState(new RetreatState()); // 후퇴
                break;
            case 3:
                monsterLight.color = Color.red; // 빨강
                StartFinalPattern(); // 최종 패턴 시작
                break;
        }
    }

    #region 최종 패턴

    /// <summary>
    /// 최종 패턴 시작
    /// </summary>
    public void StartFinalPattern()
    {
        if (IsExecutingFinalPattern) return;

        IsExecutingFinalPattern = true;
        IsFinalPatternPending = false;

        AttackCoolDown = 2f; // 공격 쿨타임 2초

        finalPatternAudioSource.Play();
        DOTween.To(() => finalPatternVolume.weight, x => finalPatternVolume.weight = x, 1f, 1f);
        underwaterCameraController.cameraLight.color = Color.red; // 카메라 손전등 빨간 빛으로 변경

        // 최종 패턴에서 나올 일반 공격만 일반 공격 패턴 리스트에 넣음 (내려치기, 휩쓸기)
        AllPatternList.Clear();
        AllPatternList.Add(new SlamAttack(this));
        AllPatternList.Add(new SweepAttack(this));

        // 은신 이동 상태로 이동
        ChangeState(new StealthMoveState());
    }

    /// <summary>
    /// 최종 패턴 클리어
    /// </summary>
    public void ClearFinalPattern()
    {
        Debug.Log("<color=cyan>★ 최종 패턴 클리어!</color>");

        IsExecutingFinalPattern = false;
        _isFinalPatternClear = true;

        underwaterPlayerStat.IsSurvivalUpdateStopped = true; // 체온 감소 중단
        helicopterRotor.IsMainRotorActive = true; // 헬기 로터 회전 시작
        DeepSeaUIManager.Instance.DisableSurvialWarning(); // 체온 경고 UI 끄기

        DOTween.To(() => finalPatternVolume.weight, x => finalPatternVolume.weight = x, 0f, 1f); // 볼륨 되돌리기
        DOTween.To(() => finalPatternAudioSource.volume, x => finalPatternAudioSource.volume = x, 0f, 2f); // 오디오 페이드 아웃

        deepSeaCameraController.cameraLight.color = Color.white; // 카메라 손전등 하얀 빛으로 되돌리기

        if (GameManager.instance.IsAbilityActive)
            ChangeState(new HiddenState());
        else
            ChangeState(new DeadState());
    }

    /// <summary>
    /// 실패 시 게임 오버 연출 코루틴 -> 먹히면서 눈 감기고 게임 오버
    /// </summary>
    public IEnumerator GameOverSequenceCoroutine()
    {
        energyGunController.ExitAimMode();

        Time.timeScale = 0.05f; // 시간 느리게 하기
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(0.1f); // 입이 닫히는 아주 짧은 타이밍 대기

        // 연출 시작
        Sequence gameOverSequence = DOTween.Sequence();
        gameOverSequence.SetUpdate(true); // timeScale 영향을 받지 않도록 unscaled time 설정

        // 눈 감기는 듯한 UI 연출
        gameOverSequence.Append(topBlackScreen.DOAnchorPosY(-540f, 0.15f).SetEase(Ease.OutQuad));
        gameOverSequence.Join(bottomBlackScreen.DOAnchorPosY(540f, 0.15f).SetEase(Ease.OutQuad));

        // 오디오 페이드 아웃
        gameOverSequence.Join(DOTween.To(() => finalPatternAudioSource.volume, x => finalPatternAudioSource.volume = x, 0f, 1f));

        // 해당 연출이 완전히 끝날 때까지 코루틴 대기
        yield return gameOverSequence.WaitForCompletion();

        // 슬로우 해제 (남은 몇 프레임동안 정상 속도로 입이 콱 닫힘)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        yield return new WaitForSeconds(1f); // 1초 후

        // 게임 오버
        Debug.Log("게임 오버! 플레이어가 괴물에게 삼켜졌습니다.");
        GameManager.instance.GameOver(EEndingType.MonsterDeath);
        yield break;
    }

    /// <summary>
    /// 삼키기 공격 중 약점 맞았을 때 -> 넉백
    /// </summary>
    public void OnSwallowWeakPointHit()
    {
        Vector3 startPos = Rb.position;
        animator.Play("SwallowAttack", 0, 0f); // 애니메이션 다시 시작
        Sequence seq = DOTween.Sequence();
        seq.Append(Rb.DOMove(Rb.position - transform.forward * 5f, 1.5f).SetEase(Ease.OutExpo));
        seq.Append(Rb.DOMove(startPos, 1.5f));
    }
    #endregion
}
