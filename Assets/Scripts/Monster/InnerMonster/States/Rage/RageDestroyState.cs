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
        private float _destroyTime; // 파괴하는데 걸리는 시간
        private float _timer; // 타이머

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ChangeMonsterModelCenter(true); // 몬스터 모델 중심 변경

            owner.currentAttackType = EAttackType.RageDestroyAttack; // 현재 공격 타입 -> 폭주 파괴 공격
            owner.Animator.SetBool("isRageDestroying", true); // 폭주 파괴 애니메이션 재생
            AudioManager.Instance.PlayGlobalOneShot(owner.destroyRageSound);
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
                case EDestroyObjType.MachinarySpaceDoor:
                    _destroyTime = 60f;
                    break;
                case EDestroyObjType.Equipment:
                    _destroyTime = 3.5f;
                    break;
            }

            // 이벤트 구독
            InnerMonsterController.OnRageStartAnimationEnded += PlayDestroyingSound; // 폭주 시작 애니메이션 종료 -> 파괴 중 사운드 재생
        }

        public void Update(InnerMonsterController owner)
        {
            if (owner.currentDestroyObj != null)
            {
                owner.LookAtTarget(owner.currentDestroyObjPos); // 현재 파괴 오브젝트를 바라보도록 회전

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
            owner.StopPlaying();
            owner.currentDestroyObjType = 0; // 현재 파괴해야할 오브젝트 타입 0으로 초기화
            owner.Nav.updateRotation = true; // 회전 자동으로 변경

            // 이벤트 구독 해제
            InnerMonsterController.OnRageStartAnimationEnded -= PlayDestroyingSound;
        }

        private void PlayDestroyingSound(InnerMonsterController monster)
        {
            AudioManager.Instance.PlaySoundSafe(monster.audioSource, monster.destroyingSound);
        }

        /// <summary>
        /// 폭주 파괴
        /// </summary>
        private void RageDestroy(InnerMonsterController monster)
        {
            AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound);
            // 타입에 따른 후처리
            switch (monster.currentDestroyObjType)
            {
                case EDestroyObjType.Door: // 일반 문을 파괴한 경우
                    monster.currentDestroyObj.SetActive(false); // 파괴 -> 현재는 비활성화
                    ResetCurrentDestroyObj(monster); // 현재 파괴해야 할 오브젝트 리셋
                    monster.ChangeState(new RageChaseState()); // 폭주 추적 상태로 전환(아직 경보 발생 중이기 때문)
                    break;

                case EDestroyObjType.EscapeRoomDoor: // 탈출실 문을 파괴한 경우
                    monster.currentDestroyObj.SetActive(false); // 파괴 -> 현재는 비활성화
                    ResetCurrentDestroyObj(monster); // 현재 파괴해야 할 오브젝트 리셋
                    FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence);
                    // 탈출실 문 파괴하고 괴물이 들어가서 포효하고 끝나는 연출 추가 예정!!!
                    GameManager.instance.GameOver(EEndingType.MonsterDeath); // 게임 오버 (탈출실 문이 파괴되었으므로 탈출 불가, 일단 괴물에게 죽은 엔딩으로 설정)
                    break;

                case EDestroyObjType.MachinarySpaceDoor: // 기계실 문을 파괴한 경우
                    monster.currentDestroyObj.SetActive(false); // 파괴 -> 현재는 비활성화
                    ResetCurrentDestroyObj(monster); // 현재 파괴해야 할 오브젝트 리셋
                    if (SubmarineInGameManager.instance.CurrentAlertArea == AlertArea.MachinerySpace) // 현재 기계실 문 경보가 발생 중일 때만 -> 게임 오버
                    {
                        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence);
                        // 괴물을 바라보고 괴물에게 공격당해 죽는 연출 추가 예정!!!
                        GameManager.instance.GameOver(EEndingType.MonsterDeath); // 게임 오버
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
                            // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 식당의 가스레인지를 부술 때 기름이 쏟아지며 폭발하는 모습만 딱 연출로 보여주면서 게임 오버
                            break;
                        case AlertArea.Storage01:
                            // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 창고01의 샘플병을 부수며 깨지는 소리나는 모습만 딱 연출로 잠깐 보여주고 다시 플레이어 카메라로 돌아옴
                            break;
                        case AlertArea.EngineRoom:
                            // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 엔진실의 엔진을 부수면서 파지직 소리와 함께 불이 다 꺼지는 걸 연출로 보여주고 다시 플레이어 카메라로 돌아옴
                            break;
                        case AlertArea.ControlRoom:
                            switch (SubmarineInGameManager.instance.CurrentDestroyEquipmentIndex)
                            {
                                case 0: // 레이더 조작 패널
                                    // 파괴 효과 연출 필요
                                    monster.currentDestroyObj.GetComponent<RadarControlPanel>().Broke(); // 고장
                                    break;
                                case 1: // 어뢰 자동 탑재 스위치
                                    // 스위치 off
                                    monster.currentDestroyObj.GetComponent<TorpedoAutoLoadSwitch>().SwitchOff();
                                    break;
                                case 2: // 탈출실 유압 패널
                                    monster.currentDestroyObj.GetComponent<EscapeRoomHydraulicSystemPanel>().Broke(); // 고장
                                    break;
                                case 3: // 통신 장치
                                    // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 조종실의 통신 장비를 부수면서 삐삐삐— 소리와 함께 망가지고 통신기가 검정색으로 되는 모습을 연출로 보여주고 다시 플레이어 카메라로 돌아옴
                                    break;
                            }
                            break;
                        case AlertArea.TorpedoRoom:
                            // 연출 X
                            break;
                    }
                    ResetCurrentDestroyObj(monster);
                    SubmarineInGameManager.instance.AlertOff(); // 경보 해제
                    monster.monsterEyeRenderer.material = monster.originalEyeMaterial; // 내부 괴물의 눈 머티리얼 원래 머티리얼(하얀색)로 변경

                    // 모든 문 NavMeshObstacle 다시 활성화하고, 한 프레임 대기 후(버그 해결 목적) 순찰 상태로 전환
                    monster.StartCoroutine(DoorObstaclesOnAndGoPatrolState(monster)); // 상태에는 Monobehaviour 없으므로 주체인 monster에서 실행
                    break;
            }
        }

        /// <summary>
        /// 현재 파괴해야 할 오브젝트 리셋
        /// </summary>
        private void ResetCurrentDestroyObj(InnerMonsterController monster)
        {
            Debug.Log(monster.currentDestroyObj + "을(를) 파괴했습니다.");
            monster.currentDestroyObj = null; // 현재 파괴해야 할 오브젝트 없음으로 설정
            monster.currentDestroyObjType = EDestroyObjType.None;
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

            yield return monster.WaitUntilNotBeingExtracted; // 추출 당하는 중일 때는 대기

            // 순찰 상태로 전환
            monster.ChangeState(new PatrolState());
        }

        private void CompleteDestroyingEscapeRoomDoor()
        {
            Sequence seq = DOTween.Sequence();

        }
    }
}