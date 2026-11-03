using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    public class DeadState : IState<InnerMonsterController>
    {
        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 불가
            owner.ChangeMonsterModelCenter(false); // 몬스터 모델 중심 기본으로 돌림

            owner.Animator.SetTrigger("Die"); // 죽는 애니메이션 재생

            owner.Nav.enabled = false; // NavMeshAgent 비활성화
            owner.spinalCord.gameObject.SetActive(false); // 척추도 비활성화
            owner.nonCordColliderObj.SetActive(false); // 척추 아닌 구역 콜라이더도 비활성화
            owner.Collider.enabled = false; // 모델 콜라이더 비활성화

            owner.monsterCorpseClue.CorpseCollider.enabled = true; // 시체 콜라이더는 활성화

            owner.enabled = false; // 괴물 FSM 비활성화
        }

        public void Update(InnerMonsterController owner)
        {

        }

        public void Exit(InnerMonsterController owner)
        {

        }
    }
}