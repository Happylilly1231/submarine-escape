using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using InnerMonsterStates;
using DG.Tweening;

public class InnerMonsterCutscene : MonoBehaviour
{
    private InnerMonsterController _controller;
    private NavMeshAgent _nav;
    private Animator _animator;

    [Header("테스트용 트리거")]
    public bool playFullCutscene;

    [Header("플레이어 컷씬 스크립트")]
    public PlayerCutscene playerCutsceneScript;

    [Header("연출용 괴물 목적지 설정")]
    public Transform controlRoomPos;
    public Transform controlRoomCenterPos;
    public Transform controlRoomFrontPos;
    public Transform engineRoomPos;
    public Transform cctvPos;

    [Header("혈흔")]
    public GameObject roomBlood;

    [Header("엔진 연기 효과")]
    public GameObject[] engineSmokeEffect;

    private void Awake()
    {
        _controller = GetComponent<InnerMonsterController>();
        _nav = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        StartCoroutine(StartCutsceneAfterDelay(2.06f));
    }

    private IEnumerator StartCutsceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        StopAllCoroutines();
        StartCoroutine(FullCutsceneSequence());
    }

    private void OnValidate()
    {
        // 게임이 재생 중일 때 인스펙터에서 체크박스를 누르면 연출 시작
        if (!Application.isPlaying) return;

        if (playFullCutscene)
        {
            playFullCutscene = false;
            StopAllCoroutines();
            StartCoroutine(FullCutsceneSequence());
        }
    }

    /// <summary>
    /// 조종실 이동 ➔ 공격 ➔ 엔진실 이동까지 연출
    /// </summary>
    private IEnumerator FullCutsceneSequence()
    {
        Debug.Log("컷씬 시퀀스 시작!");
        _controller.IsInCutscene = true;

        StartCoroutine(playerCutsceneScript.PlayPlayerSequence());

        // ==========================================
        // 1단계: 조종실로 질주
        // ==========================================
        _controller.ChangeState(new ChaseState());

        _controller.enabled = false;

        if (controlRoomPos != null && _nav.isOnNavMesh)
        {
            Debug.Log("[1단계] 조종실로 빠르게 이동");

            _nav.isStopped = false;
            _nav.SetDestination(controlRoomPos.position);
            _animator.SetBool("isChasing", true);

            while (_nav.pathPending || _nav.remainingDistance > 0.2f)
            {
                _controller.LookAtTarget(controlRoomPos.position);
                yield return null;
            }
        }

        // ==========================================
        // 2단계: 조종실 도착 후 플레이어 조준 및 공격 상태 진입
        // ==========================================
        _nav.isStopped = true;
        _animator.SetBool("isChasing", false);
        _animator.SetBool("isChaseWaiting", false);

        // 플레이어 위치를 바라보도록 설정
        Vector3 playerPos = _controller.PlayerTransform.position;
        _controller.LookAtTarget(playerPos);
        //yield return new WaitForSeconds(0.5f);

        // 플레이어 공격 모션
        Debug.Log("[2단계] 플레이어 공격 모션");
        _controller.ChangeState(new AttackState());

        _controller.currentAttackType = EAttackType.HitAttack;
        _animator.SetInteger("attackType", 0);
        _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);
        // 혈흔 연출
        StartCoroutine(SplashBloodSequence());
        playerCutsceneScript.TakeMonsterAttack();

        float attackDuration = 2.0f;
        float timer = 0f;
        while (timer < attackDuration)
        {
            _controller.LookAtTarget(_controller.PlayerTransform.position);

            float currentWeight = _animator.GetLayerWeight(_controller.lowerBodyLayerIndex);
            _animator.SetLayerWeight(_controller.lowerBodyLayerIndex, Mathf.Lerp(currentWeight, 0f, Time.deltaTime * 10f));

            timer += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(1f);

        // ==========================================
        // 3단계: 조종실 잠시 둘러보기
        // ==========================================
        _controller.ChangeState(new ChaseState());
        _controller.enabled = false;

        if (controlRoomCenterPos != null && _nav.isOnNavMesh)
        {
            Debug.Log("[3단계] 조종실 잠시 둘러보기");

            _nav.isStopped = false;
            _nav.speed = 3.0f;
            _nav.SetDestination(controlRoomCenterPos.position);

            _animator.SetBool("isChasing", true);
            _animator.SetBool("isChaseWaiting", false);

            while (_nav.pathPending || _nav.remainingDistance > 0.2f)
            {
                // 다음 경로 지점을 부드럽게 바라봄
                Vector3 dir = (_nav.steeringTarget - transform.position).normalized;
                dir.y = 0; // 모델이 앞뒤로 기울어지는 것 방지

                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5.0f);
                }
                yield return null;
            }
        }

        // ==========================================
        // 4단계: 조종실 나가기
        // ==========================================
        if (controlRoomFrontPos != null && _nav.isOnNavMesh)
        {
            Debug.Log("[4단계] 조종실 나가기");

            _nav.isStopped = false;
            _nav.speed = 7.0f;
            _nav.SetDestination(controlRoomFrontPos.position);

            _animator.SetBool("isChasing", true);
            _animator.SetBool("isChaseWaiting", false);

            while (_nav.pathPending || _nav.remainingDistance > 0.2f)
            {
                // 180도 회전 시 부드럽게 호를 그리며 돌기 위한 로직
                Vector3 dir = (_nav.steeringTarget - transform.position).normalized;
                dir.y = 0;

                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 4.0f);
                }
                yield return null;
            }
        }

        // ==========================================
        // 5단계: 엔진실로 질주
        // ==========================================
        _controller.ChangeState(new ChaseState());
        _controller.enabled = false;

        if (engineRoomPos != null && _nav.isOnNavMesh)
        {
            Debug.Log("[5단계] 엔진실로 빠르게 이동");
            _nav.isStopped = false;
            _nav.speed = 7.0f;
            _nav.SetDestination(engineRoomPos.position);

            _animator.SetBool("isChasing", true);
            _animator.SetBool("isChaseWaiting", false);

            while (_nav.pathPending || _nav.remainingDistance > 0.2f)
            {
                _controller.LookAtTarget(engineRoomPos.position);
                yield return null;
            }
        }

        // ==========================================
        // 6단계: 엔진 앞에 도착, 파괴
        // ==========================================
        Debug.Log("[6단계] 엔진 앞에 도착");
        _nav.isStopped = true;
        _animator.SetBool("isChasing", false);
        _animator.SetBool("isChaseWaiting", false);

        // 괴물이 엔진을 바라보도록 회전
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 rightRotation = new Vector3(currentEuler.x, currentEuler.y + 90f, currentEuler.z);

        yield return transform.DORotate(rightRotation, 0.2f, RotateMode.Fast)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        //yield return new WaitForSeconds(0.1f);

        Debug.Log("[6단계] 엔진에 DoubleClawAttack");
        _controller.ChangeState(new AttackState());

        _controller.currentAttackType = EAttackType.DoubleClawAttack;
        _animator.SetInteger("attackType", 1);
        _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(2.7f);

        // 엔진 연기 효과 활성화
        foreach (GameObject smoke in engineSmokeEffect) smoke.SetActive(true);
        yield return new WaitForSeconds(1f);

        // ==========================================
        // 7단계: cctv 앞으로 이동 후 파괴
        // ==========================================
        _controller.ChangeState(new ChaseState());
        _controller.enabled = false;

        if (cctvPos != null && _nav.isOnNavMesh)
        {
            Debug.Log("[7단계] CCTV 위치로 이동");
            _nav.isStopped = false;
            _nav.speed = 6.0f;
            _nav.SetDestination(cctvPos.position);

            // _animator.SetBool("isChasing", true);
            // _animator.SetBool("isPatrolling", false);
            // _animator.SetBool("isChaseWaiting", false);

            yield return null;

            while (_nav.pathPending || _nav.remainingDistance > 0.2f)
            {
                Vector3 dir = (_nav.steeringTarget - transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5.0f);
                }
                yield return null;
            }
        }

        // CCTV 앞에 도착
        _nav.isStopped = true;
        _animator.SetBool("isChasing", false);
        _animator.SetBool("isPatrolling", false);
        _animator.SetBool("isLookingAround", false);

        // cctv를 향해 공격
        Debug.Log("[7단계] cctv를 향해 공격");
        _controller.ChangeState(new AttackState());

        _controller.currentAttackType = EAttackType.HitAttack;
        _animator.SetInteger("attackType", 0);
        _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1.5f);

        //yield return new WaitForSeconds(1.0f);


        _controller.IsInCutscene = false;

        Debug.Log("모든 컷씬 연출 완료");
    }

    /// <summary>
    /// 자식 혈흔들을 순차적으로 투명도를 조절하며 활성화하는 연출 코루틴
    /// </summary>
    private IEnumerator SplashBloodSequence()
    {
        if (roomBlood == null) yield break;

        // 부모 오브젝트 활성화
        roomBlood.SetActive(true);

        // 부모 아래에 있는 모든 SpriteRenderer 자식들을 리스트로 가져오기
        List<SpriteRenderer> bloodSprites = new List<SpriteRenderer>();
        foreach (Transform child in roomBlood.transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                bloodSprites.Add(sr);
                child.gameObject.SetActive(true);
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        // 순서대로 페이드인 실행
        foreach (SpriteRenderer sprite in bloodSprites)
        {
            StartCoroutine(FadeInSprite(sprite));

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 단일 스프라이트를 투명도 0에서 1로 부드럽고 빠르게 채우는 서브 코루틴
    /// </summary>
    private IEnumerator FadeInSprite(SpriteRenderer sprite)
    {
        float alpha = 0f;
        Color c = sprite.color;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * 5f;
            c.a = Mathf.Clamp01(alpha);
            sprite.color = c;
            yield return null;
        }
    }
}
