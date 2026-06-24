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
        // 게임 초기 설정
        SubmarineInGameManager.instance.InitGame();

        // 세이브 로드 상태라면
        if (SaveSystemManager.Instance != null && SaveSystemManager.IsLoadGameMode)
        {
            SkipIntroSequenceRoutine();
        }
        else
        {
            // 새 게임인 경우에만 인트로 연출 시작
            StartCoroutine(PlayIntroSequence());
        }
    }

    IEnumerator PlayIntroSequence()
    {
        SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화

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

        FinalizeIntroState();
    }

    /// <summary>
    /// 세이브 로드 시 연출을 스킵하고 인게임 상태로 즉시 전환
    /// </summary>
    private void SkipIntroSequenceRoutine()
    {
        Debug.Log("[인트로 스킵] 세이브 기점 로드로 인해 인트로 연출 건너뛰기 프로세스 시작.");

        // 위치 이동을 방해하는 컴포넌트들을 잠시 봉인
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 애니메이터가 강제로 위치를 비트는 것을 방지 (루트 모션 일시 해제)
        bool originalRootMotion = _animator.applyRootMotion;
        _animator.applyRootMotion = false;

        // 안전해진 타이밍에 세이브 좌표 주입
        if (SaveSystemManager.Instance != null)
        {
            var saveData = SaveSystemManager.Instance.GetCurrentSavePointData();
            if (saveData != null)
            {
                Vector3 targetPos = saveData.playerPosition.ToVector3();

                // 좌표 대입 및 리지드바디 관성 초기화
                transform.position = targetPos;
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // 유니티 물리 트랜스폼 강제 동기화
                Physics.SyncTransforms();
                Debug.Log($"[인트로 스킵] 플레이어 위치를 세이브 포인트로 강제 이동 완료: {targetPos}");
            }
        }

        // 인게임 UI 및 카메라 상태 정상화
        FinalizeIntroState();

        if (cc != null) cc.enabled = true;
        _animator.applyRootMotion = originalRootMotion;

        Debug.Log("[인트로 스킵] 모든 컴포넌트 복구 완료. 정상 게임 플레이 가능 상태입니다.");
    }

    /// <summary>
    /// 인트로가 끝나거나 스킵되었을 때 공통적으로 처리해야 하는 인게임 정상화 로직
    /// </summary>
    private void FinalizeIntroState()
    {
        if (playerCameraController != null)
            playerCameraController.IsIntroPlaying = false; // 카메라 컨트롤러에 인트로 시퀀스 종료 알림

        _animator.speed = 1f;

        // 레이어 교체
        _animator.SetLayerWeight(0, 0f); // 인트로 레이어 끄기
        _animator.SetLayerWeight(1, 1f); // 플레이어 레이어 켜기

        // 게임 재개 및 UI 표시
        SubmarineInGameManager.instance.Resume();
        SubmarineInGameManager.instance.SetActiveInGameUI(true); // 인게임 UI 활성화
    }
}
