using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class IntroSequencer : MonoBehaviour
{
    [SerializeField] private GameObject statUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject interactorUI;
    [SerializeField] private PlayerCameraController playerCameraController;

    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();

        if (playerCameraController != null)
            playerCameraController.IsIntroPlaying = true; // 카메라 컨트롤러에 인트로 시퀀스 재생 중임을 알림
    }

    void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        inventoryUI.SetActive(false); // 인벤토리 UI 숨기기
        statUI.SetActive(false); // 스탯 UI 숨기기
        interactorUI.SetActive(false); // 상호작용 UI 숨기기

        transform.position = new Vector3(9.2f, 0.12f, 4.5f);
        transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        SubmarineInGameManager.instance.IntroPause(); // 인트로 시퀀스 시작

        // 애니메이션이 끝날 때까지 대기
        yield return new WaitUntil(() =>
            _animator.GetCurrentAnimatorStateInfo(0).IsName("Sit To Stand") &&
            _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);

        yield return new WaitForSeconds(0.5f);

        transform.rotation = Quaternion.Euler(0, -180f, 0);

        Debug.Log("인트로 시퀀스 종료");

        if (playerCameraController != null)
            playerCameraController.IsIntroPlaying = false; // 카메라 컨트롤러에 인트로 시퀀스 종료 알림

        _animator.speed = 1f;

        // 레이어 교체
        _animator.SetLayerWeight(0, 0f); // 인트로 레이어 끄기
        _animator.SetLayerWeight(1, 1f); // 플레이어 레이어 켜기

        // 게임 재개 및 UI 표시
        SubmarineInGameManager.instance.Resume();
        inventoryUI.SetActive(true);
        statUI.SetActive(true);
        interactorUI.SetActive(true);
    }
}
