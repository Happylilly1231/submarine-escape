using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class AttackState : IState<DeepSeaMonsterController>
    {
        public void Enter(DeepSeaMonsterController owner)
        {
            // 현재 패턴 공격 애니메이션 재생
            if (owner.currentPattern.AnimationTriggerName != "")
                owner.animator.SetTrigger(owner.currentPattern.AnimationTriggerName);
        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        public void Exit(DeepSeaMonsterController owner)
        {

        }
    }
}