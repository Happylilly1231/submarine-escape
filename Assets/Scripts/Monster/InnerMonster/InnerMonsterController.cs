using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

using InnerMonsterStates;
using System;

public enum EAttackType
{
    HitAttack,
    DoubleClawAttack,
    JumpAttack,
    ThrowAttack,
    RageDestroyAttack,
    RageAttack
}

public enum EDestroyObjType
{
    None = 0,
    Door,
    EscapeRoomDoor,
    CurrentDestroyEquipment
}

/// <summary>
/// 내부 괴물 움직임 관리
/// </summary>
public class InnerMonsterController : MonoBehaviour, IStateMachineOwner<InnerMonsterController>
{
    // 상태 머신
    private StateMachine<InnerMonsterController> _fsm; // 기본 상태 머신
    private StateMachine<InnerMonsterController> _rageFsm; // 폭주 상태 머신
    private StateMachine<InnerMonsterController> _currentFsm; // 현재 상태 머신

    // 컴포넌트, 필요 변수
    private Transform playerTransform; // 플레이어 트랜스폼
    public Transform PlayerTransform => playerTransform;
    [SerializeField] private Transform monsterHeadTransform; // 머리 위치
    [SerializeField] private Transform throwObjPos; // 투사체 위치
    [SerializeField] private GameObject throwObj; // 투사체(던지기 공격에서 사용)
    public Renderer monsterEyeRenderer; // 눈 렌더러
    public Material originalEyeMaterial; // 원래 눈 머티리얼
    public Material redEyeMaterial; // 빨간 눈 머티리얼
    private Animator _animator; // 애니메이터
    public Animator Animator { get => _animator; set => _animator = value; }
    private NavMeshAgent _nav; // NavMeshAgent
    public NavMeshAgent Nav { get => _nav; set => _nav = value; }
    private CapsuleCollider _collider; // 콜라이더


    // 플레이어 관련
    private PlayerStat _playerStat; // 플레이어 스탯
    private PlayerStatus _playerStatus; // 플레이어 상태
    private float _distToPlayer; // 플레이어와의 거리(순찰/추적/공격 상태 결정 기준)
    public float DistToPlayer => _distToPlayer;

    // 범위 관련
    private float _fov = 150f;
    private Transform _fovCenterTransform; // 시야각 중심 위치
    private float _detectDistance = 20f; // 감지 거리
    private float _rangeAttackDistance = 5f; // 원거리 공격 거리
    public float RangeAttackDistance => _rangeAttackDistance;
    private float _meleeAttackDistance = 2.5f; // 근접 공격 거리
    public float MeleeAttackDistance => _meleeAttackDistance;
    private float _rageAttackDistance = 3f; // 폭주 공격 거리
    public float RageAttackDistance => _rageAttackDistance;

    // 순찰
    public Transform wayPointsParent; // 웨이포인트 부모 오브젝트
    private Transform[] _wayPoints; // 웨이포인트 배열
    public Transform[] WayPoints => _wayPoints;
    public int currentIndex = 0; // 현재 웨이포인트 인덱스(순찰 중일 때는 도착 전까지 현재 목적지를 가리킴)
    private bool _isNoWaypointCanGo = false; // 갈 수 있는 웨이포인트가 없는지 변수
    public bool IsNoWaypointCanGo { get => _isNoWaypointCanGo; set => _isNoWaypointCanGo = value; }

    // 추적
    private bool _isChaseEndDelay = false; // 추적 종료 딜레이(추적 중에 플레이어가 감지 범위를 벗어났을 때 일정 시간 동안은 추적함) 여부
    public bool IsChaseEndDelay => _isChaseEndDelay;
    private float _chaseEndDelayTime = 3f;
    public float _chaseEndDelayTimer = 0f;

    // 공격
    public EAttackType currentAttackType;
    private float attackCooldownTime = 2f;
    private bool _isAttackCoolDown = false;
    private float _attackCoolDownTimer = 0f;
    private bool _isJumping = false; // 점프 애니메이션에서 실제 점프 중(점프 애니메이션 실행 중 X)
    public bool IsJumping => _isJumping;
    private bool _isGrabbing = false; // 투사체 잡고 있는 중인지 여부
    private float _throwPower = 15f; // 던지기 공격에서 던지는 힘
    private float _attackForce = 1f; // 공격력
    private bool _canRangeAttack = true; // 원거리 공격 가능 여부
    public bool CanRangeAttack => _canRangeAttack;

