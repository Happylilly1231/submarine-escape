using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class HiddenState : IState<DeepSeaMonsterController>
    {
        private float _timer;
        private float waitingTime = 15f;

        public void Enter(DeepSeaMonsterController owner)
        {
            _timer = 0f;

            owner.monsterGeo.SetActive(false); // 외형 안 보이게 함
        }

        public void Update(DeepSeaMonsterController owner)
        {
            // FSM 종료 플래그가 켜져있을 시 -> 더 이상 스폰하지 않고 아예 FSM 종료 (스크립트 비활성화)
            if (owner.IsEnded)
            {
                owner.TerminateFSM(); // FSM 종료
                return;
            }

            _timer += Time.deltaTime;

            // 기다리는 시간(쿨타임) 지나면 -> 스폰 상태로 전환
            if (_timer >= waitingTime)
            {
                owner.ChangeState(new SpawnState());
                return;
            }
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(true); // 외형 보이게 함
        }
    }
}