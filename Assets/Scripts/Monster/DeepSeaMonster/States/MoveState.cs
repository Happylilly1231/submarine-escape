using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class MoveState : IState<DeepSeaMonsterController>
    {
        public void Enter(DeepSeaMonsterController owner)
        {

        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        /// <summary>
        /// Rigidbody를 이용한 이동
        /// </summary>
        /// <param name="owner"></param>
        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            bool isMoveFinished = owner.currentPattern.UpdateMovement();

            // 이동이 완료되거나 플레이어가 닿았을 때 -> 공격 상태로 전환됨
            if (isMoveFinished || owner.IsPlayerTriggered)
            {
                owner.ChangeState(new AttackState());
                return;
            }
        }

        public void Exit(DeepSeaMonsterController owner)
        {

        }
    }
}