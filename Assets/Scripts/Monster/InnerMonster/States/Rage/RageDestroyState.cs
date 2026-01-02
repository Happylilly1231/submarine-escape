using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 폭주 파괴 상태 
    /// <para>- 현재 파괴할 오브젝트를 일정 시간 동안 파괴한다.</para>
    /// <para><code>- 파괴 후 타입에 따른 후처리 로직을 진행한다.
    ///     - 일반 문 -> 폭주 추적 상태로 전환
    ///     - 탈출실 문 -> 연출 처리
    ///     - 현재 파괴해야 할 장비 -> 다음 장비로 설정, 경보 해제, 모든 문 Obstacle 활성화, 순찰 상태로 전환
    /// </code></para>
    /// </summary>
    public class RageDestroyState : IState<InnerMonsterController>
    {
        private float _destroyTime; // 파괴하는데 걸리는 시간
        private float _timer; // 타이머

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ColliderCenterChange(true); // 컨트롤러 중심 변경

            owner.currentAttackType = EAttackType.RageDestroyAttack; // 현재 공격 타입 -> 폭주 파괴 공격
            owner.Animator.SetBool("isRageDestroying", true); // 폭주 파괴 애니메이션 재생
            _timer = 0f; // 타이머 초기화

            // 파괴하는데 걸리는 시간 설정
            switch (owner.currentDestroyObjType)
            {
                case EDestroyObjType.Door:
                    _destroyTime = 7f;
                    break;
                case EDestroyObjType.EscapeRoomDoor:
                    _destroyTime = 30f;
                    break;
                case EDestroyObjType.CurrentDestroyEquipment:
                    _destroyTime = 3.5f;
                    break;
            }
        }

        public void Update(InnerMonsterController owner)
        {
            if (owner.currentDestroyObj != null)
            {
                owner.LookAtTarget(owner.currentDestroyObj.transform.position); // 현재 파괴 오브젝트를 바라보도록 회전

                _timer += Time.deltaTime;
                if (_timer > _destroyTime)
                {
                    RageDestroy(owner); // 폭주 파괴
                }
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Animator.SetBool("isRageDestroying", false);
            owner.currentDestroyObjType = 0; // 현재 파괴해야할 오브젝트 타입 0으로 초기화
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
        }

        /// <summary>
        /// 폭주 파괴
        /// </summary>
        private void RageDestroy(InnerMonsterController monster)
        {
            // 현재 파괴해야 할 오브젝트 파괴
            DestroyCurrentDestroyObj(monster);

            // 타입에 따른 후처리
            switch (monster.currentDestroyObjType)
            {
                case EDestroyObjType.Door: // 일반 문을 파괴한 경우
                    monster.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환(아직 경보 발생 중이기 때문)
                    break;

                case EDestroyObjType.EscapeRoomDoor: // 탈출실 문을 파괴한 경우
                    // 경보 끌 수 없으므로 끄지 않음
                    // 이후 탈출실에서 플레이어가 괴물에게 죽는 장면이 연출 처리될 것
                    break;

                case EDestroyObjType.CurrentDestroyEquipment: // 현재 파괴될 장비를 파괴한 경우
                    SubmarineInGameManager.instance.currentDestroyEquipmentIndex += 1; // 현재 파괴될 장비 인덱스 1 증가
                    SubmarineInGameManager.instance.AlertOff(); // 경보 해제
                    monster.monsterEyeRenderer.material = monster.originalEyeMaterial; // 내부 괴물의 눈 머티리얼 원래 머티리얼(하얀색)로 변경

                    // 모든 문 NavMeshObstacle 다시 활성화하고, 한 프레임 대기 후(버그 해결 목적) 순찰 상태로 전환
                    monster.StartCoroutine(DoorObstaclesOnAndGoPatrolState(monster)); // 상태에는 Monobehaviour 없으므로 주체인 monster에서 실행
                    break;

            }
        }

        /// <summary>
        /// 현재 파괴해야 할 오브젝트 파괴
        /// </summary>
        private void DestroyCurrentDestroyObj(InnerMonsterController monster)
        {
            Debug.Log(monster.currentDestroyObj + "을(를) 파괴했습니다.");
            monster.currentDestroyObj.SetActive(false); // 임시 - 비활성화
            monster.currentDestroyObj = null; // 현재 파괴해야 할 오브젝트 없음으로 설정
            monster.Animator.SetBool("isRageDestroying", false);
        }

        /// <summary>
        /// 모든 문 NavMeshObstacle 다시 활성화하고, 한 프레임 대기 후 순찰 상태로 전환하는 코루틴
        /// </summary>
        IEnumerator DoorObstaclesOnAndGoPatrolState(InnerMonsterController monster)
        {
            // 모든 문 NavMeshObstacle 다시 활성화
            foreach (Door door in SubmarineInGameManager.instance.Doors)
            {
                if (door.gameObject.activeSelf)
                    door.gameObject.GetComponent<NavMeshObstacle>().enabled = true;
            }

            monster.Animator.SetBool("isRageEnd", true); // 폭주 서브 스테이트머신 종료

            yield return null; // 한 프레임 대기 <- 문 NavMeshObstacle 활성화와 순찰 상태에서 경로 계산이 똑같은 프레임에 일어날 경우 Obstacle 인식 제대로 안되는 버그 해결

            // 순찰 상태로 전환
            monster.ChangeState(new PatrolState());
        }
    }
}