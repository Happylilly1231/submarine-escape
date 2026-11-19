using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NoHelicopterEndingCutscene : MonoBehaviour
{
    public static NoHelicopterEndingCutscene Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private DeepSeaCameraController cameraController;

    [Header("Player")]
    [SerializeField] private DeepSeaPlayerMove playerMove;
    [SerializeField] private Transform soldier_body;
    [SerializeField] private Transform monsterHandsTransform; // 괴물 양손 트랜스폼

    [Header("Pos")]
    [SerializeField] private Transform lookAroundStartPoint;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Image mutationVignetteImg;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartCutScene()
    {
        Debug.Log("헬기 미도착 엔딩 컷씬 - 완전 괴물화");

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
    /// 헬기 미도착 컷씬 시퀀스
    /// <para> 1. 수면 위에서 헬기를 찾으며 좌우로 둘러봄 </para>
    /// <para> 2. 1인칭 시점에서 고통스럽게 괴물화 진행 </para>
    /// <para> 3. 3인칭 괴물 모델로 전환되며 카메라가 뒤로 멀어짐 </para>
    /// <para> 4. 화면 페이드 아웃 </para>
    /// </summary>
    private IEnumerator StartSequence()
    {
        // =====================================
        // 1. 좌우 둘러보기
        // =====================================
        yield return StartCoroutine(LookAroundForHelicopter());

        // =====================================
        // 2. 괴물화 진행
        // =====================================
        yield return StartCoroutine(MutatePlayer());

        // =====================================
        // 3. 마무리 페이드 아웃
        // =====================================
        yield return StartCoroutine(Fade(1f));

        Debug.Log("헬기 미도착(완전 괴물화) 엔딩 컷씬 완료");

        Debug.Log("=== Game Over ===");
        GameManager.instance.GameOver(EEndingType.MonsterDeath);
    }

    /// <summary>
    /// 플레이어와 카메라를 특정 방향으로 회전시키고 완료될 때까지 대기
    /// </summary>
    private IEnumerator RotateCameraTo(Quaternion targetRotation, float duration)
    {
        if (cameraController == null) yield break;

        Quaternion startRotation = cameraController.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            Quaternion currentRot = Quaternion.Slerp(startRotation, targetRotation, t);
            cameraController.SetCutSceneRotation(currentRot);

            yield return null;
        }

        cameraController.SetCutSceneRotation(targetRotation);
    }

    /// <summary>
    /// 1. 하늘을 올려다보며 좌우로 헬기를 찾는 연출
    /// </summary>
    private IEnumerator LookAroundForHelicopter()
    {
        Debug.Log("주변 둘러보기 시작");

        Quaternion frontRotation = lookAroundStartPoint != null
        ? lookAroundStartPoint.rotation
        : cameraController.transform.rotation;

        if (lookAroundStartPoint != null)
        {
            yield return StartCoroutine(RotateCameraTo(frontRotation, 0.7f));
        }

        yield return new WaitForSeconds(0.2f);

        // 3. 저장해 둔 정면(frontRotation)을 기준으로 오른쪽 둘러보기
        Quaternion rightRotation = frontRotation * Quaternion.Euler(0f, 45f, 0f);
        yield return StartCoroutine(RotateCameraTo(rightRotation, 2f));
        yield return new WaitForSeconds(0.4f);

        // 4. 정면을 기준으로 왼쪽 둘러보기
        Quaternion leftRotation = frontRotation * Quaternion.Euler(0f, -45f, 0f);
        yield return StartCoroutine(RotateCameraTo(leftRotation, 3f));
        yield return new WaitForSeconds(0.4f);

        // 5. 다시 정면으로 복귀
        yield return StartCoroutine(RotateCameraTo(frontRotation, 1.5f));
        yield return new WaitForSeconds(0.4f);

        Debug.Log("주변 둘러보기 완료");
    }

    /// <summary>
    /// 2. 1인칭 괴물화 연출
    /// </summary>
    private IEnumerator MutatePlayer()
    {
        Debug.Log("괴물화 변이 시작");

        if (monsterHandsTransform != null)
        {
            soldier_body.gameObject.SetActive(false);
            monsterHandsTransform.gameObject.SetActive(true);
            monsterHandsTransform.DOLocalMoveZ(0.4f, 3.5f).SetEase(Ease.InSine);
        }

        Quaternion baseRotation = cameraController.transform.rotation;
        Quaternion dropRotation = baseRotation * Quaternion.Euler(35f, 0f, 12f);

        mutationVignetteImg.gameObject.SetActive(true);

        float mutationDuration = 2.5f;
        float elapsedTime = 0f;

        while (elapsedTime < mutationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / mutationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Quaternion currentBaseRot = Quaternion.Slerp(baseRotation, dropRotation, smoothT);

            float shakeAmount = Mathf.Lerp(0.5f, 4.0f, t);
            float shakeSpeed = 22f;

            float pitchShake = Mathf.Sin(elapsedTime * shakeSpeed) * shakeAmount;
            float yawShake = Mathf.Cos(elapsedTime * shakeSpeed * 0.85f) * shakeAmount * 0.7f;
            float rollShake = Mathf.Sin(elapsedTime * shakeSpeed * 1.1f) * shakeAmount * 0.5f;

            Quaternion shakeRotation = currentBaseRot * Quaternion.Euler(pitchShake, yawShake, rollShake);
            cameraController.SetCutSceneRotation(shakeRotation);

            // 괴물화 화면 오버레이가 연결되어 있다면 알파값 증가
            if (mutationVignetteImg != null)
            {
                Color c = mutationVignetteImg.color;
                c.a = Mathf.Lerp(0f, 0.8f, t);
                mutationVignetteImg.color = c;
            }

            yield return null;
        }

        Debug.Log("괴물화 변이 완료");
    }

    /// <summary>
    /// 화면 페이드
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
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
    }
}
