using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

            owner.deepSeaPlayerMove.CanMove = false; // 플레이어 이동 정지
            owner.deepSeaPlayerMove.ResetModelRotation();

            owner.transform.position = owner.playerGrabPos.position;
            owner.transform.LookAt(owner.playerGrabPos.position + Vector3.up * 5f);

            owner.CameraController.SetInputEnabled(false); // 카메라 조작 불가능
            owner.StartCoroutine(owner.CameraController.Routine_LookAtPosition(owner.transform.position, 0.5f));

            owner.DeepSeaPlayerMove.OnGrabQTESuccess += OnPlayerQTESuccess;
            owner.DeepSeaPlayerMove.OnGrabQTEFailed += OnPlayerQTEFailed;

            // 플레이어를 붙잡힘 상태로 만듦 -> 플레이어에서 QTE 로직 처리
            owner.DeepSeaPlayerMove.StartGrabQTE();
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
            owner.deepSeaPlayerMove.CanMove = true; // 플레이어 이동 가능
            owner.CameraController.SetInputEnabled(true); // 카메라 조작 가능
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