    // 폭주
    private bool _isRageStartEnd = false;
    public bool IsRageStartEnd { get => _isRageStartEnd; set => _isRageStartEnd = value; }
    public EDestroyObjType currentDestroyObjType;
    public GameObject currentDestroyObj = null; // 현재 파괴해야 할 오브젝트

    // 휘청임
    private bool _isStaggering; // 현재 휘청임 중인지 여부
    public bool IsStaggering { get => _isStaggering; set => _isStaggering = value; }

    // 기타 변수
    private bool _isLookingAroundAfterAction = false;
    public bool IsLookingAroundAfterAction => _isLookingAroundAfterAction;

    // 이벤트
    public static event Action<InnerMonsterController> OnRageStartAnimationEnded; // 폭주 시작 애니메이션 종료 이벤트

    // 사운드
    [Header("Sound")]
    public AudioClip idleGrowlSound;
    public AudioClip patrolSound;
    public AudioClip detectSound;
    public AudioClip chaseSound;
    public AudioClip lookAroundGrowlSound;
    public AudioClip[] attackSounds;
    public AudioClip jumpLandingSound;
    public AudioClip staggerSound;
    public AudioClip rageStartSound;
    public AudioClip rageChaseSound;
    public AudioClip rageAttackSound;
    public AudioClip destroyRageSound;
    public AudioClip destroyingSound;
    public AudioClip destroyCompleteSound;
    public AudioSource audioSource;

    private void OnEnable()
    {
        // 이벤트 구독
        LightingManager.instance.OnLightChanged += ChangeStatValue; // 전등 상태 변경 -> 몬스터 스탯 수치 변경
        SubmarineInGameManager.instance.OnAlertStarted += RageStart; // 경보 발생 시작 -> 폭주 시작
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        LightingManager.instance.OnLightChanged -= ChangeStatValue;
        SubmarineInGameManager.instance.OnAlertStarted -= RageStart;
    }

    private void Awake()
    {
        _fsm = new StateMachine<InnerMonsterController>(this);
        _rageFsm = new StateMachine<InnerMonsterController>(this);
    }

    private void Start()
    {
        // 현재 상태머신을 기본 상태머신으로 초기화
        _currentFsm = _fsm;

        // 플레이어 트랜스폼 가져오기
        playerTransform = SubmarineInGameManager.instance.player.transform;

        // 컴포넌트 초기화
        _playerStat = playerTransform.GetComponent<PlayerStat>();
        _playerStatus = playerTransform.GetComponent<PlayerStatus>();
        _animator = GetComponent<Animator>();
        _nav = GetComponent<NavMeshAgent>();
        _collider = GetComponent<CapsuleCollider>();
        audioSource = GetComponent<AudioSource>();

        // 웨이포인트 배열 가져오기
        _wayPoints = wayPointsParent.GetComponentsInChildren<Transform>().Where(t => t != wayPointsParent).ToArray();

        // 괴물 초기화
        originalEyeMaterial = monsterEyeRenderer.material;
        _nav.Warp(_wayPoints[0].position); // 첫번째 웨이포인트 위치로 초기화
        _fovCenterTransform = monsterHeadTransform; // 시야각 중심 위치는 머리 기준
        ChangeState(new PatrolState()); // 처음 상태는 순찰 상태(-> Idle 상태로 전환됨)
    }

    private void Update()
    {
        // 플레이어와의 거리 계산
        _distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 경보 발생 중이 아닐 때(= 폭주 중 X)
        if (!SubmarineInGameManager.instance.IsAlerting)
        {
            // 추적 종료 딜레이 중 -> 추적 종료 딜레이 타이머 계산
            if (_isChaseEndDelay)
            {
                ChaseEndDelay(); // 추적 종료 딜레이 타이머 계산
            }

            // 공격 쿨타임 중 -> 공격 쿨타임 타이머 계산
            if (_isAttackCoolDown)
            {
                AttackCoolDown(); // 공격 쿨타임 타이머 계산
            }

            // 던지는 중 -> 투사체가 몬스터 손 위치를 따라감(= 손에 잡혀있음)
            if (_isGrabbing)
            {
                throwObj.transform.position = throwObjPos.position;
            }
        }

        // 현재 상태 머신 업데이트
        _currentFsm.Update();
    }

