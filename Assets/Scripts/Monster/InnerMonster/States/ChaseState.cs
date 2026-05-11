using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 추적 상태 
    /// <para>- 플레이어를 향해 이동한다.</para>
    /// <para><code>- [추적 종료 딜레이]
    ///     - 플레이어가 추적 범위를 벗어나도 일정 시간 동안 추적 상태에 머무른다.
    ///     - 추적 범위를 벗어났을 때 플레이어의 위치를 향해 이동한다.
    ///     - 딜레이 중 플레이어를 다시 감지하면 딜레이는 종료되고 다시 정상적으로 추적한다.(InnerMonsterController 코드)
    ///     - 감지 못하고 딜레이 종료 시 두리번거리고 Idle 상태로 전환된다.(InnerMonsterController 코드)
    /// </code></para>
    /// <para>+) 조건 만족 시 공격 상태로 전환 가능</para>
    /// </summary>
    public class ChaseState : IState<InnerMonsterController>
    {
        private float _chaseSpeed = 6f; // 추적 속도
        private float _stopDistance = 2f; // 멈춤 거리
        private Vector3 _lastPos; // 추적 범위를 벗어났을 때 목표의 마지막 위치
        private bool _isClose; // 가까운지 여부
        private Vector3 _targetPos;
        private bool _isHitPosChasing = false;

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(true); // 이동
            owner.Nav.speed = _chaseSpeed; // 추적 속도로 변경
            owner.monsterEyeRenderer.material = owner.redEyeMaterial; // 눈 색 빨간색으로 변경
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ChangeMonsterModelCenter(true); // 몬스터 모델 중심 변경
            owner.Animator.SetBool("isChasing", true);
            AudioManager.Instance.PlaySoundSafe(owner.audioSource, owner.chaseSound, 3f);

            // 목적지 설정
            if (owner.CanDetect()) // 플레이어 감지 가능 상태면 -> 플레이어 추적
            {
                _isHitPosChasing = false;
                _targetPos = owner.PlayerTransform.position; // 플레이어 위치를 목적지로 설정
                owner.EndChastHitPosTimer(); // 만약 소리 난 곳 추적 가능 타이머 돌아가고 있었으면 종료 (플레이어 감지 경우로 우선 선택됐기 때문)
            }
            else // 그렇지 않은데 추적 상태에 온 건, 현재 소리 난 곳 추적 가능한 경우라는 소리
            {
                _isHitPosChasing = true;
                _targetPos = owner.CurrentHitPos; // 현재 소리난 곳으로 목적지 설정
            }
            owner.Nav.SetDestination(_targetPos);

            // 플레이어와의 거리에 따른 애니메이션 설정 초기화
            _isClose = Vector3.Distance(owner.transform.position, _targetPos) < _stopDistance;
            if (_isClose) // 플레이어와 가까우면(플레이어가 시야 내에 존재) -> Chase Idle 애니메이션
            {
                owner.CanMove(false);
                owner.Animator.SetBool("isChaseWaiting", true);
                owner.StopPlaying();
            }
            else // 플레이어와 멀면 -> Chase 애니메이션
            {
                owner.CanMove(true);
                owner.Animator.SetBool("isChaseWaiting", false);
                AudioManager.Instance.PlaySoundSafe(owner.audioSource, owner.chaseSound, 3f);
            }
        }

        public void Update(InnerMonsterController owner)
        {
            // 추적 도중 플레이어 완전 괴물화 시 -> 바로 순찰 상태로 전환 
            if (owner.IsPlayerMutationCompeleted)
            {
                owner.Animator.Play("Walk");
                owner.ChangeState(new PatrolState());
                // owner.Animator.Play("Idle");
                // owner.ChangeState(new IdleState());
                return;
            }

            // 공격 상태로 전환
            if (owner.CanAttack())
            {
                owner.ChangeState(new AttackState());
                return;
            }

            // 소리 난 곳에 도착 시
            if (owner.CanChaseHitPos && Vector3.Distance(owner.transform.position, _targetPos) < 0.5f)
            {
                Debug.Log("도착!");
                owner.EndChastHitPosTimer();
                owner.StartLookAround(); // 추적 종료 딜레이 없이 Idle 상태로 전환(두리번거림)
                return;
            }

            // 소리 난 곳을 일정 시간 내에 도달하지 못해서, 더는 소리 난 곳을 추적할 수 없을 때
            if (_isHitPosChasing && !owner.CanChaseHitPos)
            {
                owner.StartLookAround(); // 추적 종료 딜레이 없이 Idle 상태로 전환(두리번거림)
                return;
            }

            // 추적 종료 딜레이 중이 아닐 때(= 이전까지 플레이어 감지되었음) -> 추적 종료 딜레이 시작
            if (!owner.IsChaseEndDelay)
            {
                if (_isHitPosChasing) // 소리 난 곳 추적 중
                {
                    owner.LookAtTarget(owner.CurrentHitPos); // 소리 난 곳을 바라보도록 회전
                }
                else // 플레이어 추적 중
                {
                    owner.LookAtTarget(owner.PlayerTransform.position); // 현재 플레이어 위치를 바라보도록 회전
                    owner.Nav.SetDestination(owner.PlayerTransform.position); // 목적지를 현재 플레이어 위치로 갱신
                }

                // 추적 종료 딜레이 시작 경우
                if (!_isHitPosChasing && !owner.CanDetect()) // 플레이어를 추적 중이었는데, 플레이어가 감지 범위를 벗어났을 때
                {
                    _lastPos = owner.PlayerTransform.position; // 추적 범위를 벗어났을 때 플레이어의 위치(마지막 위치) 설정
                    owner.StartChaseEndDelay(_lastPos); // 해당 위치로 일정 시간 동안 이동하는 추적 종료 딜레이 시작
                    return;
                }
            }
            else
            {
                owner.LookAtTarget(_lastPos); // 목표의 마지막 위치를 바라보도록 회전
            }

            // 현재 남은 거리 - 경로 계산 중이 아닐 때만 남은 거리로 비교하고, 계산 중일 때는 직접 거리 구해 사용
            float currentDistance = owner.Nav.pathPending
                ? Vector3.Distance(owner.transform.position, _targetPos)
                : owner.Nav.remainingDistance;

            // 플레이어와의 거리에 따른 애니메이션 설정
            if (!_isClose && owner.CanDetect() && currentDistance <= _stopDistance) // 플레이어와 가까우면(플레이어가 시야 내에 존재) -> Chase Idle 애니메이션
            {
                _isClose = true;
                owner.CanMove(false);
                owner.Animator.SetBool("isChaseWaiting", true);
                owner.StopPlaying();
            }
            else if (_isClose && currentDistance > _stopDistance) // 플레이어와 멀어지면 -> Chase 애니메이션
            {
                _isClose = false;
                owner.CanMove(true);
                owner.Animator.SetBool("isChaseWaiting", false);
                AudioManager.Instance.PlaySoundSafe(owner.audioSource, owner.chaseSound, 3f);
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Animator.SetBool("isChasing", false);
            owner.Animator.SetBool("isChaseWaiting", false);
            owner.StopPlaying();
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
            owner.monsterEyeRenderer.material = owner.originalEyeMaterial; // 내부 괴물의 눈 머티리얼 원래 머티리얼(하얀색)로 변경
        }
    }
}