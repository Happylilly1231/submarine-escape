using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterRescueCutscene : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private DeepSeaCameraController cameraController;

    [Header("Obj")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject helicopter;
    [SerializeField] private GameObject ladder;

    [Header("Pos")]
    [SerializeField] private Transform ladderClimbStartPoint; // 사다리 등반이 시작하는 위치
    [SerializeField] private Transform ladderClimbTargetPoint; // 사다리 등반이 끝나는 위치
    [SerializeField] private Transform helicopterSeatPoint; // 헬기 내부 플레이어 착석 위치

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Canvas helmetCanvas;

    [Header("Audio")]
    [SerializeField] private AudioSource breathingAudio;
    [SerializeField] private AudioSource windAudio;
    [SerializeField] private AudioSource rotorAudio;

    public bool IsCutScene { get; private set; } = false;

    public void StartCutscene()
    {
        Debug.Log("헬리콥터 구조 컷씬 시작");

        IsCutScene = true;

        InputManager.instance.DisableAllInputs(); // 모든 인풋 비활성화

        helmetCanvas.enabled = false; // 헬멧 UI 비활성화

        StartCoroutine(StartSequence());
    }

    /// <summary>
    /// 로프와 상호작용했을 때 컷씬
    /// <para> - 사다리 등반 시 위쪽으로 시선 고정, 상하 움직임과 미세한 좌우 흔들림 </para>
    /// <para> - 거친 숨소리와 바람 소리 재생 </para>
    /// <para> - 화면 페이드 아웃 후 페이드 인, 사다리 비활성화 </para>
    /// <para> - 헬기 내부 시점으로 전환되며 이륙 카메라 흔들림 </para>
    /// </summary>
    private IEnumerator StartSequence()
    {
        // =====================================
        // 1. 사다리 등반
        // - 시선을 위쪽으로 회전
        // - 플레이어 몸을 사다리 방향으로 회전
        // - 카메라에 상하 움직임과 좌우 흔들림 적용
        // - 거친 숨소리와 바람 소리 재생
        // =====================================
        yield return StartCoroutine(ClimbLadder());

        // =====================================
        // 2. 암전
        // - 페이드 아웃
        // - 사다리 비활성화
        // - 헬기 내부에 의자로 플레이어 이동
        // =====================================
        yield return StartCoroutine(EnterHelicopter());

        // =====================================
        // 3. 헬기 내부
        // - 페이드 인
        // - 헬기 내부 시점으로 전환
        // - 헬기 내부를 둘러봄
        // =====================================
        yield return StartCoroutine(LookAroundHelicopter());

        // =====================================
        // 4. 헬기 이륙
        // - 프로펠러 소리 증가
        // - 헬기 이동
        // - 카메라에 이륙 진동 적용
        // =====================================
        yield return StartCoroutine(HelicopterTakeoff());

        // 5. 선택지 표시
        ShowChoices();
    }

    /// <summary>
    /// 두 회전 사이를 부드럽게 회전
    /// </summary>
    private IEnumerator RotatePlayerAndCamera(Quaternion startRotation, Quaternion targetRotation, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            Quaternion currentRotation =
                Quaternion.Slerp(startRotation, targetRotation, t);

            player.rotation = currentRotation;

            cameraController.SetCutSceneRotation(currentRotation);


            yield return null;
        }

        player.rotation = targetRotation;
        cameraController.SetCutSceneRotation(targetRotation);
    }

    /// <summary>
    /// 1. 사다리 등반
    /// </summary>
    private IEnumerator ClimbLadder()
    {
        Debug.Log("사다리 등반 시작");

        // 사다리 등반 시작 위치로 이동
        if (ladderClimbStartPoint != null)
        {
            player.position = ladderClimbStartPoint.position;
            player.rotation = ladderClimbStartPoint.rotation;
        }

        if (breathingAudio != null && !breathingAudio.isPlaying)
            breathingAudio.Play();

        if (windAudio != null && !windAudio.isPlaying)
            windAudio.Play();

        // 현재 카메라 방향을 기준으로 위를 바라본다.
        Quaternion startRotation = cameraController.transform.rotation;

        Quaternion climbRotation =
            Quaternion.Euler(-50f, player.eulerAngles.y, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(startRotation, climbRotation, 3f)
        );

        Vector3 startPosition = player.position;

        Vector3 targetPosition = ladderClimbTargetPoint != null ? ladderClimbTargetPoint.position : startPosition + Vector3.up * 3f;

        float elapsedTime = 0f;
        float climbDuration = 10f; // 사다리 등반 시간

        while (elapsedTime < climbDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / climbDuration);

            // 등반 진행에 따라 이동
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);

            // 사다리를 오르는 듯한 상하 리듬
            float bob = Mathf.Sin(elapsedTime * 5f) * 0.05f;

            position.y += bob;

            player.position = position;

            // 미세한 좌우 흔들림을 카메라 회전에 적용
            float sway = Mathf.Sin(elapsedTime * 3f) * 1.2f;

            Quaternion currentCameraRotation = climbRotation * Quaternion.Euler(0f, sway, 0f);

            cameraController.SetCutSceneRotation(currentCameraRotation);

            yield return null;
        }

        // 등반 종료 시 정확한 위치로 이동
        player.position = targetPosition;

        cameraController.SetCutSceneRotation(climbRotation);

        if (windAudio != null)
            windAudio.Stop();

        Debug.Log("사다리 등반 완료");
    }

    /// <summary>
    /// 2. 암전 후 헬기 내부로 이동
    /// </summary>
    private IEnumerator EnterHelicopter()
    {
        Debug.Log("헬기 내부 진입");

        // 페이드 아웃
        yield return StartCoroutine(Fade(1f));

        // 암전 중 사다리 비활성화
        if (ladder != null)
            ladder.SetActive(false);

        // 플레이어를 헬기 좌석으로 이동
        if (helicopterSeatPoint != null)
        {
            player.position = helicopterSeatPoint.position;

            // 좌석 방향에 맞춰 플레이어 몸을 회전
            Vector3 seatEuler = helicopterSeatPoint.eulerAngles;

            player.rotation = Quaternion.Euler(0f, seatEuler.y, 0f);

            // 카메라가 바라볼 방향도 설정
            cameraController.SetCutSceneRotation(helicopterSeatPoint.rotation);
        }

        yield return new WaitForSeconds(0.7f);

        // 페이드 인
        yield return StartCoroutine(Fade(0f));

        Debug.Log("헬기 내부 전환 완료");
    }

    /// <summary>
    /// 3. 헬기 내부 둘러보기
    /// </summary>
    private IEnumerator LookAroundHelicopter()
    {
        Debug.Log("헬기 내부 둘러보기");

        Quaternion startRotation = cameraController.transform.rotation;


        // 오른쪽 둘러보기
        Quaternion rightRotation = startRotation * Quaternion.Euler(0f, 35f, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(startRotation, rightRotation, 1f)
        );

        yield return new WaitForSeconds(0.5f);

        // 왼쪽 둘러보기
        Quaternion leftRotation = startRotation * Quaternion.Euler(0f, -70f, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(rightRotation, leftRotation, 1.7f)
        );

        yield return new WaitForSeconds(0.5f);

        // 정면으로 돌아오기
        yield return StartCoroutine(
            RotatePlayerAndCamera(leftRotation, startRotation, 0.7f)
        );

        // 내부를 둘러본 후 잠시 대기
        yield return new WaitForSeconds(0.7f);

        Debug.Log("헬기 내부 둘러보기 완료");
    }

    /// <summary>
    /// 4. 헬기 이륙
    /// </summary>
    private IEnumerator HelicopterTakeoff()
    {
        Debug.Log("헬기 이륙 시작");

        if (rotorAudio != null && !rotorAudio.isPlaying)
            rotorAudio.Play();

        Vector3 helicopterStartPosition = helicopter.transform.position;
        Vector3 playerStartPosition = player.position;

        // 헬기 상승 및 전진 목표 위치
        Vector3 helicopterTargetPosition =
            helicopterStartPosition
            + Vector3.up * 8f
            + helicopter.transform.forward * 12f;

        // 플레이어는 헬기의 움직임에 맞춰 함께 이동
        Vector3 playerTargetPosition =
            playerStartPosition + (helicopterTargetPosition - helicopterStartPosition);

        Quaternion baseCameraRotation = cameraController.transform.rotation;

        float elapsedTime = 0f;
        float takeoffDuration = 4f; // 이륙 시간

        while (elapsedTime < takeoffDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / takeoffDuration);

            // 초반에는 천천히 움직이고 이후 속도를 높임
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            helicopter.transform.position = Vector3.Lerp(
                helicopterStartPosition,
                helicopterTargetPosition,
                smoothT
            );

            player.position = Vector3.Lerp(
                playerStartPosition,
                playerTargetPosition,
                smoothT
            );

            float shakeAmount = 1f; // 흔들림 강도
            float shakeSpeed = 15f; // 흔들림 속도

            // 이륙 중 카메라 진동
            float pitchShake =
                Mathf.Sin(elapsedTime * shakeSpeed)
                * shakeAmount * 0.5f;

            float yawShake =
                Mathf.Sin(elapsedTime * shakeSpeed * 0.7f)
                * shakeAmount * 0.3f;

            Quaternion shakeRotation =
                baseCameraRotation
                * Quaternion.Euler(
                    pitchShake,
                    yawShake,
                    0f
                );

            cameraController.SetCutSceneRotation(shakeRotation);

            yield return null;
        }

        // 목표 위치에 정확히 정착
        helicopter.transform.position = helicopterTargetPosition;
        player.position = playerTargetPosition;

        // 이륙 진동 종료
        cameraController.SetCutSceneRotation(baseCameraRotation);

        Debug.Log("헬기 이륙 완료");
    }

    /// <summary>
    /// 화면 페이드
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / 1f);

            fadeCanvasGroup.alpha = Mathf.Lerp(
                startAlpha, targetAlpha, t
            );

            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// 선택지 표시
    /// </summary>
    private void ShowChoices()
    {
        Debug.Log("헬기 구조 엔딩 선택지 표시");

        // if (choicesUI != null)
        //     choicesUI.SetActive(true);
    }
}