    // 상태 전환
    public void ChangeState(IState<InnerMonsterController> newState)
    {
        if (SubmarineInGameManager.instance.IsAlerting) // 경보 발생 중(= 폭주 중)
        {
            _currentFsm = _rageFsm;
            _fsm.ExitState();
        }
        else
        {
            _currentFsm = _fsm;
            _rageFsm.ExitState();
        }

        _currentFsm.ChangeState(newState); // 상태 변경
    }

    /// <summary>
    /// 폭주 시작
    /// </summary>
    private void RageStart()
    {
        ChangeState(new RageStartState()); // 폭주 시작 상태로 전환
    }

    /// <summary>
    /// 타겟을 바라보도록 회전
    /// </summary>
    /// <param name="targetPos">타겟 위치</param>
    public void LookAtTarget(Vector3 targetPos)
    {
        Vector3 targetDir = targetPos - transform.position;
        targetDir.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
    }

    /// <summary>
    /// 이동 가능 여부 설정 
    /// </summary>
    public void CanMove(bool canMove)
    {
        _nav.isStopped = !canMove;
        if (!canMove)
            _nav.velocity = Vector3.zero;
    }

    /// <summary>
    /// 목적지로 향하는 경로가 유효한지 여부 반환
    /// </summary>
    public bool IsPathValid(Vector3 targetPos)
    {
        NavMeshPath path = new NavMeshPath();
        if (_nav.CalculatePath(targetPos, path))
        {
            return path.status == NavMeshPathStatus.PathComplete; // 경로가 완전한지 여부 반환
        }
        // 경로 계산을 못하면
        return false; // 목적지가 navMesh 위에 있지 않음
    }

    /// <summary>
    /// 공격(폭주 공격 포함) 중 여부에 따른 시야각 중심 위치 변경
    /// </summary>
    /// <param name="isAttacking">공격(폭주 공격 포함) 중 여부</param>
    public void ChangeFovCenter(bool isAttacking)
    {
        if (isAttacking)
            _fovCenterTransform = transform;
        else
            _fovCenterTransform = monsterHeadTransform;
    }

    /// <summary>
    /// 감지 가능 여부 반환
    /// </summary>
    public bool CanDetect()
    {
        Vector3 eyePos = transform.position + Vector3.up; // 눈높이 위치

        DrawVisionRays(eyePos); // 씬에서 시야 레이(좌, 중, 우) 그리기

        // 플레이어와의 거리 계산
        _distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 거리 체크
        if (_distToPlayer > _detectDistance) // 감지 거리보다 크면 -> 감지 X
            return false;

        // 각도 체크
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized; // 플레이어를 바라보는 방향 벡터
        float angle = Vector3.Angle(_fovCenterTransform.forward, dirToPlayer); // 괴물이 앞을 바라보는 벡터와 플레이어를 바라보는 방향 벡터 사이의 각도
        if (angle > _fov * 0.5f) // 각도가 시야각의 절반을 벗어나면 ->  감지 X
            return false;

        // 레이캐스트로 시야 거리 내 장애물 감지
        int mask = ~LayerMask.GetMask("Monster"); // Monster 레이어만 제외
        Debug.DrawRay(eyePos, dirToPlayer * _detectDistance, Color.red);
        if (Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, _detectDistance, mask))
        {
            if (hit.collider.CompareTag("Player")) // 레이가 플레이어에 닿으면 -> 감지 O
                return true;
        }

