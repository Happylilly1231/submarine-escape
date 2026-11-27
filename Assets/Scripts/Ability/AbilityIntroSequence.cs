using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AbilityIntroSequence : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private AbilityUI abilityUI;
    [SerializeField] private Image blackImage;
    [SerializeField] private Image vignetteImage;

    [Header("오디오")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip growlSound;
    [SerializeField] private AudioClip tinnitusSound;

    /// <summary>
    /// 능력 사용 게임 인트로 연출 재생
    /// </summary>
    public void PlayAbilityIntroSequence()
    {
        Debug.Log("능력 사용 게임에서의 인트로 연출 재생");

        // 초기 설정
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 연출 포커스로 설정 (입력 조작 불가)
        Vector3 startPos = PlayerManager.Instance.playerMoveBase.transform.position;
        startPos.y = 0f; // y좌표 0으로 해줌
        PlayerManager.Instance.playerMoveBase.transform.position = startPos;
        PlayerManager.Instance.playerCameraController.SetRotationInstant(-90f, 0f); // 카메라 즉시 플레이어 초기 위치 정면 바라보도록 적용 (카메라 조작 스크립트 비활성화 상태기 때문에)
        blackImage.gameObject.SetActive(true); // 암전 이미지 켜기
        vignetteImage.gameObject.SetActive(true); // 비네트 이미지 켜기
        vignetteImage.rectTransform.localScale = Vector3.one;

        // 연출 시작
        Sequence seq = DOTween.Sequence();

        // 일정 시간 삐 이명 소리 남
        audioSource.clip = tinnitusSound;
        audioSource.loop = false;
        audioSource.volume = 0f;
        audioSource.Play();
        seq.Append(audioSource.DOFade(0.7f, 0.5f));
        seq.AppendInterval(1f);

        // 삐 소리 줄어들기
        seq.Append(audioSource.DOFade(0f, 1f).SetEase(Ease.OutSine)); // 삐 소리가 1초간 사그라들음

        // 잠깐의 정적
        seq.AppendInterval(0.5f);

        // 눈을 좀 뜨면서 그르릉 소리가 커지다가
        seq.AppendCallback(() =>
        {
            // 그르릉 소리가 나기 시작
            audioSource.clip = growlSound;
            audioSource.loop = true;
            audioSource.Play();
        });
        seq.Append(audioSource.DOFade(0.5f, 2f).SetEase(Ease.InSine)); // 점점 커짐
        seq.Join(blackImage.DOFade(0f, 2f).SetEase(Ease.OutQuad)); // 암전 끄기
        seq.Join(vignetteImage.rectTransform.DOScale(new Vector3(3f, 3f, 1f), 2f).SetEase(Ease.InOutSine)); // 눈을 좀 뜨다가

        // 잠시 조금 눈을 감으면서 소리가 줄었다가
        seq.Append(audioSource.DOFade(0.3f, 1.5f).SetEase(Ease.InOutSine));
        seq.Join(blackImage.DOFade(1f, 1.5f).SetEase(Ease.InOutSine));
        seq.Join(vignetteImage.rectTransform.DOScale(new Vector3(1f, 1f, 1f), 1.5f).SetEase(Ease.InOutQuad));

        // 다시 눈 떴다가
        seq.Append(audioSource.DOFade(0.5f, 1.7f).SetEase(Ease.InSine)); // 점점 커짐
        seq.Join(blackImage.DOFade(0f, 1.7f).SetEase(Ease.OutQuad)); // 암전 끄기
        seq.Join(vignetteImage.rectTransform.DOScale(new Vector3(4f, 4f, 1f), 1.7f).SetEase(Ease.InOutSine)); // 눈을 좀 뜨다가

        // 잠시 조금 눈을 감으면서 소리가 줄었다가
        seq.Append(audioSource.DOFade(0.3f, 1.5f).SetEase(Ease.InOutSine));
        seq.Join(blackImage.DOFade(1f, 1.5f).SetEase(Ease.InOutSine));
        seq.Join(vignetteImage.rectTransform.DOScale(new Vector3(1f, 1f, 1f), 1.5f).SetEase(Ease.InOutQuad));

        // 다시 눈을 아예 뜨면서 소리가 커짐
        seq.Append(audioSource.DOFade(1f, 3f).SetEase(Ease.OutQuad));
        seq.Join(blackImage.DOFade(0f, 2f).SetEase(Ease.OutQuad));
        seq.Join(vignetteImage.rectTransform.DOScale(new Vector3(6f, 6f, 1f), 3f).SetEase(Ease.OutCubic));

        // 가시가 움직이고 있고, 괴물 자기 손 바라보는 애니메이션 실행 (X)
        // 아래 내려다봤다가 다시 정면 보기
        seq.AppendCallback(() =>
        {
            PlayerManager.Instance.SetCameraControllerEnable(true);
            PlayerManager.Instance.playerCameraController.RotateToYAngle(PlayerManager.Instance.playerMoveBase.transform.eulerAngles.y, 2f, 50f, Ease.InOutSine);
        });
        seq.AppendInterval(2f);
        seq.AppendCallback(() =>
        {
            PlayerManager.Instance.playerCameraController.RotateToYAngle(PlayerManager.Instance.playerMoveBase.transform.eulerAngles.y, 1.5f, 0f, Ease.InOutQuad);
        });
        seq.AppendInterval(1.5f);
        seq.Join(audioSource.DOFade(0f, 1f)); // 그르릉 소리 줄이고 끄기

        // 인트로 끝나면
        seq.OnComplete(() =>
        {
            // 초기화
            vignetteImage.rectTransform.localScale = Vector3.one;
            vignetteImage.gameObject.SetActive(false);
            blackImage.gameObject.SetActive(false);

            FocusManager.Instance.PopFocusState();

            // 능력 설명 UI 보여주기
            abilityUI.ShowAbilityInfoUI();
        });
    }
}
