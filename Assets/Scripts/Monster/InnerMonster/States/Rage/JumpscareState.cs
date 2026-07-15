using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace InnerMonsterStates
{
    public class JumpscareState : IState<InnerMonsterController>
    {
        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(false); // 이동 정지
            owner.ChangeMonsterModelCenter(false); // 몬스터 모델 중심 기본으로 돌림
            owner.Nav.updateRotation = false; // 회전 수동으로 변경

            owner.IsShowingJumpscare = true;

            FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // (플레이어 정지)

            PlayerManager.Instance.playerMove.PlayerTeleport(owner.machinarySpaceAlertPlayerPos.position, owner.machinarySpaceAlertPlayerPos.rotation); // 플레이어를 문 앞으로 위치 보정 (순간이동) (카메라 컨트롤러 켜짐)
            PlayerManager.Instance.SetCameraControllerEnable(true); // 카메라 컨트롤러 활성화

            owner.Nav.Warp(owner.machinarySpaceAlertMonsterPos.position); // 괴물이 어뢰실 문쪽(기계실 문 앞보다 훨씬 떨어지게)으로 순간이동
            owner.transform.rotation = owner.machinarySpaceAlertMonsterPos.rotation; // 괴물 회전

            AudioManager.Instance.PlayGlobalOneShot(owner.rageStartSound); // 무서운 소리 재생

            // 카메라 이동 전 플레이어가 카메라 뚫지 않게 플레이어 외형 비활성화
            PlayerManager.Instance.SetPlayerGeoActive(false);

            // 애니메이션 JumpscareStart 트리거 실행 -> 점프스케어 애니메이션 실행
            owner.Animator.SetTrigger("JumpscareStart");

            DOTween.To(() => owner.jumpscareVolume.weight, x => owner.jumpscareVolume.weight = x, 1f, 0.2f);

            // 연출 시작
            Sequence seq = DOTween.Sequence();

            // 플레이어 정면 문 보여주기
            seq.AppendInterval(0.5f);

            seq.AppendCallback(() =>
            {
                PlayerManager.Instance.SetCameraControllerEnable(false); // 카메라 컨트롤러 비활성화
            });

            // // 카메라가 괴물 얼굴 빡 줌인하도록 이동 (돌아보면서 얼굴쪽으로 갈 것)
            // seq.Append(Camera.main.transform.DOMove(owner.jumpscareZoomInPos.position, 1.5f)
            //     .SetEase(Ease.OutQuad));
            // seq.Join(Camera.main.transform.DORotateQuaternion(owner.jumpscareZoomInPos.rotation, 1.5f)
            //     .SetEase(Ease.OutQuad));

            seq.AppendCallback(() =>
            {
                // 이때 플레이어 위치를 기계실 안으로 순간이동 (카메라에는 안 나옴)
                PlayerManager.Instance.playerMove.PlayerTeleport(owner.machinarySpaceInnerPos.position, owner.machinarySpaceInnerPos.rotation);

                // owner.Animator.SetTrigger("JumpscareStart");
                owner.StartCoroutine(CameraTrackSequence(owner, owner.jumpscareZoomInPos, 2f)); // 괴물 얼굴 카메라가 따라가도록 하기
            });

            // 1초 동안 괴물 얼굴 보여주기
            seq.AppendInterval(2f);

            // 카메라 문틀이 보일 때까지 이동 & 괴물이 앞으로 다가옴
            seq.Append(Camera.main.transform.DOMove(owner.jumpscareZoomOutPos.position, 1f)
                .SetEase(Ease.OutQuad));
            seq.JoinCallback(() =>
            {
                owner.Nav.enabled = false;
                owner.transform.DOMove(owner.monsterApproachPos.position, 1f);
                owner.Nav.enabled = true;
                // owner.CanMove(true); // 이동
                // owner.Nav.speed = _approachSpeed; // 폭주 속도로 변경
                // owner.Nav.SetDestination(owner.monsterApproachPos.position);
            });

            // 완료 -> 폭주 공격 상태로 전환
            seq.OnComplete(() =>
            {
                owner.ChangeState(new RageAttackState());
            });
        }

        public void Update(InnerMonsterController owner)
        {

        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
        }

        private IEnumerator CameraTrackSequence(InnerMonsterController monster, Transform targetTrans, float duration)
        {
            Transform cameraTrans = Camera.main.transform;

            float timer = 0f;
            Vector3 velocity = Vector3.zero; // SmoothDamp용 가속도 버퍼
            float smoothTime = 0.2f; // 카메라가 대상을 쫓아가는 부드러운 시간 (낮을수록 칼같이 쫓아감)

            while (timer < duration)
            {
                timer += Time.deltaTime;

                // 1. [실시간 위치 추적] targetTrans가 움직여도 실시간으로 쫓아갑니다.
                cameraTrans.position = Vector3.SmoothDamp(
                    cameraTrans.position,
                    targetTrans.position,
                    ref velocity,
                    smoothTime,
                    Mathf.Infinity,
                    Time.deltaTime
                );

                // 2. [실시간 회전 추적] 회전값도 부드럽게 실시간으로 쫓아갑니다.
                cameraTrans.rotation = Quaternion.Slerp(
                    cameraTrans.rotation,
                    targetTrans.rotation,
                    10f * Time.deltaTime
                );

                yield return null;
            }
        }
    }
}