using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class DeadState : IState<DeepSeaMonsterController>
    {
        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(false); // 외형 안 보이게 함
            owner.monsterLight.gameObject.SetActive(false);
        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        public void Exit(DeepSeaMonsterController owner)
        {

        }
    }
}