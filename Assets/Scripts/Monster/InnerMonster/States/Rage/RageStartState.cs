using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 폭주 시작 상태
    /// <para>- 제자리에서 포효 애니메이션이 재생된다.</para>
    /// <para>- 모든 문의 NavMeshObstacle이 비활성화되어, 경보 발생지로 가는 경로를 계산할 때 문을 고려하지 않도록 한다.</para>
    /// <para>- 폭주 시작 애니메이션 종료 이벤트를 구독하여 끝날 때 폭주 추적 상태로 전환되게 한다.</para>
    /// </summary>
    public class RageStartState : IState<InnerMonsterController>
    {
        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.ColliderCenterChange(false); // 컨트롤러 중심 기본으로 돌림

            owner.Animator.SetBool("isRageEnd", false);
            owner.Animator.SetTrigger("RageStart");
            AudioManager.Instance.PlaySFX(owner.rageStartSound);
            owner.ResetAttackCoolDown(); // 쿨타임 초기화(쿨타임 상태 아닌 걸로 변경)
            owner.monsterEyeRenderer.material = owner.redEyeMaterial; // 눈 색 빨간색으로 변경

            // 모든 문 NavMeshObstacle 비활성화
            foreach (Door door in SubmarineInGameManager.instance.Doors)
            {
                if (door.gameObject.activeSelf)
                    door.gameObject.GetComponent<NavMeshObstacle>().enabled = false;
            }

            // 이벤트 구독
            InnerMonsterController.OnRageStartAnimationEnded += ChangeToRageChaseState; // 폭주 시작 애니메이션 종료 -> 폭주 추적 상태로 전환
        }

        public void Update(InnerMonsterController owner)
        {
            // // 포효 애니메이션 종료 -> 폭주 추적 상태로 전환
            // if (owner.IsRageStartEnd)
            // {
            //     owner.IsRageStartEnd = false;
            //     owner.ChangeState(new RageChaseState());
            //     return;
            // }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.StopPlaying();

            // 이벤트 구독 해제
            InnerMonsterController.OnRageStartAnimationEnded -= ChangeToRageChaseState;
        }

        public void ChangeToRageChaseState(InnerMonsterController monster)
        {
            monster.ChangeState(new RageChaseState());
        }
    }
}