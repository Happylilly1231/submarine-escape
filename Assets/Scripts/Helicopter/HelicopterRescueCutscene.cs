using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterRescueCutscene : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private DeepSeaCameraController cameraController;

    [Header("Player")]
    [SerializeField] private DeepSeaPlayerMove playerMove;

    [Header("Obj")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject helicopter;
    [SerializeField] private GameObject ladder;
    [SerializeField] private BoxCollider ladderCollider;

    [Header("Pos")]
    [SerializeField] private Transform ladderClimbStartPoint; // 사다리 등반이 시작하는 위치
    [SerializeField] private Transform ladderClimbTargetPoint; // 사다리 등반이 끝나는 위치
    [SerializeField] private Transform helicopterSeatPoint; // 헬기 내부 플레이어 착석 위치

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("UI")]
    [SerializeField] private GameObject choicesUI;

    public bool IsCutScene { get; private set; } = false;

    private void Start()
    {
        bool hasContactedHQ = DeepSeaBridge.Instance != null && DeepSeaBridge.Instance.HasContactedHQ;

        if (!hasContactedHQ) helicopter.SetActive(false);
    }

    public void StartCutscene()
    {
        Debug.Log("헬리콥터 구조 컷씬 시작");

        IsCutScene = true;

        InputManager.instance.DisableAllInputs(); // 모든 인풋 비활성화

        // HUD 비활성화
        DeepSeaUIManager.Instance?.gameObject.SetActive(false);

        // 카메라 입력 막기
        cameraController.SetInputEnabled(false);
        cameraController.IsInCutScene = true;

        playerMove.PrepareCutscene(); // 플레이어 컷씬 모드 활성화

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
    /// 플레이어를 특정 위치로 이동시키고 완료될 때까지 대기
    /// </summary>
    private IEnumerator MovePlayerTo(Vector3 targetPosition, float duration)
    {
        bool isComplete = false;

        playerMove.MoveToLocationForCutscene(
            targetPosition,
            duration,
            () => isComplete = true
        );

        yield return new WaitUntil(() => isComplete);
    }

    /// <summary>
    /// 플레이어와 카메라를 특정 방향으로 회전시키고 완료될 때까지 대기
    /// </summary>
    private IEnumerator RotatePlayerTo(Quaternion targetRotation, float duration)
    {
        bool isComplete = false;

        playerMove.RotateToForCutscene(
            targetRotation,
            duration,
            () => isComplete = true
        );

        yield return new WaitUntil(() => isComplete);
    }

    /// <summary>
    /// 1. 사다리 등반
    /// </summary>
    private IEnumerator ClimbLadder()
    {
        Debug.Log("사다리 등반 시작");

        // 사다리 콜라이더 비활성화
        if (ladderCollider != null)
            ladderCollider.enabled = false;

        // 사다리 등반 시작 위치 및 정면 방향으로 회전
        if (ladderClimbStartPoint != null)
        {
            player.position = ladderClimbStartPoint.position;
            player.rotation = Quaternion.Euler(0f, ladderClimbStartPoint.eulerAngles.y, 0f);
            cameraController.SetCutSceneRotation(ladderClimbStartPoint.rotation);
        }

        // 사다리 위쪽을 올려다봄
        float baseYaw = player.eulerAngles.y;
        Quaternion climbRotation = Quaternion.Euler(-45f, baseYaw, 0f);
        yield return StartCoroutine(
            RotatePlayerTo(climbRotation, 3f)
        );

        // 사다리 등반
        Vector3 startPos = player.position;
        Vector3 targetPos = ladderClimbTargetPoint != null
            ? ladderClimbTargetPoint.position
            : player.position + Vector3.up * 3f;

        float elapsed = 0f;
        float climbDuration = 4f; // 등반 시간
        float climbStepCount = 4f; // 등반 스텝 수
        float stepFrequency = (climbStepCount * Mathf.PI) / climbDuration;

        while (elapsed < climbDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / climbDuration);

            float stepPulse = Mathf.Sin(normalizedTime * climbStepCount * Mathf.PI * 2f) / (climbStepCount * Mathf.PI * 2f);
            float steppedProgress = Mathf.Clamp01(normalizedTime - stepPulse * 0.45f);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, steppedProgress);
            player.position = currentPos;

            // 시선 및 흔들림 계산
            float handCycle = elapsed * stepFrequency;
            float roll = Mathf.Sin(handCycle) * 2f;         // 몸이 좌우로 살짝 기움
            float yawOffset = Mathf.Cos(handCycle) * 2f;     // 시선 좌우 미세 흔들림
            float pitchBob = Mathf.Abs(Mathf.Sin(handCycle)) * 3f; // 발 디딜 때 살짝 고개 숙임

            // 올라갈수록 위를 보는 각도를 서서히 완화
            float basePitch = Mathf.Lerp(-45f, -28f, normalizedTime);

            Quaternion currentCamRot = Quaternion.Euler(
                basePitch + pitchBob,
                baseYaw + yawOffset,
                roll
            );

            cameraController.SetCutSceneRotation(currentCamRot);

            yield return null;
        }

        // 최종 위치 및 정면 시선 보정
        player.position = targetPos;
        Quaternion finishLookRot = Quaternion.Euler(-20f, baseYaw, 0f);
        yield return StartCoroutine(RotatePlayerTo(finishLookRot, 0.5f));

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

        if (ladder != null)
            ladder.SetActive(false);

        if (helicopterSeatPoint != null)
        {
            player.position = helicopterSeatPoint.position;
            player.rotation = Quaternion.Euler(0f, helicopterSeatPoint.eulerAngles.y, 0f);
            cameraController.SetCutSceneRotation(helicopterSeatPoint.rotation);

            // 플레이어를 헬기 자식으로 설정
            player.SetParent(helicopter.transform, true);
        }

        // 암전 유지 시간 동안 알파값 1 보장
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 1f;

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

        float lookUpAngle = -10f;

        // 오른쪽 둘러보기
        Quaternion rightRotation =
            startRotation * Quaternion.Euler(0f, 35f, 0f) * Quaternion.Euler(lookUpAngle, 0f, 0f);

        yield return StartCoroutine(
            RotatePlayerTo(rightRotation, 1f)
        );

        yield return new WaitForSeconds(0.5f);

        // 왼쪽 둘러보기
        Quaternion leftRotation =
            startRotation * Quaternion.Euler(0f, -70f, 0f) * Quaternion.Euler(lookUpAngle, 0f, 0f);

        yield return StartCoroutine(
            RotatePlayerTo(leftRotation, 1.7f)
        );

        yield return new WaitForSeconds(0.5f);

        // 정면으로 복귀
        yield return StartCoroutine(
            RotatePlayerTo(startRotation, 0.7f)
        );

        yield return new WaitForSeconds(0.7f);

        Debug.Log("헬기 내부 둘러보기 완료");
    }

    /// <summary>
    /// 4. 헬기 이륙
    /// </summary>
    private IEnumerator HelicopterTakeoff()
    {
        Debug.Log("헬기 이륙 시작");

        Vector3 helicopterStartPosition = helicopter.transform.position;

        // 헬기의 수평 전진 방향
        Vector3 forwardDirection = helicopter.transform.forward;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        // 위쪽 + 앞쪽으로 비스듬하게 상승
        Vector3 helicopterRisePosition =
            helicopterStartPosition
            + Vector3.up * 8f
            + forwardDirection * 6f;

        // 상승한 높이를 유지하며 앞으로 전진
        Vector3 helicopterTargetPosition =
            helicopterRisePosition + forwardDirection * 12f;

        float takeoffDuration = 4f;
        float riseDuration = 2f;
        float forwardDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < takeoffDuration)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime < riseDuration)
            {
                // 1. 비스듬하게 상승
                float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedTime / riseDuration));
                helicopter.transform.position = Vector3.Lerp(helicopterStartPosition, helicopterRisePosition, smoothT);
            }
            else
            {
                float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsedTime - riseDuration) / forwardDuration));
                helicopter.transform.position = Vector3.Lerp(helicopterRisePosition, helicopterTargetPosition, smoothT);
            }

            // 카메라 진동
            float shakeAmount = 1f;
            float shakeSpeed = 15f;

            float pitchShake =
                Mathf.Sin(elapsedTime * shakeSpeed)
                * shakeAmount * 0.5f;

            float yawShake =
                Mathf.Sin(elapsedTime * shakeSpeed * 0.7f)
                * shakeAmount * 0.3f;

            Quaternion shakeRotation = helicopterSeatPoint.rotation * Quaternion.Euler(pitchShake, yawShake, 0f);
            cameraController.SetCutSceneRotation(shakeRotation);

            yield return null;
        }

        helicopter.transform.position = helicopterTargetPosition;
        cameraController.SetCutSceneRotation(helicopterSeatPoint.rotation);

        Debug.Log("헬기 이륙 완료");
    }

    /// <summary>
    /// 화면 페이드
    /// </summary>
    public IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
            yield break;

        // 비활성화 상태라면 활성화
        if (!fadeCanvasGroup.gameObject.activeSelf)
            fadeCanvasGroup.gameObject.SetActive(true);

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

        // 페이드 아웃 완료 후 비활성화
        if (targetAlpha >= 1f)
            fadeCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// 선택지 표시
    /// </summary>
    private void ShowChoices()
    {
        Debug.Log("헬기 구조 엔딩 선택지 표시");

        if (choicesUI != null)
            choicesUI.SetActive(true);

        FocusManager.Instance.PushFocusState(GameFocusState.DeepSeaUI);
        PlayerManager.Instance.playerInput.actions.FindAction("DeepSea/SlotKeyPress")?.Enable();
    }
}
