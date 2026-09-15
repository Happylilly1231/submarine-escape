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

            owner.DeepSeaPlayerMove.CanMove = false; // 플레이어 이동 정지
            owner.DeepSeaPlayerMove.ResetModelRotation();

            owner.transform.position = owner.playerGrabPos.position;
            Debug.Log(owner.playerGrabPos.position);

            // // 1. 괴물 위치에서 수직 위쪽 방향
            // Vector3 targetPosition = owner.transform.position + Vector3.up * 5f;

            // // 2. Y축 기준: 괴물이 플레이어를 바라보는 수평 방향 계산 (Y축 높이 차이는 제거)
            // Vector3 dirToPlayer = (owner.PlayerTransform.position - owner.transform.position);
            // dirToPlayer.y = 0; // 수평 방향만 추출
            // dirToPlayer.Normalize();

            // // 3. 위를 쳐다보되(targetPosition), 머리/등 방향은 플레이어를 마주보는 방향(-dirToPlayer)으로 설정
            // if (dirToPlayer != Vector3.zero)
            // {
            //     owner.transform.LookAt(targetPosition, -dirToPlayer);
            // }

            owner.CameraController.SetInputEnabled(false); // 카메라 조작 불가능
            owner.StartCoroutine(owner.CameraController.Routine_LookAtPosition(owner.transform.position, 0.5f));

            owner.DeepSeaPlayerMove.OnGrabQTESuccess += OnPlayerQTESuccess;
            owner.DeepSeaPlayerMove.OnGrabQTEFailed += OnPlayerQTEFailed;

            // 플레이어를 붙잡힘 상태로 만듦 -> 플레이어에서 QTE 로직 처리
            owner.DeepSeaPlayerMove.StartGrabQTE();
        }

        public void Update(DeepSeaMonsterController owner)
        {
            Debug.Log(owner.playerGrabPos.position);
        }

        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            Vector3 targetChargePos = owner.Rb.position + Vector3.down * dragDownSpeed * Time.fixedDeltaTime;
            owner.Rb.MovePosition(targetChargePos);
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            owner.DeepSeaPlayerMove.CanMove = true; // 플레이어 이동 가능
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