using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeepSeaMonsterStates;
using System;

/// <summary>
/// 심해 괴물 움직임 관리
/// </summary>
public class DeepSeaMonsterController : MonoBehaviour
{
    private StateMachine<DeepSeaMonsterController> _fsm; // 상태 머신

    [SerializeField] private MonsterHitBox hitBox;
    [SerializeField] private DeepSeaCameraController deepSeaCameraController;
    public DeepSeaCameraController CameraController => deepSeaCameraController;
    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;
    [SerializeField] private DeepSeaPlayerMove deepSeaPlayerMove;
    public DeepSeaPlayerMove DeepSeaPlayerMove => deepSeaPlayerMove;
    public Transform playerGrabPos;
    public LayerMask obstacleLayerMask;
    public GameObject monsterGeo; // 몬스터 외형 모습

    private Rigidbody _rb;
    public Rigidbody Rb => _rb;
    public Animator animator;
    private float _fsmEndDepth = 95f; // FSM 종료 수심 

    public List<DeepSeaMonsterAttackBase> AllPatternList { get; private set; } = new List<DeepSeaMonsterAttackBase>();
    public DeepSeaMonsterAttackBase currentPattern = null;
    private Dictionary<DeepSeaMonsterAttackBase, int> _patternCounts = new Dictionary<DeepSeaMonsterAttackBase, int>();

    public bool IsEnded { get; private set; } = false; // FSM 종료 여부
    public bool IsPlayerTriggered { get; private set; } = false;
    public float MonsterRadius { get; private set; } = 5f;
    public int TorpedoHitCount { get; private set; } = 0; // 어뢰 발사 맞은 횟수
    public float CurrentDamageScale { get; private set; } = 1f; // 현재 대미지 배율
    public float RespawnWaitingTime { get; private set; } = 15f; // 재스폰 대기 시간

    public AudioSource audioSource;
    public AudioClip spawnSound;

    private void Awake()
    {
        if (deepSeaPlayerMove == null) return;

        _fsm = new StateMachine<DeepSeaMonsterController>(this);
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (deepSeaPlayerMove == null) return;

        AllPatternList.Add(new DashAttack(this));
        AllPatternList.Add(new ScratchAttack(this));
        AllPatternList.Add(new DragDownAttack(this));

        foreach (var pattern in AllPatternList)
        {
            _patternCounts[pattern] = 0;
        }

        // SetTorpedoHitCountAndApplyEffect(3);

        _fsm.ChangeState(new SpawnState());
    }

    private void Update()
    {
        if (deepSeaPlayerMove == null) return;

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (deepSeaPlayerMove.DisplayDepth <= _fsmEndDepth && !IsEnded)
        {
            IsEnded = true;
        }

        _fsm.Update();
    }

    private void FixedUpdate()
    {
        if (deepSeaPlayerMove == null) return;

        _fsm.FixedUpdate();
    }

    private void OnDisable()
    {
        if (deepSeaPlayerMove == null) return;

        _fsm.ExitState(); // 비활성화(파괴 직전)될 때 -> 무조건 상태 종료
    }

    private void OnTriggerEnter(Collider other)
    {
        if (deepSeaPlayerMove == null) return;

        if (!IsPlayerTriggered && other.gameObject.CompareTag("Player"))
        {
            IsPlayerTriggered = true;
            Debug.Log("플레이어 트리거 발생");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (deepSeaPlayerMove == null) return;

        if (IsPlayerTriggered && other.gameObject.CompareTag("Player"))
        {
            IsPlayerTriggered = false;
            Debug.Log("플레이어 트리거 발생 X");
        }
    }

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
        enabled = false;
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
            CurrentDamageScale = 0.5f; // 대미지 1/2배
        }

        if (TorpedoHitCount >= 2)
        {
            RespawnWaitingTime = 20f; // 재스폰 대기 시간 5초 증가 -> 괴물 출현 횟수 감소
        }

        if (TorpedoHitCount == 3)
        {
            deepSeaPlayerMove.TargetSpaceCount = 5; // 끌어내리기 탈출 스페이스 횟수 10 -> 5로 감소

            // 최종 패턴 슬로우 배속 0.04f 줄여줌
            FinalPatternManager finalPatternManager = FindAnyObjectByType<FinalPatternManager>();
            finalPatternManager.FirstSlowTimeScale -= 0.04f; // 0.08f -> 0.04f
            finalPatternManager.FinalSlowTimeScale -= 0.04f; // 0.18f -> 0.14f
        }
    }

    // 애니메이션 이벤트 1 (공격 내지르는 순간)
    public void OnAttackStart()
    {
        hitBox.EnableHitbox(
            onSuccess: () => currentPattern.OnAttackSuccess(), // 적중 시
            onMiss: () => currentPattern.OnAttackMiss()       // 빗나갔을 때
        );
    }

    // 애니메이션 이벤트 2 (공격 휘두르기가 끝나는 순간)
    public void OnAttackEnd()
    {
        hitBox.DisableHitbox();
    }

    // /// <summary>
    // /// 공격 애니메이션에서 공격이 플레이어에게 실제로 닿을 때 호출되는 이벤트
    // /// </summary>
    // public void OnAttack()
    // {
    //     if (IsPlayerTriggered)
    //     {
    //         Debug.Log("공격 성공 판정");
    //         currentPattern.OnAttackSuccess();
    //     }
    //     else
    //     {
    //         Debug.Log("공격 실패 판정");
    //         currentPattern.OnAttackMiss();
    //     }
    // }

    /// <summary>
    /// 플레이어에게 대미지 (산소 감소)
    /// </summary>
    public void DamagePlayer(float amount)
    {
        deepSeaPlayerMove.TakeDamage(amount * CurrentDamageScale); // 현재 대미지 배율 적용하여 대미지 줌

        // 1. 공격 방향 계산 (괴물 위치 -> 플레이어 위치 방향 vector)
        Vector3 hitDirection = (deepSeaPlayerMove.transform.position - transform.position).normalized;
        hitDirection.y = 0; // 평면 기준 넉백을 위해 Y축 보정 (필요시 제거 가능)

        // 2. HitStop 적용
        deepSeaPlayerMove.TriggerHitStop(0.2f);

        // // 3. 카메라 셰이크 적용
        // deepSeaCameraController.ShakeCameraWithFOV(duration: 1f, strength: 0.5f, fovImpact: 8f);

        // // 4. 플레이어 넉백 적용
        // deepSeaPlayerMove.ApplyKnockback(hitDirection, distance: 5f, duration: 0.5f);
    }
}
