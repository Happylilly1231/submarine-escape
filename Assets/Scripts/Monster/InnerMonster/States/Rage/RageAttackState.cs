using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 폭주 공격 상태 
    /// <para>- 폭주 공격 애니메이션이 진행된다.</para>
    /// <para>- 실제 폭주 공격은 애니메이션 이벤트로 실행된다.(InnerMonsterController 코드)</para>
    /// </summary>
    public class RageAttackState : IState<InnerMonsterController>
    {
        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.currentAttackType = EAttackType.RageAttack; // 현재 공격 타입 -> 폭주 공격
            owner.Animator.SetTrigger("RageAttack"); // 폭주 공격 애니메이션 재생
            owner.audioSource.PlayOneShot(owner.rageAttackSound);
            owner.ChangeFovCenter(true); // 시야각 중심 위치를 그냥 트랜스폼으로 변경
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ChangeMonsterModelCenter(true); // 몬스터 모델 중심 변경
        }

        public void Update(InnerMonsterController owner)
        {
            // 공격 도중 플레이어 완전 괴물화 시 -> 바로 폭주 추적 상태로 전환 
            if (owner.IsPlayerMutationCompeleted)
            {
                owner.Animator.Play("RageChase");
                owner.ChangeState(new RageChaseState());
                // owner.Animator.Play("Idle");
                // owner.ChangeState(new IdleState());
                return;
            }

            owner.LookAtTarget(owner.PlayerTransform.position); // 현재 플레이어 위치를 바라보도록 회전
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.ChangeFovCenter(false); // 시야각 중심 위치를 머리 위치로 변경
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
            owner.StopPlaying();
        }
    }
}