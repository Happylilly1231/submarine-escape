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
        private Vector3 _playerLastPos; // 추적 범위를 벗어났을 때 플레이어의 위치(마지막 위치)

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(true); // 이동
            owner.Nav.speed = _chaseSpeed; // 추적 속도로 변경
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ColliderCenterChange(true); // 컨트롤러 중심 변경
            owner.Animator.SetBool("isChasing", true);
            AudioManager.Instance.PlaySoundSafe(owner.audioSource, owner.chaseSound, 3f);

            // 플레이어 위치를 목적지로 설정
            owner.Nav.SetDestination(owner.PlayerTransform.position);
        }

        public void Update(InnerMonsterController owner)
        {
            // 공격 상태로 전환
            if (owner.CanAttack())
            {
                owner.ChangeState(new AttackState());
                return;
            }

            if (!owner.IsChaseEndDelay) // 추적 종료 딜레이 중이 아닐 때(= 이전까지 플레이어 감지되었음)
            {
                owner.LookAtTarget(owner.PlayerTransform.position); // 현재 플레이어 위치를 바라보도록 회전
                owner.Nav.SetDestination(owner.PlayerTransform.position); // 플레이어를 향해 이동

                // 플레이어가 감지 범위를 벗어났을 때 -> 추적 종료 딜레이 시작
                if (!owner.CanDetect())
                {
                    _playerLastPos = owner.PlayerTransform.position; // 추적 범위를 벗어났을 때 플레이어의 위치(마지막 위치) 설정
                    owner.StartChaseEndDelay(_playerLastPos); // 해당 위치로 일정 시간 동안 이동하는 추적 종료 딜레이 시작
                    return;
                }
            }
            else
            {
                owner.LookAtTarget(_playerLastPos); // 플레이어 마지막 위치를 바라보도록 회전
            }

            // 플레이어와의 거리에 따른 애니메이션 설정
            if (owner.Nav.remainingDistance < _stopDistance) // 플레이어와 가까우면 -> Chase Idle 애니메이션
            {
                owner.CanMove(false);
                owner.Animator.SetBool("isChaseWaiting", true);
                owner.StopPlaying();
            }
            else // 플레이어와 멀어지면 -> Chase 애니메이션
            {
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
        }
    }
}