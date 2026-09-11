using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class FinalPatternManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DeepSeaPlayerMove deepSeaPlayerMove;
    [SerializeField] private GameObject playerGeo;
    [SerializeField] private GameObject monsterGeo;
    [SerializeField] private DeepSeaCameraController deepSeaCameraController;

    [Header("조준선 & 산소")]
    [SerializeField] private PanicCrosshair panicCrosshair;
    [SerializeField] private OxygenGaugeMinigame oxgenGaugeMinigame;

    [Header("UI")]
    [SerializeField] private GameObject finalPatternUI;
    [SerializeField] private RectTransform topBlackScreen;    // 위쪽 검은 상단 막(UI)
    [SerializeField] private RectTransform bottomBlackScreen; // 아래쪽 검은 하단 막(UI)
    [SerializeField] private Volume finalPatternVolume; // 최종 패턴 볼륨
    [SerializeField] private Slider fireAvailableTimeSlider; // 발사 가능 시간 타이머

    [Header("괴물")]
    [SerializeField] private Transform monsterTransform;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private Transform monsterFarPos;       // 기포에 밀려났을 때 위치
    [SerializeField] private Transform monsterAttackPos;    // 플레이어 코앞 공격 위치
    [SerializeField] private ParticleSystem bubbleVFX;      // 화면 가득 차는 대량 기포 파티클

    [Header("패턴 설정")]
    private int _maxRepetitions = 5;       // 총 성공해야 하는 횟수
    private float _approachDuration = 1f;     // 괴물이 다가오는 시간
    private float currentSlowTimeScale;    // 슬로우 모션 배속

    [Header("오디오")]
    private AudioSource _audioSource;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip appearSound;

    private int currentSuccessCount = 0;
    private bool isFiredInTime = false;
    private bool isSuccessInZone = false;
    private float _startDepth = 90f; // 패턴 시작 수심 (90m)
    private bool isPatternRunning = false;
    private bool _isClear = false;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        finalPatternUI.SetActive(false);
        finalPatternVolume.weight = 0f;
    }

    private void OnEnable()
    {
        // PanicCrosshair의 발사 이벤트 구독
        panicCrosshair.OnFireTriggered += HandleFireTriggered;
    }

    private void OnDisable()
    {
        panicCrosshair.OnFireTriggered -= HandleFireTriggered;

        // if (AudioManager.Instance != null)
        //     AudioManager.Instance.StopSFX();

        _audioSource.Stop();
    }

    private void HandleFireTriggered(bool inTargetZone)
    {
        // 입력이 들어왔음을 기록
        isFiredInTime = true;
        isSuccessInZone = inTargetZone;

        isFiredInTime = true;       // 우선 발사 시도했음을 알림
        isSuccessInZone = inTargetZone; // 성공 영역 안에서 눌렀는지 여부 저장 (true/false)

        if (inTargetZone)
        {
            // [성공 시] 즉시 조준선 고정 및 성공 효과음
            panicCrosshair.SetFireAvailable(false);
            // AudioManager.Instance.PlayGlobalOneShot(hitSound);
        }
        else
        {
            // [실패 시] 빗맞춤 효과음만 내주고 아무것도 중단하지 않음
            // (코루틴 루프 내부에서 isFiredInTime을 false로 다시 리셋해주므로 연속 시도 가능)
            // AudioManager.Instance.PlayGlobalOneShot(missSound);
        }
    }

    private void Update()
    {
        // 아직 시작하지 않았고, 클리어하지 않았고, 플레이어 수심이 시작 수심(80m) 위일 때 -> 최종 패턴 시작
        if (!isPatternRunning && !_isClear && deepSeaPlayerMove.DisplayDepth <= _startDepth)
        {
            StartFinalPattern();
        }
    }

    public void StartFinalPattern()
    {
        Debug.Log("최종 패턴 시작");
        isPatternRunning = true;
        currentSuccessCount = 0;

        deepSeaPlayerMove.CanMove = false; // 플레이어 이동 정지
        playerGeo.SetActive(false); // 플레이어 외형 안 보이게 하기

        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(3f);

        // 섬뜩한 소리 재생
        seq.AppendCallback(() =>
        {
            _audioSource.clip = startSound;
            _audioSource.Play();
        });

        // 플레이어가 중앙에서 좀 떨어져있으면 -> 무조건 카메라가 중앙 바라보도록 함 (플레이어가 벽을 보고 있는 경우를 방지하기 위해서)
        Vector3 centerPos = new Vector3(0f, deepSeaPlayerMove.transform.position.y, 0f);
        deepSeaCameraController.SetInputEnabled(false);
        StartCoroutine(deepSeaCameraController.Routine_LookAtPosition(centerPos, 1f));
        seq.AppendInterval(1f); // 소리 나오고 기다리기 (시간 차 등장)
        seq.AppendInterval(1f);
        seq.Join(DOTween.To(() => finalPatternVolume.weight, x => finalPatternVolume.weight = x, 1f, 1f));

        seq.AppendCallback(() =>
        {
            // 괴물 등장
            monsterGeo.SetActive(true); // 괴물 외형 보이게

            // 최종 패턴 UI 활성화
            finalPatternUI.SetActive(true);

            // 카메라 손전등 빨간 빛으로 변경
            deepSeaCameraController.cameraLight.color = Color.red;

            // // 산소 관리도 같이 시작
            // oxgenGaugeMinigame.StartMinigame();

            // 괴물 공격 패턴 시작
            StartCoroutine(Routine_FinalPatternLoop());

            // // monsterTransform.position = deepSeaPlayerMove.transform.position + Camera.main.transform.forward * 5f; // 카메라 바로 앞에 괴물 위치
            // monsterTransform.position = monsterAttackPos.position; // 카메라 바로 앞에 괴물 위치
            // monsterTransform.LookAt(deepSeaPlayerMove.transform.position); // 플레이어를 바라보도록 즉시 회전
        });
    }

    private IEnumerator Routine_FinalPatternLoop()
    {
        while (currentSuccessCount < _maxRepetitions)
        {
            isFiredInTime = false;
            isSuccessInZone = false;
            panicCrosshair.SetFireAvailable(false);
            if (currentSuccessCount > 0)
                panicCrosshair.ToggleCrosshair(false);

            // 1. 먼 위치 스폰
            monsterTransform.position = monsterFarPos.position;
            monsterTransform.LookAt(deepSeaPlayerMove.transform.position);

            _audioSource.PlayOneShot(appearSound);

            // 2. 접근 이동
            float approachElapsed = 0f;
            Vector3 startPos = monsterFarPos.position;
            Vector3 targetPos = monsterAttackPos.position;
            while (approachElapsed < _approachDuration)
            {
                approachElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(approachElapsed / _approachDuration);

                monsterTransform.position = Vector3.Lerp(startPos, targetPos, t);
                monsterTransform.LookAt(deepSeaPlayerMove.transform.position);

                yield return null;
            }

            monsterTransform.position = targetPos;

            // 3. 공격 시작
            monsterAnimator.Play("FinalPatternAttack", 0, 0f);

            yield return null; // 1프레임 대기

            bool isSlowApplied = false;
            bool isTimedOut = false; // 시간 초과 전용 플래그 추가

            while (!isSuccessInZone && !isTimedOut)
            {
                AnimatorStateInfo stateInfo = monsterAnimator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName("FinalPatternAttack"))
                {
                    // [0.5f 지점 진입]: 슬로우 시작 & 조준선/슬라이더 켜기
                    if (!isSlowApplied && stateInfo.normalizedTime >= 0.5f)
                    {
                        isSlowApplied = true;

                        float tProgress = (float)currentSuccessCount / Mathf.Max(1, _maxRepetitions - 1);
                        currentSlowTimeScale = Mathf.Lerp(0.08f, 0.18f, tProgress);

                        Time.timeScale = currentSlowTimeScale;
                        Time.fixedDeltaTime = 0.02f * Time.timeScale;

                        panicCrosshair.ToggleCrosshair(true);
                        panicCrosshair.SetFireAvailable(true);

                        if (fireAvailableTimeSlider != null)
                        {
                            fireAvailableTimeSlider.gameObject.SetActive(true);
                            fireAvailableTimeSlider.value = 1f;
                        }
                    }

                    // [슬로우 진행 중]: 애니메이션 normalizedTime에 직접 슬라이더 동기화
                    if (isSlowApplied)
                    {
                        float progress = Mathf.InverseLerp(0.5f, 0.8f, stateInfo.normalizedTime);

                        if (fireAvailableTimeSlider != null)
                        {
                            fireAvailableTimeSlider.value = 1f - progress;
                        }

                        // -----------------------------------------------------
                        // [오발 처리]: 시간을 놓치지 않고 잘못 눌렀을 경우 재시도 허용
                        // -----------------------------------------------------
                        if (isFiredInTime && !isSuccessInZone)
                        {
                            // 빗맞춤 사운드나 쿨다운 연출이 필요하다면 여기서 호출
                            // AudioManager.Instance.PlayGlobalOneShot(missSound);

                            // 플래그 리셋하여 남아있는 시간 동안 다시 발사 가능하게 함
                            isFiredInTime = false;
                        }
                    }

                    // [0.8f 지점 도달]: 시간 초과 (게임 오버 대상)
                    if (stateInfo.normalizedTime >= 0.8f)
                    {
                        if (fireAvailableTimeSlider != null)
                        {
                            fireAvailableTimeSlider.value = 0f;
                        }
                        isTimedOut = true;
                        break;
                    }
                }

                yield return null;
            }

            // 슬라이더 비활성화
            if (fireAvailableTimeSlider != null)
            {
                fireAvailableTimeSlider.gameObject.SetActive(false);
            }

            // -----------------------------------------------------
            // 4. 최종 판정 (성공 실패 여부)
            // -----------------------------------------------------
            // 시간 초과였거나 성공 영역을 맞추지 못했다면 게임 오버
            if (isTimedOut || !isSuccessInZone)
            {
                yield return StartCoroutine(Routine_ExecuteGameOver());
                yield break;
            }

            // 성공: 기포 발사 및 괴물 밀쳐내기
            currentSuccessCount++;
            panicCrosshair.ToggleCrosshair(false);

            // 시간 배속 복원
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // -----------------------------------------------------
            // 5. 대량 기포 발생 및 괴물 후퇴 연출
            // -----------------------------------------------------
            if (bubbleVFX != null) bubbleVFX.Play();
            monsterAnimator.SetTrigger("GetHit");
            monsterTransform.DOMove(monsterFarPos.position, 0.5f).SetEase(Ease.OutQuad);

            if (currentSuccessCount == _maxRepetitions)
                break;

            // // 괴물을 다시 멀리 이동
            // monsterTransform.position = monsterFarPos.position;

            // 기포로 시야가 가려졌다가 차차 걷히는 대기 시간
            yield return new WaitForSeconds(0.5f);
        }

        // 5회 성공 시 최종 패턴 클리어
        OnPatternClear();
    }

    /// <summary>
    /// 실패 시: 슬로우 풀리고 remaining 프레임 재생되며 입 닫히고 UI 쾅 닫힘
    /// </summary>
    private IEnumerator Routine_ExecuteGameOver()
    {
        // AudioManager.Instance.PlayGlobalOneShot(startSound);

        panicCrosshair.ToggleCrosshair(false);

        Time.timeScale = 0.05f;

        // 2. 입이 닫히는 아주 짧은 타이밍 대기
        yield return new WaitForSecondsRealtime(0.1f);

        // 2. DOTween 시퀀스 생성
        Sequence gameOverSequence = DOTween.Sequence();

        // timeScale 영향을 받지 않도록 unscaled time 설정
        gameOverSequence.SetUpdate(true);

        // Top UI: Y 좌표를 -540으로 이동
        gameOverSequence.Append(topBlackScreen.DOAnchorPosY(-540f, 0.15f).SetEase(Ease.OutQuad));
        // Bottom UI: Y 좌표를 540으로 이동
        gameOverSequence.Join(bottomBlackScreen.DOAnchorPosY(540f, 0.15f).SetEase(Ease.OutQuad));

        // 오디오 페이드 아웃
        gameOverSequence.Join(DOTween.To(() => _audioSource.volume, x => _audioSource.volume = x, 0f, 1f));

        // 3. DOTween 트윈 연출이 완전히 끝날 때까지 코루틴 대기
        yield return gameOverSequence.WaitForCompletion();

        // 1. 슬로우 해제 (남은 몇 프레임동안 정상 속도로 입이 콱 닫힘)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        yield return new WaitForSeconds(1f);

        // 게임 오버
        Debug.Log("게임 오버! 플레이어가 괴물에게 삼켜졌습니다.");
        GameManager.instance.GameOver(EEndingType.MonsterDeath);
        yield break;
    }

    private void OnPatternClear()
    {
        Debug.Log("<color=cyan>★ 최종 패턴 클리어!</color>");

        _isClear = true;
        isPatternRunning = false;

        // 볼륨 되돌리기
        DOTween.To(() => finalPatternVolume.weight, x => finalPatternVolume.weight = x, 0f, 1f);

        // 오디오 페이드 아웃
        DOTween.To(() => _audioSource.volume, x => _audioSource.volume = x, 0f, 2f);

        // 괴물 비활성화
        monsterTransform.gameObject.SetActive(false);

        // UI 비활성화
        finalPatternUI.SetActive(false);

        // 시간 원래대로 되돌리기
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // 조준선 & 산소 미니게임 끄기
        panicCrosshair.ToggleCrosshair(false);
        // oxgenGaugeMinigame.StopMinigame();

        // 플레이어 이동 가능 & 카메라 조작 가능
        deepSeaPlayerMove.CanMove = true;
        deepSeaCameraController.SetInputEnabled(true);
        deepSeaCameraController.cameraLight.color = Color.white; // 카메라 손전등 하얀 빛으로 되돌리기
    }
}