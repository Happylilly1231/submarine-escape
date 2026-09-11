using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class DragDownState : IState<DeepSeaMonsterController>
    {
        private DeepSeaMonsterController _monster;

        private float dragDownSpeed = 4f;

        public void Enter(DeepSeaMonsterController owner)
        {
            _monster = owner;

            owner.transform.position = owner.playerGrabPos.position;

            owner.DeepSeaPlayerMove.OnGrabQTESuccess += OnPlayerQTESuccess;
            owner.DeepSeaPlayerMove.OnGrabQTEFailed += OnPlayerQTEFailed;

            // 플레이어를 붙잡힘 상태로 만듦 -> 플레이어에서 QTE 로직 처리
            owner.DeepSeaPlayerMove.StartGrabQTE();

            owner.CameraController.SetInputEnabled(false);
            owner.CameraController.RotateToTargetPos(owner.transform.position, 0.5f);
        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            Vector3 targetChargePos = owner.Rb.position + Vector3.down * dragDownSpeed * Time.fixedDeltaTime;
            owner.Rb.MovePosition(targetChargePos);
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            owner.DeepSeaPlayerMove.OnGrabQTESuccess -= OnPlayerQTESuccess;
            owner.DeepSeaPlayerMove.OnGrabQTEFailed -= OnPlayerQTEFailed;
        }

        public void OnPlayerQTESuccess()
        {
            Debug.Log("탈출 성공! 퇴각");
            _monster.ChangeState(new RetreatState());
        }

        public void OnPlayerQTEFailed()
        {
            Debug.Log("탈출 실패! 게임 오버");
            GameManager.instance.GameOver(EEndingType.MonsterDeath);
        }
    }
}