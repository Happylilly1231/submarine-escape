using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - Idle 상태
    /// <para>- 가만히 있다가 일정 시간 후 순찰 상태로 전환된다.</para>
    /// <para>- 단, 액션 후 두리번거리는 중이거나 갈 수 있는 웨이포인트가 없을 시에는 무기한으로 Idle 상태에 머무른다.</para>
    /// <para>+) 조건 만족 시 추적, 공격 상태로 전환 가능</para>
    /// </summary>
    public class IdleState : IState<InnerMonsterController>
    {
        private float _idleTime = 10f;
        private float _timer;

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.ChangeMonsterModelCenter(false); // 몬스터 모델 중심 기본으로 돌림

            _timer = 0f;
            if (!owner.IsLookingAroundAfterAction)
            {
                owner.Animator.SetBool("isWaiting", true);
                AudioManager.Instance.PlaySoundSafe(owner.audioSource, owner.idleGrowlSound);
            }
        }

        public void Update(InnerMonsterController owner)
        {
            // 액션(추적/공격/휘청임) 후 두리번거리는 중이 아닐 경우 & 갈 수 있는 웨이포인트가 있을 때만 -> 머무르는 시간 타이머 계산
            if (!owner.IsLookingAroundAfterAction && !owner.IsNoWaypointCanGo)
            {
                _timer += Time.deltaTime;

                // 머무르는 시간 종료 -> 순찰 상태로 전환
                if (_timer > _idleTime)
                {
                    owner.ChangeState(new PatrolState());
                    return;
                }
            }

            // 추적 상태로 전환
            if (owner.CanDetect() || owner.CanChaseHitPos)
            {
                if (owner.IsLookingAroundAfterAction)
                    owner.EndLookAround();
                owner.ChangeState(new ChaseState());
                return;
            }

            // 공격 상태로 전환
            if (owner.CanAttack())
            {
                if (owner.IsLookingAroundAfterAction)
                    owner.EndLookAround();
                owner.ChangeState(new AttackState());
                return;
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Animator.SetBool("isWaiting", false);
            owner.IsNoWaypointCanGo = false;
            owner.StopPlaying();
        }
    }
}