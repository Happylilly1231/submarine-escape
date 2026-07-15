using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 폭주 공격 상태 
    /// <para>- 폭주 공격 애니메이션이 진행된다.</para>
    /// <para>- 실제 폭주 공격은 애니메이션 이벤트로 실행된다.(InnerMonsterController 코드)</para>
    /// </summary>
    public class RageAttackState : IState<InnerMonsterController>
    {
        private bool isSuccess = false;

        InnerMonsterController monster;

        public void Enter(InnerMonsterController owner)
        {
            monster = owner;

            owner.CanMove(false); // 이동 정지
            AudioManager.Instance.PlayGlobalOneShot(owner.rageAttackSound);
            owner.ChangeFovCenter(true); // 시야각 중심 위치를 그냥 트랜스폼으로 변경
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ChangeMonsterModelCenter(true); // 몬스터 모델 중심 변경

            // 점프스케어에서 공격하는 경우 -> 
            if (owner.IsShowingJumpscare)
            {
                owner.currentAttackType = EAttackType.JumpscareRageAttack; // 현재 공격 타입 -> 점프스케어 폭주 공격
                owner.Animator.Play("Rage StateMachine.RageAttack"); // 폭주 공격 애니메이션 재생

                // Door.OnDoorClosed += ChangeStateToRageStart;

                // 점프스케어 QTE 시작
                StartJumpscareQTE();
            }
            else
            {
                owner.currentAttackType = EAttackType.RageAttack; // 현재 공격 타입 -> 폭주 공격
                owner.Animator.SetTrigger("RageAttack"); // 폭주 공격 애니메이션 재생
            }
        }

        public void Update(InnerMonsterController owner)
        {
            // 공격 도중 플레이어 완전 괴물화 시 -> 바로 폭주 추적 상태로 전환 
            if (owner.IsPlayerMutationCompeleted)
            {
                owner.Animator.Play("RageChase");
                owner.ChangeState(new RageChaseState());
                // owner.Animator.Play("Idle");
                // owner.ChangeState(new IdleState());
                return;
            }

            if (!owner.IsShowingJumpscare)
                owner.LookAtTarget(owner.PlayerTransform.position); // 현재 플레이어 위치를 바라보도록 회전
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.ChangeFovCenter(false); // 시야각 중심 위치를 머리 위치로 변경
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
            owner.StopPlaying();

            if (owner.IsShowingJumpscare)
            {
                // Door.OnDoorClosed -= ChangeStateToRageStart;
                owner.IsShowingJumpscare = false;
            }
        }

        /// <summary>
        /// 점프스케어 QTE 시작 함수
        /// </summary>
        public void StartJumpscareQTE()
        {
            monster.StartCoroutine(QTECheckSequence());
        }

        /// <summary>
        /// 2초 안에 E를 누르는지 QTE 검사 코루틴
        /// </summary>
        /// <returns></returns>
        private IEnumerator QTECheckSequence()
        {
            float durationLimit = 2.0f; // 현실 시간 2초 제한
            float timer = 0f;
            bool isSuccess = false;

            // 슬로우 모션 적용 (게임 내 속도는 0.3배로 느려짐)
            Time.timeScale = 0.3f;

            // UI 활성화
            monster.jumpscareQTEUI.SetActive(true);

            // 루프 시작
            while (timer < durationLimit)
            {
                // timescale 영향 없이 '실제 현실 시간' 누적
                timer += Time.unscaledDeltaTime;

                // UI의 360도 원 게이지를 현실 시간 2초에 맞춰 줄여나감
                monster.gaugeImage.fillAmount = Mathf.Clamp01(1f - (timer / durationLimit));

                // E 키 입력 감지
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    isSuccess = true;
                    break; // E키를 누르면 루프 즉시 탈출
                }

                yield return null; // 다음 프레임까지 대기
            }

            // --- QTE 검사 종료 시점 ---

            // 타임스케일 정상화 및 UI 비활성화
            Time.timeScale = 1f;
            monster.jumpscareQTEUI.SetActive(false);

            monster.IsJumpscareAttackSuccess = !isSuccess;

            if (isSuccess)
            {
                // [성공] 문이 자동으로 닫히는 연출 및 안전 상태로 복귀
                HandleQTESuccess();
            }
            else
            {
                // [실패] 그대로 괴물이 덮치는 사망 연출 진행
                HandleQTEFailure();
            }
        }

        /// <summary>
        /// QTE 성공
        /// </summary>
        private void HandleQTESuccess()
        {
            Debug.Log("QTE 성공! 문이 쾅 닫힙니다.");

            PlayerManager.Instance.SetPlayerGeoActive(true); // 플레이어 활성화
            monster.machinarySpaceDoor.CloseDoor(); // 기계실 문 자동으로 닫기
            // 쾅 소리 재생?
            FocusManager.Instance.PopFocusState(); // 포커스 해제

            DOTween.To(() => monster.jumpscareVolume.weight, x => monster.jumpscareVolume.weight = x, 0f, 0.2f);
        }

        /// <summary>
        /// QTE 실패
        /// </summary>
        private void HandleQTEFailure()
        {
            Debug.Log("QTE 실패... 플레이어가 습격당합니다.");
            // 그대로 폭주 공격이 진행되므로 (OnAttack) 죽게 됨
        }

        //     /// <summary>
        //     /// 폭주 시작 상태로 전환
        //     /// </summary>
        //     /// <param name="door"></param>
        //     /// <param name="monster"></param>
        //     private void ChangeStateToRageStart(Door door)
        //     {
        //         if (door == monster.machinarySpaceDoor)
        //             monster.ChangeState(new RageStartState()); // 폭주 시작 상태로 전환
        //     }
    }
}