using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        private float _needDestroyAttackCount; // 필요한 파괴 공격 수
        // private bool _isSequencing = false; // 연출 보여주는 중인지 여부
        private bool _isDestroying = false;

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ChangeMonsterModelCenter(true); // 몬스터 모델 중심 변경

            owner.currentAttackType = EAttackType.RageDestroyAttack; // 현재 공격 타입 -> 폭주 파괴 공격
            owner.Animator.SetBool("isRageDestroying", true); // 폭주 파괴 애니메이션 재생
            AudioManager.Instance.PlayGlobalOneShot(owner.destroyRageSound);

            if (owner.IsDestroyImmediately) // 즉시 파괴 경우 -> 즉시 파괴
            {
                Debug.Log("즉시 파괴");
                _needDestroyAttackCount = 0;
            }
            else // 즉시 파괴 X
            {
                // 파괴하는데 걸리는 시간 설정 & 문은 빨간색으로 변경
                switch (owner.currentDestroyObjType)
                {
                    case EDestroyObjType.Door:
                        _needDestroyAttackCount = 2;
                        owner.currentDestroyObj.gameObject.GetComponent<Renderer>().material.color = Color.red; // 문 빨간색으로 표시
                        break;
                    case EDestroyObjType.EscapeRoomDoor:
                        _needDestroyAttackCount = 10;
                        owner.currentDestroyObj.gameObject.GetComponent<Renderer>().material.color = Color.red; // 문 빨간색으로 표시
                        break;
                    case EDestroyObjType.MachinerySpaceDoor:
                        _needDestroyAttackCount = 15;
                        owner.currentDestroyObj.gameObject.GetComponent<Renderer>().material.color = Color.red; // 문 빨간색으로 표시
                        break;
                    case EDestroyObjType.Equipment:
                        if (SubmarineInGameManager.instance.CurrentAlertArea == AlertArea.TorpedoRoom) // 어뢰실 장치의 경우 늦게 부서지게 함
                        {
                            _needDestroyAttackCount = 7;
                        }
                        else
                        {
                            _needDestroyAttackCount = 1;
                        }
                        break;
                }
            }

            // 이벤트 구독
            InnerMonsterController.OnAnimationRageDestroyAttackTouched += PlayDestroyingSound;
        }

        public void Update(InnerMonsterController owner)
        {
            if (owner.IsDestroySequencing || _isDestroying) return;

            if (owner.currentDestroyObj != null)
            {
                owner.LookAtTarget(owner.currentDestroyObjPos); // 현재 파괴 오브젝트를 바라보도록 회전

                if (owner.currentDestroyAttackCount == _needDestroyAttackCount)
                {
                    // 탈출 연출 중 괴물이 탈출실 문을 부수는 것을 방지하기 위해
                    // 탈출 연출 재생 중에는 괴물이 탈출실 문을 파괴할 수 없게 막음
                    if (SubmarineInGameManager.instance.IsEscaped) return;
                    RageDestroy(owner); // 폭주 파괴
                }
            }

            switch (owner.currentDestroyObjType)
            {
                case EDestroyObjType.Door:
                    if (owner.currentDestroyDoor.isOpened) // 파괴 중이었는데 문이 열리면
                    {

                        owner.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환(아직 경보 발생 중이기 때문)
                    }
                    break;
                case EDestroyObjType.EscapeRoomDoor:
                    if (owner.currentDestroyDoor.isOpened) // 파괴 중이었는데 문이 열리면
                    {
                        Sequence_EnterMonsterEscapeRoom(owner); // 진입 연출 재생
                    }

                    break;
                case EDestroyObjType.MachinerySpaceDoor:
                    if (owner.currentDestroyDoor.isOpened) // 파괴 중이었는데 문이 열리면
                    {
                        owner.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환(아직 경보 발생 중이기 때문)
                    }
                    break;
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            if (owner.currentDestroyObj != null)
            {
                // 문 빨간색인 거 원래 색으로 변경
                switch (owner.currentDestroyObjType)
                {
                    case EDestroyObjType.Door:
                    case EDestroyObjType.EscapeRoomDoor:
                    case EDestroyObjType.MachinerySpaceDoor:
                        owner.currentDestroyObj.gameObject.GetComponent<Renderer>().material.color = Color.white; // 문 원래색으로 변경
                        break;
                }
            }

            ResetCurrentDestroyObj(owner); // 현재 파괴 장치 초기화 (중간에 다른 상태로 전환된 거면 어차피 RageChaseState에서 넘어올 때 다시 currentDestroyObj 등 맞게 할당될 것)
            owner.StopPlaying();
            owner.Nav.updateRotation = true; // 회전 자동으로 변경

            // 이벤트 구독 해제
            InnerMonsterController.OnAnimationRageDestroyAttackTouched -= PlayDestroyingSound;
        }

        private void PlayDestroyingSound(InnerMonsterController monster)
        {
            AudioManager.Instance.PlayGlobalOneShot(monster.destroyingSound);
        }

        /// <summary>
        /// 폭주 파괴
        /// </summary>
        private void RageDestroy(InnerMonsterController monster)
        {
            _isDestroying = true;
            Debug.Log(monster.currentDestroyObjType + " / " + SubmarineInGameManager.instance.CurrentAlertArea);
            // 타입에 따른 후처리
            switch (monster.currentDestroyObjType)
            {
                case EDestroyObjType.Door: // 일반 문을 파괴한 경우
                    monster.currentDestroyObj.SetActive(false); // 파괴 -> 현재는 비활성화
                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                    monster.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환(아직 경보 발생 중이기 때문)
                    break;

                case EDestroyObjType.EscapeRoomDoor: // 탈출실 문을 파괴한 경우
                    monster.currentDestroyObj.SetActive(false); // 파괴 -> 현재는 비활성화
                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                    // 연출
                    Sequence_EnterMonsterEscapeRoom(monster);
                    return;

                case EDestroyObjType.MachinerySpaceDoor: // 기계실 문을 파괴한 경우
                    monster.currentDestroyObj.SetActive(false); // 파괴 -> 현재는 비활성화
                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                    if (SubmarineInGameManager.instance.CurrentAlertArea == AlertArea.MachinerySpace) // 현재 기계실 문 경보가 발생 중일 때만 -> 게임 오버
                    {
                        // 연출
                        Sequence_EnterMonsterMachinarySpace(monster);
                        return;
                    }
                    else // 기계실 문 경보 발생 중 X -> 일반 문 파괴 경우와 동일
                    {
                        monster.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환(아직 경보 발생 중이기 때문)
                    }
                    break;

                case EDestroyObjType.Equipment: // 현재 파괴될 장비를 파괴한 경우
                    switch (SubmarineInGameManager.instance.CurrentAlertArea)
                    {
                        case AlertArea.Galley:
                            // 연출
                            monster.destroySequencer.DestroySequence(monster, () =>
                            {
                                GameManager.instance.GameOver(EEndingType.SubmarineExplode); // 게임 오버
                            });
                            return; // 게임 오버이므로 즉시 종료
                        case AlertArea.Storage01:
                        case AlertArea.EngineRoom:
                            // 연출
                            monster.destroySequencer.DestroySequence(monster, () =>
                            {
                                CompleteDestroyEquipmentNotGameOver(monster);
                            });
                            break;
                        case AlertArea.ControlRoom:
                            switch (SubmarineInGameManager.instance.CurrentDestroyEquipmentIndex)
                            {
                                case 0: // 레이더 조작 패널
                                    // 파괴 효과 연출 필요
                                    ObjectiveManager.Instance.UnlockObjective("FixTorpedoRadar"); // 1차 수리 해금
                                    ObjectiveManager.Instance.CompleteObjective("FireTorpedo"); // 어뢰 발사 클리어

                                    monster.currentDestroyObj.GetComponent<RadarControlPanel>().Broke(); // 고장
                                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                                    CompleteDestroyEquipmentNotGameOver(monster);
                                    break;
                                case 1: // 어뢰 자동 탑재 스위치
                                    // 스위치 off
                                    ObjectiveManager.Instance.UnlockObjective("LoadTorpedoTube"); // 2차 수리 해금
                                    ObjectiveManager.Instance.UpdateObjectiveUI();

                                    monster.currentDestroyObj.GetComponent<TorpedoAutoLoadSwitch>().SwitchOff();
                                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                                    CompleteDestroyEquipmentNotGameOver(monster);
                                    break;
                                case 2: // 탈출실 유압 패널
                                    ObjectiveManager.Instance.UnlockObjective("OperateHydraulicValve"); // 3차 수리 해금
                                    ObjectiveManager.Instance.UpdateObjectiveUI();

                                    monster.currentDestroyObj.GetComponent<EscapeRoomHydraulicSystemPanel>().Broke(); // 고장
                                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                                    CompleteDestroyEquipmentNotGameOver(monster);
                                    break;
                                case 3: // 통신 장치
                                    // 연출
                                    monster.destroySequencer.DestroySequence(monster, () =>
                                    {
                                        CompleteDestroyEquipmentNotGameOver(monster);
                                    });
                                    break;
                            }
                            break;
                        case AlertArea.TorpedoRoom:
                            // 연출 X
                            AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
                            monster.destroySequencer.alarmControlPanel.DeactivateAreaButton(SubmarineInGameManager.instance.CurrentAlertArea); // 경보 패널에서 어뢰실 버튼 선택 불가능으로 변경
                            CompleteDestroyEquipmentNotGameOver(monster);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// 게임 오버 아닐 때 장치 파괴 완료 시 로직
        /// </summary>
        public void CompleteDestroyEquipmentNotGameOver(InnerMonsterController monster)
        {
            Debug.Log(monster.currentDestroyObj + "을(를) 파괴했습니다.");
            ResetCurrentDestroyObj(monster); // 현재 파괴 오브젝트 초기화
            FocusManager.Instance.PopFocusState(); // 포커스 해제
            monster.IsDestroySequencing = false; // 파괴 연출 종료

            if (monster.IsDestroyImmediately) // 즉시 파괴 (아직 점프스퀘어 포커스 상태)
            {
                // 이제 파괴 연출 끝났으므로, 기계실로 현재 경보 위치 변경
                SubmarineInGameManager.instance.ChangeCurrentAlertPos(AlertArea.MachinerySpace);
                monster.DestroyDoorsAndTransitionToJumpscare(); // 현재 목적지로 가는 경로 상 문 즉시 파괴 후 점프스케어 상태로 전환

                // 즉시 파괴 여부 초기화
                monster.IsDestroyImmediately = false;
            }
            else // 즉시 파괴 아닌 경우
            {
                SubmarineInGameManager.instance.AlertOff(); // 경보 해제
                monster.monsterEyeRenderer.material = monster.originalEyeMaterial; // 내부 괴물의 눈 머티리얼 원래 머티리얼(하얀색)로 변경

                // 모든 문 NavMeshObstacle 다시 활성화하고, 한 프레임 대기 후(버그 해결 목적) 순찰 상태로 전환
                monster.StartCoroutine(DoorObstaclesOnAndGoPatrolState(monster)); // 상태에는 Monobehaviour 없으므로 주체인 monster에서 실행
            }
            _isDestroying = false;
        }

        /// <summary>
        /// 현재 파괴해야 할 오브젝트 리셋
        /// </summary>
        private void ResetCurrentDestroyObj(InnerMonsterController monster)
        {
            monster.currentDestroyObj = null; // 현재 파괴해야 할 오브젝트 없음으로 설정
            monster.currentDestroyDoor = null;
            monster.currentDestroyObjType = EDestroyObjType.None;
            monster.Animator.SetBool("isRageDestroying", false);
            monster.currentDestroyAttackCount = 0; // 현재 파괴 공격수 다시 0으로 초기화
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

            yield return monster.WaitUntilNotBeingExtracted; // 추출 당하는 중일 때는 대기

            // 순찰 상태로 전환
            monster.ChangeState(new PatrolState());
        }

        /// <summary>
        /// 탈출실에 괴물 진입 시 연출
        /// </summary>
        /// <param name="monster"></param>
        public void Sequence_EnterMonsterEscapeRoom(InnerMonsterController monster)
        {
            // 수압 밸브를 작동한 상태면
            if (monster.seaWaterValve.IsActivated)
            {
                // 바로 게임 오버
                GameManager.instance.GameOver(EEndingType.MonsterDeath); // 게임 오버 (탈출실 문이 파괴되었으므로 탈출 불가, 일단 괴물에게 죽은 엔딩으로 설정)
                return;
            }
            FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 포커스
            monster.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환
        }

        /// <summary>
        /// 기계실에 괴물 진입 시 연출
        /// </summary>
        /// <param name="monster"></param>
        public void Sequence_EnterMonsterMachinarySpace(InnerMonsterController monster)
        {
            FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 포커스
            monster.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환
        }
    }
}