        return false; // 레이가 플레이어에 닿지 않으면 -> 감지 X
    }

    /// <summary>
    /// 씬에서 시야 레이(좌, 중, 우) 그리기
    /// </summary>
    /// <param name="eyePos">눈높이 위치</param>
    private void DrawVisionRays(Vector3 eyePos)
    {
        Debug.DrawRay(eyePos, _fovCenterTransform.forward * _detectDistance, Color.green);
        // 오른쪽 시야각 끝 레이
        Vector3 rightDir = Quaternion.Euler(0, _fov / 2f, 0) * _fovCenterTransform.forward;
        Debug.DrawRay(eyePos, rightDir * _detectDistance, Color.yellow);
        // 왼쪽 시야각 끝 레이
        Vector3 leftDir = Quaternion.Euler(0, -_fov / 2f, 0) * _fovCenterTransform.forward;
        Debug.DrawRay(eyePos, leftDir * _detectDistance, Color.yellow);
    }

    /// <summary>
    /// 변경 여부에 따른 콜라이더 중심 변경
    /// <para>- 몬스터의 머리가 애니메이션 때문에 콜라이더 바깥으로 나가서 카메라에 머릿속이 보이는 현상 때문에 필요</para>
    /// </summary>
    /// <param name="isChange">변경 여부, false면 기본으로 되돌림</param>
    public void ColliderCenterChange(bool isChange)
    {
        if (isChange)
        {
            Vector3 centerPos = _collider.center;
            centerPos.z = 0.5f;
            _collider.center = centerPos;
        }
        else
        {
            Vector3 centerPos = _collider.center;
            centerPos.z = 0f;
            _collider.center = centerPos;
        }
    }

    /// <summary>
    /// 추적 종료 딜레이 시작
    /// </summary>
    /// <param name="lastPos">추적 범위 내에서 플레이어의 마지막 위치</param>
    public void StartChaseEndDelay(Vector3 lastPos)
    {
        _isChaseEndDelay = true; // 추적 종료 딜레이 중으로 설정
        _chaseEndDelayTimer = 0f; // 추적 종료 딜레이 타이머 0으로 초기화
        _nav.SetDestination(lastPos); // 목적지를 플레이어의 실시간 위치가 아닌, 추적 범위 내에서 플레이어의 마지막 위치로 설정
    }

    /// <summary>
    /// 추적 종료 딜레이(추적 상태에서 진행됨)
    /// </summary>
    private void ChaseEndDelay()
    {
        _chaseEndDelayTimer += Time.deltaTime; // 타이머 계산

        // 감지 가능하면 -> 추적 종료 딜레이 종료 => 추적 상태 다시 정상적 진행
        if (CanDetect())
        {
            _isChaseEndDelay = false;
            return;
        }

        // 추적 종료 딜레이 시간 초과 시 -> 추적 종료 딜레이 종료 => 두리번거리기 시작(Idle 상태로 전환됨)
        if (_chaseEndDelayTimer > _chaseEndDelayTime)
        {
            _isChaseEndDelay = false;
            StartLookAround(); // 두리번거리기 시작
        }
    }

    /// <summary>
    /// 두리번거리기 시작(추적 불가능 상태 -> Idle 상태로 전환)
    /// </summary>
    private void StartLookAround()
    {
        _isLookingAroundAfterAction = true; // 행동 후 두리번거리는 중으로 설정
        _animator.SetBool("isLookingAround", true); // 애니메이션 파라미터 설정(-> LookingAfterAction)
        AudioManager.Instance.PlaySoundSafe(audioSource, lookAroundGrowlSound);
        ChangeState(new IdleState()); // Idle 상태로 전환
    }

    /// <summary>
    /// 두리번거리기 애니메이션 종료 이벤트
    /// </summary>
    private void OnLookAroundEnd()
    {
        EndLookAround(); // 두리번거리기 종료
        ChangeState(new PatrolState()); // 순찰 상태로 전환
    }

    /// <summary>
    /// 두리번거리기 종료 
    /// <para>- 애니메이션이 종료되지 않아도 다른 상태 전환 조건을 만족할 때 호출됨</para>
    /// </summary>
    public void EndLookAround()
    {
        _isLookingAroundAfterAction = false; // 행동 후 두리번거리는 중 아님으로 설정
        _animator.SetBool("isLookingAround", false); // 애니메이션 파라미터 설정(-> Chase)
    }

    /// <summary>
    /// 공격 가능 여부(원거리 기준) 반환
    /// </summary>
    public bool CanAttack()
    {
        if (CanDetect() && _distToPlayer <= _rangeAttackDistance && (!_isAttackCoolDown || SubmarineInGameManager.instance.IsAlerting)) // 감지 가능 & 플레이어와의 거리가 원거리 공격 거리 이내 & 공격 쿨타임 진행 중이 아니거나 경보 발생 중(경보 발생 시 공격 쿨타임 X)일 때 -> 공격 가능
        {
            return true;
        }
        return false; // 그 외 -> 공격 불가능
    }


    #region Melee Attack
    /// <summary>
    /// 때리기 공격
    /// </summary>
    public void HitAttack()
    {
        Debug.Log("때리기 공격!");
        _playerStat.Damage(5f * _attackForce);
    }

    /// <summary>
    /// 두 번 할퀴기 공격
    /// </summary>
    public void DoubleClawAttack()
    {
        Debug.Log("두 번 할퀴기 공격!");
        _playerStat.Damage(10f * _attackForce);
    }
    #endregion


    #region Range Attack
    /// <summary>
    /// 던지기 공격
    /// </summary>
    public void ThrowAttack()
    {
        Debug.Log("던지기 공격!");
        // 더 이상 투사체 잡고 있지 않음
        _isGrabbing = false;

        // 물리 활성화
        Rigidbody rb = throwObj.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        // 몬스터가 바라보는 방향으로 힘 가하기
        Vector3 dir = transform.forward;
        rb.AddForce(dir * _throwPower, ForceMode.VelocityChange);
    }

    // 던지기 공격 애니메이션에서 던지는 모션이 시작될 때 이벤트
    public void OnThrowStart()
    {
        Debug.Log("던지기 시작");
        _isGrabbing = true; // 투사체 잡고 있는 중
        throwObj.SetActive(true); // 투사체 활성화
        throwObj.GetComponent<ThrowObj>().damage = 5f * _attackForce; // 투사체의 대미지 설정
    }

    /// <summary>
    /// 점프 공격
    /// </summary>
    public void JumpAttack()
    {
        Debug.Log("점프 공격!");
        _playerStat.Damage(15f * _attackForce);
    }
    #endregion


    #region Rage Attack
    /// <summary>
    /// 폭주 공격(즉사)
    /// </summary>
    public void RageAttack()
    {
        Debug.Log("폭주 공격!");
        _playerStat.Die(EEndingType.MonsterDeath); // 즉사
    }
    #endregion


    /// <summary>
    /// 공격 애니메이션에서 공격이 플레이어에게 실제로 닿을 때 호출되는 이벤트
    /// </summary>
    public void OnAttack()
    {
        Debug.Log("공격 중");


        // 폭주 파괴 공격 -> RageDestroyState에서 처리
        if (currentAttackType == EAttackType.RageDestroyAttack)
        {
            return;
        }

        // 폭주 공격 실행(회피 불가능)
        if (currentAttackType == EAttackType.RageAttack)
        {
            if (CanDetect() && DistToPlayer <= _rageAttackDistance) // 공격 모션이 실제 닿는지 여부(폭주 공격 거리 기준)
                RageAttack();
            return;
        }

        // 공격 모션이 실제 닿는지 여부(점프 공격도 근거리 공격 거리 기준)
        bool canReach = CanDetect() && DistToPlayer <= _meleeAttackDistance;

        // 회피 가능 공격들에 대한 회피 판정
        PlayerMove playerMove = playerTransform.GetComponent<PlayerMove>();
        if (playerMove.IsDodging) // 공격 모션이 실제로 닿고 있는 와중에 플레이어가 회피 중이라면 -> 회피 성공
        {
            Debug.Log("회피 성공!");
            ChangeState(new StaggerState()); // 휘청임 상태로 전환
        }
        else // 회피 실패
        {
            // 공격 실행
            switch (currentAttackType)
            {
                case EAttackType.HitAttack:
                    AudioManager.Instance.PlaySFX(attackSounds[0]);
                    if (canReach)
                        HitAttack();
                    break;
                case EAttackType.DoubleClawAttack:
                    AudioManager.Instance.PlaySFX(attackSounds[1]);
                    if (canReach)
                        DoubleClawAttack();
                    break;
                case EAttackType.JumpAttack:
                    AudioManager.Instance.PlaySFX(attackSounds[2]);
                    if (canReach)
                        JumpAttack();
                    break;
                case EAttackType.ThrowAttack:
                    AudioManager.Instance.PlaySFX(attackSounds[3]);
                    ThrowAttack();
                    break;
            }
        }
    }

    /// <summary>
    /// 공격 애니메이션 종료 이벤트
    /// </summary>
    public void OnAttackEnd()
    {
        Debug.Log("공격 종료");
        StartAttackCoolDown(); // 공격 쿨타임 시작
    }

    /// <summary>
    /// 공격 쿨타임 시작(공격 상태)
    /// </summary>
    private void StartAttackCoolDown()
    {
        _isAttackCoolDown = true; // 공격 쿨타임 진행 중으로 설정
        _attackCoolDownTimer = 0f; // 공격 쿨타임 타이머 0으로 초기화

        if (_currentFsm == _rageFsm) // 현재 상태 머신이 폭주 상태 머신이라면(폭주 진행 중) -> 폭주 추적 상태로 전환 후 종료
        {
            ChangeState(new RageChaseState());
            return;
        }

        if (CanDetect()) // 쿨타임 중인데 감지 범위 내면 -> 추적 상태로 전환
        {
            ChangeState(new ChaseState()); // 추적 상태로 전환
        }
        else // 쿨타임 중인데 감지 범위를 벗어나면
        {
            StartLookAround(); // 두리번거리기
        }
    }

    /// <summary>
    /// 공격 쿨타임
    /// </summary>
    private void AttackCoolDown()
    {
        _attackCoolDownTimer += Time.deltaTime; // 타이머 계산

        // 공격 쿨타임 초과 시 -> 공격 쿨타임 종료
        if (_attackCoolDownTimer > attackCooldownTime)
        {
            _isAttackCoolDown = false; // 공격 쿨타임 진행 중 아님으로 설정
        }
    }

    /// <summary>
    /// 공격 쿨타임 리셋
    /// </summary>
    public void ResetAttackCoolDown()
    {
        _isAttackCoolDown = false;
        _attackCoolDownTimer = 0f;
    }

    /// <summary>
    /// 휘청임 애니메이션 종료 이벤트
    /// </summary>
    public void OnStaggerEnd()
    {
        Debug.Log("휘청임 종료");
        if (throwObj.activeSelf) // 투사체가 활성화되어 있으면(던지기 공격이 이뤄지지 않았으므로 무언가에 충돌해서 꺼지지 않음) -> 수동으로 비활성화
            throwObj.SetActive(false); // 투사체 비활성화
        StartAttackCoolDown(); // 공격이 실행됐으나 종료 발생 X(애니메이션 종료 이베트가 발생하지 않으므로 공격 쿨타임 시작 안됨) -> 공격 쿨타임 시작
    }

    /// <summary>
    /// 점프 애니메이션에서 실제 점프 시작 이벤트
    /// </summary>
    public void OnJumpStart()
    {
        Debug.Log("점프 시작");
        _isJumping = true; // 점프 중으로 설정
        CanMove(true); // 이동 정지 해제
    }

    /// <summary>
    /// 점프 애니메이션에서 실제 점프 종료 이벤트
    /// </summary>
    public void OnJumpEnd()
    {
        Debug.Log("점프 종료");
        _isJumping = false; // 점프 중 아님으로 설정
        CanMove(false); // 이동 정지
        AudioManager.Instance.PlaySFX(jumpLandingSound);
        if (_distToPlayer <= 3f) // 일정 범위 내일 때 -> 스턴
        {
            // 플레이어에게 점프해서 다가온다는 효과음, 쿵 하는 효과음 필요!!!(없으면 플레이어가 뒤돌아 있을 때 스턴이 걸리면 이유를 알기 어려움)

            StartCoroutine(_playerStatus.StunEffect()); // 스턴 효과
        }
    }

    /// <summary>
    /// 폭주 시작(포효) 애니메이션 종료 이벤트
    /// </summary>
    public void OnRageStartEnd()
    {
        // 폭주 시작 애니메이션 종료 이벤트 알림
        OnRageStartAnimationEnded?.Invoke(this);
    }

    /// <summary>
    /// 점등 여부에 따른 스탯 수치(근접 공격 범위 제외) 변경
    /// </summary>
    /// <param name="isTurnOnLight">점등 여부</param>
    void ChangeStatValue(bool isTurnOnLight)
    {
        if (isTurnOnLight) // 점등 -> 난이도 상승
        {
            _detectDistance = 20f; // 시야 감지 범위가 매우 길어짐
            _rangeAttackDistance = 5f; // 원거리 공격 거리 증가(원거리 공격 가능)
            _attackForce = 2f; // 공격력 기본
        }
        else // 소등 -> 난이도 하강
        {
            _detectDistance = 5f; // 시야 감지 범위가 매우 짧아짐
            _rangeAttackDistance = _meleeAttackDistance; // 원거리 공격 거리를 근접 공격 거리와 동일하게 변경(원거리 공격 불가)
            _attackForce = 1f; // 공격력 반으로 깎임
        }
    }

    public void StopPlaying()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }
}
