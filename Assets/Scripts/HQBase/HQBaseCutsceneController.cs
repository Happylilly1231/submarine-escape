using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HQBaseCutsceneController : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float moveDistanceX = -16.0f;
    [SerializeField] private float moveDuration = 10.0f;

    [Header("RescueAgent Walk")]
    [SerializeField] private RescueAgentMove rescueAgentMove;
    [SerializeField] private AudioSource walkAudio;
    [SerializeField] private bool enableWalkBob = true;
    [SerializeField] private float stepFrequency = 2.0f;   // 초당 발걸음 리듬
    [SerializeField] private float verticalBobAmp = 0.045f; // 상하 흔들림 폭
    [SerializeField] private float horizontalBobAmp = 0.025f; // 좌우 흔들림 폭
    [SerializeField] private float rollTiltAmp = 0.7f;      // 좌우 미세 기울기

    [Header("Bomb QTE")]
    [SerializeField] private GameObject bombQtePanel;
    [SerializeField] private Button bombQteButton;
    [SerializeField] private float qteStartTime = 7.0f; // QTE 시작 시간
    [SerializeField] private float qteEndTime = 9.0f;   // QTE 종료 시간
    [SerializeField] private ParticleSystem gasParticleSystem;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private bool _shouldShowQte = false;
    private bool _isQteActive = false;
    private bool _isBombTriggered = false;
    private bool _isEndingStarted = false;
    private Coroutine _walkRoutine;

    private void Awake()
    {
        if (bombQtePanel != null)
            bombQtePanel.SetActive(false);

        if (bombQteButton != null)
            bombQteButton.onClick.AddListener(OnClickBombQte);

        if (gasParticleSystem != null)
            gasParticleSystem.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        _shouldShowQte = HQBaseBridge.Instance != null && HQBaseBridge.Instance.ShouldTriggerBombQTE;
        _walkRoutine = StartCoroutine(MoveForward());
    }

    private IEnumerator MoveForward()
    {
        Vector3 startPos = targetCamera.position;
        Vector3 endPos = startPos + new Vector3(moveDistanceX, 0f, 0f);
        Quaternion startRot = targetCamera.localRotation;

        float elapsed = 0f;
        float bobPhase = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            UpdateQteWindow(elapsed);

            // 이동 끝부분에서 흔들림이 자연스럽게 잦아들도록 감쇠 계수 적용
            float bobWeight = enableWalkBob ? Mathf.Sin(t * Mathf.PI) : 0f;
            bobWeight = Mathf.Clamp01(bobWeight * 2.5f); // 시작·끝 구간만 부드럽게 페이드 인/아웃

            Vector3 basePos = Vector3.Lerp(startPos, endPos, t);

            if (bobWeight > 0.001f)
            {
                bobPhase += Time.deltaTime * stepFrequency * Mathf.PI * 2f;

                // 한 걸음 내디딜 때마다 아래로 살짝 꺼지고, 좌우로 번갈아 흔들림
                float yOffset = -Mathf.Abs(Mathf.Sin(bobPhase * 0.5f)) * verticalBobAmp * bobWeight;
                float sideOffset = Mathf.Cos(bobPhase * 0.5f) * horizontalBobAmp * bobWeight;
                float rollAngle = -Mathf.Cos(bobPhase * 0.5f) * rollTiltAmp * bobWeight;

                // 카메라가 +X 또는 -X를 바라볼 때 로컬 오른쪽 축 기준으로 좌우 흔들림 적용
                Vector3 rightAxis = targetCamera.right;
                rightAxis.y = 0f;
                if (rightAxis.sqrMagnitude > 0.0001f)
                    rightAxis.Normalize();

                targetCamera.position = basePos + new Vector3(0f, yOffset, 0f) + rightAxis * sideOffset;
                targetCamera.localRotation = startRot * Quaternion.Euler(0f, 0f, rollAngle);
            }
            else
            {
                targetCamera.position = basePos;
                targetCamera.localRotation = startRot;
            }

            yield return null;
        }

        targetCamera.position = endPos;
        targetCamera.localRotation = startRot;

        // QTE를 안 눌렀거나 _shouldShowQte가 false인 경우 기본 엔딩으로 진행
        if (!_isBombTriggered && !_isEndingStarted)
        {
            StartCoroutine(PlayNormalEnding());
        }
    }

    /// <summary>
    /// 4초 ~ 5초 구간에서 _shouldShowQte가 true일 때만 QTE 패널 활성화
    /// </summary>
    private void UpdateQteWindow(float elapsed)
    {
        if (!_shouldShowQte || _isBombTriggered)
        {
            if (_isQteActive)
                CloseQtePanel();
            return;
        }

        if (elapsed >= qteStartTime && elapsed < qteEndTime)
        {
            if (!_isQteActive)
            {
                _isQteActive = true;
                if (bombQtePanel != null)
                    bombQtePanel.SetActive(true);
            }
        }
        else if (elapsed >= qteEndTime && _isQteActive)
        {
            // 5초 경과: 제한 시간 내 미입력 -> 패널 닫고 계속 걷다가 기본 엔딩으로 진행
            CloseQtePanel();
        }
    }

    private void CloseQtePanel()
    {
        _isQteActive = false;
        if (bombQtePanel != null)
            bombQtePanel.SetActive(false);
    }

    /// <summary>
    /// QTE 버튼 OnClick 이벤트에 연결할 메서드 (4초~5초 사이에만 유효)
    /// </summary>
    public void OnClickBombQte()
    {
        if (!_isQteActive || _isBombTriggered || _isEndingStarted)
            return;

        _isBombTriggered = true;
        CloseQtePanel();

        if (_walkRoutine != null)
        {
            StopCoroutine(_walkRoutine);
            _walkRoutine = null;
        }

        StartCoroutine(PlayBombEnding());
    }

    private IEnumerator PlayBombEnding()
    {
        Debug.Log("QTE 성공! 폭탄 엔딩 실행");
        _isEndingStarted = true;

        // 가스 파티클 재생
        if (gasParticleSystem != null)
        {
            gasParticleSystem.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(rescueAgentMove.ShowBackSpikesRoutine());

        yield return new WaitForSeconds(1f);

        gasParticleSystem.gameObject.SetActive(false);

        yield return new WaitForSeconds(2f);

        // 페이드 아웃
        yield return StartCoroutine(Fade(1f));

        GameManager.instance.GameOver(EEndingType.MonsterDeath);

        yield break;
    }

    private IEnumerator PlayNormalEnding()
    {
        Debug.Log("기본 엔딩 실행 (QTE 미발동 또는 시간 초과)");
        _isEndingStarted = true;

        // 페이드 아웃
        yield return StartCoroutine(Fade(1f));

        GameManager.instance.GameOver(EEndingType.MonsterDeath);

        yield break;
    }

    private IEnumerator Fade(float alpha)
    {
        walkAudio.Stop();

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
                startAlpha, alpha, t
            );

            yield return null;
        }

        fadeCanvasGroup.alpha = alpha;
    }
}
