using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 휘청임 상태
    /// <para>- 제자리에서 휘청임 애니메이션이 재생된다.</para>
    /// </summary>
    public class StaggerState : IState<InnerMonsterController>
    {
        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.ChangeMonsterModelCenter(false); // 몬스터 모델 중심 기본으로 돌림

            owner.Animator.SetTrigger("Stagger"); // 휘청임 애니메이션 재생
            AudioManager.Instance.PlayGlobalOneShot(owner.staggerSound);
        }

        public void Update(InnerMonsterController owner)
        {

        }

        public void Exit(InnerMonsterController owner)
        {
            owner.StopPlaying();
        }
    }
}