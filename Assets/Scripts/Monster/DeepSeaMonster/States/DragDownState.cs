using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class DragDownState : IState<DeepSeaMonsterController>
    {
        private DeepSeaMonsterController _monster;
        private UnderwaterCameraController underwaterCameraController;

        private float dragDownSpeed = 4f;
        private Rigidbody playerRb = null;

        // 붙잡힘
        private float _qteTimeLimit = 3f; // QTE 제한 시간
        public int TargetSpaceCount { get; set; } = 10; // 필요한 Space 연타 횟수
        private int _currentSpaceCount = 0; // 현재 스페이스 개수
        private float _qteTimer = 0f; // 현재 QTE 타이머

        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(true);
            _monster = owner;

            owner.underwaterPlayerMove.SetMoveable(false);
            if (underwaterCameraController == null)
                underwaterCameraController = PlayerManager.Instance.playerCameraControllerBase as UnderwaterCameraController;
            underwaterCameraController.SetInputEnabled(false);
            underwaterCameraController.LookDown(); // 아래 바라보는 카메라 회전

            owner.transform.position = owner.playerGrabPos.position;
            owner.transform.rotation = owner.playerGrabPos.rotation; // 괴물이 플레이어 바라보도록 회전
            Debug.Log(owner.playerGrabPos.position);

            // // 괴물이 플레이어를 바라보는 수평 방향 계산
            // Vector3 targetPosition = owner.transform.position + Vector3.up * 5f;
            // Vector3 dirToPlayer = owner.PlayerTransform.position - owner.transform.position;
            // dirToPlayer.y = 0;
            // dirToPlayer.Normalize();
            // // 위를 쳐다보되(targetPosition), 머리/등 방향은 플레이어를 마주보는 방향(-dirToPlayer)으로 설정
            // if (dirToPlayer != Vector3.zero)
            // {
            //     owner.transform.LookAt(targetPosition, -dirToPlayer);
            // }

            // Space(위로 대시) 키 입력 이벤트 구독
            owner.underwaterInput.upwardDashEvent += OnUpwardDash;

            playerRb = owner.PlayerTransform.GetComponent<Rigidbody>();

            StartGrabQTE();
        }

        public void Update(DeepSeaMonsterController owner)
        {
            HandleGrabQTE();
        }

        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            Vector3 movement = Vector3.down * dragDownSpeed * Time.fixedDeltaTime;
            owner.Rb.MovePosition(owner.Rb.position + movement);
            playerRb.MovePosition(playerRb.position + movement);
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            owner.underwaterPlayerMove.SetMoveable(true); // 플레이어 이동 가능
            underwaterCameraController.SetInputEnabled(true); // 카메라 조작 가능

            _monster.dragDownUI.SetActive(false); // UI 비활성화

            owner.underwaterInput.upwardDashEvent -= OnUpwardDash; // 입력 이벤트 구독 해제

            // 최종 패턴 예약 중 -> 현재 상태가 끝났으므로 최종 패턴 실행
            if (owner.IsFinalPatternPending)
            {
                owner.StartFinalPattern();
            }
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

        #region 붙잡힘 (탈출 QTE)

        /// <summary>
        /// 괴물이 플레이어를 붙잡았을 때 호출 -> 탈출 위한 QTE 시작
        /// </summary>
        public void StartGrabQTE()
        {
            _currentSpaceCount = 0;
            _qteTimer = 0f;

            _monster.dragDownUI.SetActive(true); // UI 활성화
            _monster.leftTimeImage.fillAmount = 1f;
            _monster.spaceCountText.text = "0";
            _monster.targetSpaceCountText.text = TargetSpaceCount.ToString();
        }

        private void OnUpwardDash()
        {
            _currentSpaceCount++; // 눌렀으므로 카운트 증가
            _monster.spaceCountText.text = $"{_currentSpaceCount}";
            Debug.Log("스페이스 횟수: " + _currentSpaceCount + " / 남은 시간: " + (_qteTimeLimit - _qteTimer));

            // 제한 시간 내 목표 횟수 달성 -> 성공
            if (_currentSpaceCount >= TargetSpaceCount)
            {
                OnPlayerQTESuccess();
                return;
            }
        }

        /// <summary>
        /// 붙잡혔을 때 탈출하는 QTE 입력 받기
        /// </summary>
        private void HandleGrabQTE()
        {
            // 타이머 증가
            _qteTimer += Time.deltaTime;
            _monster.leftTimeImage.fillAmount = Mathf.Clamp01((_qteTimeLimit - _qteTimer) / _qteTimeLimit);

            // 제한 시간 초과 -> 실패
            if (_qteTimer >= _qteTimeLimit)
            {
                OnPlayerQTEFailed();
            }
        }

        #endregion
    }
}