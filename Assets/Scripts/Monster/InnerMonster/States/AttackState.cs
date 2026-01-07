using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 공격 상태 
    /// <para>- 플레이어를 공격한다.</para>
    /// <para>- 공격 종류: 근접 - 때리기 공격, 두 번 할퀴기 공격 / 원거리 - 점프 공격, 던지기 공격</para>
    /// <para>- 플레이어와의 거리에 따라 공격 패턴(근접/원거리)을 선택한다.</para>
    /// <para>- 단, 원거리 공격 거리를 벗어났을 시 추적 상태로 전환된다.</para>
    /// <para>- 선택된 공격 패턴에서 랜덤으로 하나의 공격이 선택되고 해당 애니메이션이 진행된다.</para>
    /// <para>- 실제 공격과 공격 쿨타임 시작은 애니메이션 이벤트로 실행된다.(InnerMonsterController 코드)</para>
    /// </summary>
    public class AttackState : IState<InnerMonsterController>
    {
        private float _stopDistance = 2f; // 멈춤 거리

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            AttackCurrentPattern(owner); // 현재 패턴 공격(근접/원거리)
            owner.monsterEyeRenderer.material = owner.redEyeMaterial; // 눈 색 빨간색으로 변경
            owner.ChangeFovCenter(true); // 시야각 중심 위치를 그냥 트랜스폼으로 변경
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ColliderCenterChange(true); // 컨트롤러 중심 변경
        }

        public void Update(InnerMonsterController owner)
        {
            owner.LookAtTarget(owner.PlayerTransform.position); // 현재 플레이어 위치를 바라보도록 회전

            // 점프 중 -> 플레이어를 향해 이동
            if (owner.IsJumping)
            {
                if (owner.DistToPlayer <= _stopDistance)
                    owner.CanMove(false);
                else
                    owner.CanMove(true);

                owner.Nav.SetDestination(owner.PlayerTransform.position);
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.ChangeFovCenter(false); // 시야각 중심 위치를 머리 위치로 변경
            owner.monsterEyeRenderer.material = owner.originalEyeMaterial; // 내부 괴물의 눈 머티리얼 원래 머티리얼(하얀색)로 변경
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
        }

        // 현재 패턴 공격(근접/원거리)
        void AttackCurrentPattern(InnerMonsterController monster)
        {
            if (monster.DistToPlayer <= monster.MeleeAttackDistance) // 근접 공격 거리 내(소등 상태 -> 원거리 공격 불가(원거리와 근접 공격 거리가 동일하므로 근접 공격 선택되거나 이 함수가 실행 되기 직전에 플레이어가 공격 거리에서 벗어난다고 하더라도 else 문으로 가므로 순찰 상태로 전환됨))
                MeleePattern(monster); // 근접 공격
            else if (monster.DistToPlayer <= monster.RangeAttackDistance) // 원거리 공격 거리 내
                RangePattern(monster); // 원거리 공격
            else // 공격 불가
                monster.ChangeState(new ChaseState()); // 추적 상태로 전환
        }

        // 근접 공격
        void MeleePattern(InnerMonsterController monster)
        {
            int r = Random.Range(0, 2);

            switch (r)
            {
                case 0:
                    monster.currentAttackType = EAttackType.HitAttack;
                    monster.Animator.SetInteger("attackType", 0);
                    monster.Animator.SetTrigger("Attack");
                    break;

                case 1:
                    monster.currentAttackType = EAttackType.DoubleClawAttack;
                    monster.Animator.SetInteger("attackType", 1);
                    monster.Animator.SetTrigger("Attack");
                    break;
            }
        }

        // 원거리 공격
        void RangePattern(InnerMonsterController monster)
        {
            int r = Random.Range(0, 2);

            switch (r)
            {
                case 0:
                    monster.currentAttackType = EAttackType.ThrowAttack;
                    monster.Animator.SetInteger("attackType", 2);
                    monster.Animator.SetTrigger("Attack");
                    break;

                case 1:
                    monster.currentAttackType = EAttackType.JumpAttack;
                    monster.Animator.SetInteger("attackType", 3);
                    monster.Animator.SetTrigger("Attack");
                    break;
            }
        }
    }
}
