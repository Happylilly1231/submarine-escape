using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class FinalPatternManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DeepSeaPlayerMove deepSeaPlayerMove;
    [SerializeField] private GameObject monsterGeo;
    [SerializeField] private DeepSeaCameraController deepSeaCameraController;
    [SerializeField] private Rig playerHandRig;
    [SerializeField] private Rig playerMonsterSpikeRig;
    private Rig _currentRig;

    [Header("조준선")]
    [SerializeField] private PanicCrosshair panicCrosshair;

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
    public float FirstSlowTimeScale { get; set; } = 0.08f;    // 맨 처음 가장 느린(작은) 슬로우 모션 배속
    public float FinalSlowTimeScale { get; set; } = 0.16f;    // 맨 마지막 가장 빠른(큰) 슬로우 모션 배속

    [Header("오디오")]
    private AudioSource _audioSource;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip appearSound;

    private int currentSuccessCount = 0; // 현재 패턴 성공 횟수
    private bool isFiredInTime = false; // 발사 가능 시간 안에 발사되었는지 여부
    private bool isSuccessInZone = false; // 성공 영역 안에 발사되었는지 여부
    private float _startDepth = 90f; // 패턴 시작 수심 (90m) (테스트용: 270)
    public float startDepth => _startDepth; // 패턴 시작 수심 (90m)
    public bool IsPatternRunning { get; private set; } = false; // 패턴 진행 중 여부
    private bool _isClear = false; // 클리어 여부

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        finalPatternUI.SetActive(false);
        finalPatternVolume.weight = 0f;
        _currentRig = GameManager.instance.IsAbilityActive ? playerMonsterSpikeRig : playerHandRig;
    }

    private void OnEnable()
    {
        // PanicCrosshair의 발사 이벤트 구독
        panicCrosshair.OnFireTriggered += HandleFireTriggered;
    }

    private void OnDisable()
    {
        panicCrosshair.OnFireTriggered -= HandleFireTriggered;

        if (_audioSource != null)
            _audioSource.Stop();
    }

    /// <summary>
    /// 발사 발생했을 때 -> 시간 안에 입력이 들어왔음 기록, 성공 영역 안인지 여부 기록
    /// </summary>
    /// <param name="inTargetZone"></param>
    private void HandleFireTriggered(bool inTargetZone)
    {
        // 입력이 들어왔음을 기록
        isFiredInTime = true;       // 우선 발사 시도했음을 알림
        isSuccessInZone = inTargetZone; // 성공 영역 안에서 눌렀는지 여부 저장

        // 영역 안에 들어왔다면 -> 현재는 발사 더 이상 불가능 (이미 맞췄으니까)
        if (inTargetZone)
            panicCrosshair.SetFireAvailable(false);
    }

    private void Update()
    {
        // 아직 패턴 시작하지 않았고, 클리어하지 않았고, 플레이어 수심이 시작 수심 위일 때 -> 최종 패턴 시작
        if (!IsPatternRunning && !_isClear && deepSeaPlayerMove.DisplayDepth <= _startDepth)
        {
            StartFinalPattern();
        }
    }

    public void StartFinalPattern()
    {
        Debug.Log("최종 패턴 시작");

        IsPatternRunning = true; // 패턴 진행 중으로 변경
        currentSuccessCount = 0; // 성공 횟수 초기화

        deepSeaPlayerMove.SetMoveable(false); // 플레이어 이동 정지

        // 능력 사용 게임일 때 -> 애니메이션 가만히 있는 걸로 변경, 그에 따른 카메라 위치 조정
        if (GameManager.instance.IsAbilityActive)
        {
            PlayerManager.Instance.playerMoveBase.animator.SetLayerWeight(2, 1f); // Idle2 애니메이션으로 덮어쓰기
            Vector3 newCamPos = deepSeaCameraController.CameraPos.localPosition;
            newCamPos.z += 0.25f;
            deepSeaCameraController.CameraPos.localPosition = newCamPos;
        }

        Sequence seq = DOTween.Sequence(); // 연출 시작

        seq.AppendInterval(3f); // 3초 기다림

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
        seq.JoinCallback(() =>
        {
            // 최종 패턴 UI 활성화
            finalPatternUI.SetActive(true);

            // 카메라 손전등 빨간 빛으로 변경
            deepSeaCameraController.cameraLight.color = Color.red;
        });
        seq.Append(DOTween.To(() => _currentRig.weight, x => _currentRig.weight = x, 1f, 1f)); // 리그 타겟 활성화

        seq.AppendCallback(() =>
        {
            // 괴물 등장
            monsterGeo.SetActive(true); // 괴물 외형 보이게

            // 최종 패턴 루프 시작
            StartCoroutine(FinalPatternLoopCoroutine());
        });
    }

    /// <summary>
    /// 최종 패턴 루프 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator FinalPatternLoopCoroutine()
    {
        // 반경 설정
        float farRadius = 5f;    // 먼 위치 변주 반경
        float attackRadius = 2.5f; // 공격 위치 변주 반경

        panicCrosshair.ToggleCrosshair(true);

        // 최대 반복 횟수보다 현재 횟수가 적은 동안
        while (currentSuccessCount < _maxRepetitions)
        {
            // 정지 중일 때 -> 실행 안되도록 중지
            while (GameManager.instance.IsPausing) yield return null;

            // 발사 여부, 성공 여부, 발사 가능 여부 초기화
            isFiredInTime = false;
            isSuccessInZone = false;
            panicCrosshair.SetFireAvailable(false);

            // // 크로스헤어 UI 끄기
            // panicCrosshair.ToggleCrosshair(false);

            // 변주 오프셋 계산
            Vector2 randomCircle = Random.insideUnitCircle.normalized; // 정규화를 해서 무조건 원 둘레 상에서만 나오도록 함
            Vector3 randomOffsetDir = new Vector3(randomCircle.x, randomCircle.y, 0f); // 3차원으로 변경
            // Vector3 randomOffsetDir = new Vector3(0f, 1f, 0f); // 테스트용

            // farPos와 attackPos에 변주 오프셋 적용
            Vector3 calculatedFarPos = monsterFarPos.position + (randomOffsetDir * farRadius);
            Vector3 calculatedAttackPos = monsterAttackPos.position + (randomOffsetDir * attackRadius);
            if (GameManager.instance.IsAbilityActive) // 능력 사용 게임일 때는 -> 괴물은 좀 더 앞으로 나와있기 때문에 위치들을 다 뒤로 좀 더 보내줌
            {
                calculatedFarPos.z += 0.5f;
                calculatedAttackPos.z += 0.5f;
            }

            // 괴물 현재 계산된 먼 위치에서 생성(등장)
            monsterTransform.position = calculatedFarPos;
            monsterTransform.LookAt(deepSeaPlayerMove.transform.position); // 플레이어 바라보도록 함
            _audioSource.PlayOneShot(appearSound); // 괴물 등장 소리 재생

            // 접근 시간 동안 접근 이동
            float approachElapsed = 0f;
            Vector3 startPos = calculatedFarPos;
            Vector3 targetPos = calculatedAttackPos; // 현재 계산된 공격 위치로 괴물이 다가옴
            while (approachElapsed < _approachDuration) // 접근 시간 다 지나지 않은 동안
            {
                // 정지 중일 때 -> 실행 안되도록 중지
                while (GameManager.instance.IsPausing) yield return null;

                approachElapsed += Time.deltaTime; // 타이머 증가
                float t = Mathf.Clamp01(approachElapsed / _approachDuration); // 접근 비율 계산
                monsterTransform.position = Vector3.Lerp(startPos, targetPos, t); // 접근 비율에 따른 위치 업데이트
                monsterTransform.LookAt(deepSeaPlayerMove.transform.position); // 플레이어 바라봄

                yield return null;
            }
            monsterTransform.position = targetPos; // 마지막에 공격 위치로 정확히 보정

            // 공격 시작
            monsterAnimator.Play("FinalPatternAttack", 0, 0f); // 애니메이션 재생

            yield return null; // 1프레임 대기

            bool isSlowApplied = false; // 슬로우 변수 초기화
            bool isTimedOut = false; // 시간 초과 변수 초기화
            float timer = 0f; // 느린 구간 타이머 0으로 초기화
            float animationLength = 1f;

            // 성공하거나 시간 초과 전까지 공격 애니메이션 진행하는 동안 맞춰야 함
            while (!isSuccessInZone && !isTimedOut)
            {
                // 정지 중일 때 -> 실행 안되도록 중지
                while (GameManager.instance.IsPausing) yield return null;

                timer += Time.deltaTime; // 타이머 증가
                // Debug.Log(timer + " | " + monsterAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);

                // 0.5f(반) 지점 -> 슬로우 시작 & 조준선/슬라이더 켜기
                if (!isSlowApplied && timer >= animationLength * 0.5f)
                {
                    isSlowApplied = true; // 슬로우 시작

                    // 현재 패턴 받아치기 성공 횟수에 따라 슬로우가 더 빨라지게 함
                    float tProgress = (float)currentSuccessCount / (_maxRepetitions - 1); // 0, 0.25, 0.5, 0.75, 1
                    currentSlowTimeScale = Mathf.Lerp(FirstSlowTimeScale, FinalSlowTimeScale, tProgress);

                    // 슬로우 적용
                    Time.timeScale = currentSlowTimeScale;
                    Time.fixedDeltaTime = 0.02f * Time.timeScale;

                    // 현재 오프셋에 따른 타겟 존 위치 업데이트, 발사 가능하게 하기
                    panicCrosshair.SetTargetZonePos(randomOffsetDir);
                    panicCrosshair.SetFireAvailable(true);

                    // 발사 가능 남은 시간 슬라이더 UI 보이게 하기 & 1로 초기화
                    if (fireAvailableTimeSlider != null)
                    {
                        fireAvailableTimeSlider.gameObject.SetActive(true);
                        fireAvailableTimeSlider.value = 1f;
                    }
                    // Debug.Log("!!! " + fireAvailableTimeSlider.value);
                }

                // 0.5f 지점 지나서 슬로우 적용된 동안 -> 애니메이션 normalizedTime에 직접 슬라이더 동기화
                if (isSlowApplied)
                {
                    // 0.5f 지점부터 0.8f 지점(거의 물 때) 도달하기까지 현재 애니메이션 진행 비율
                    float progress = Mathf.InverseLerp(animationLength * 0.5f, animationLength * 0.8f, timer);

                    // 발사 가능 남은 시간 슬라이더 UI에 남은 시간 업데이트
                    if (fireAvailableTimeSlider != null) fireAvailableTimeSlider.value = 1f - progress;
                    // Debug.Log("??? " + fireAvailableTimeSlider.value);

                    // 시간 안에 눌렀는데, 구역 안이 아니라서 성공하지 못한 경우 -> 재시도 허용
                    if (isFiredInTime && !isSuccessInZone)
                    {
                        isFiredInTime = false; // 플래그 리셋하여 남아있는 시간 동안 다시 발사 가능하게 함
                    }
                }

                // 0.8f 지점(거의 물 때) -> 시간 초과로 게임 오버
                if (timer > animationLength * 0.8f)
                {
                    if (fireAvailableTimeSlider != null) fireAvailableTimeSlider.value = 0f; // 슬라이더 UI 확실하게 0으로 만듦
                    isTimedOut = true; // 시간 초과 변수 true
                    break; // 판정 검사 종료 -> 아래에서 최종 판정
                }


                // // 애니메이션 진행 시점에 따른 로직
                // AnimatorStateInfo stateInfo = monsterAnimator.GetCurrentAnimatorStateInfo(0);
                // if (stateInfo.IsName("FinalPatternAttack")) // 최종 공격 애니메이션이 진행 중인 동안
                // {
                //     Debug.Log($"[디버깅] 이름: {stateInfo.IsName("FinalPatternAttack")}, 시간: {stateInfo.normalizedTime}, 타임스케일: {Time.timeScale}");

                //     // 0.5f(반) 지점 -> 슬로우 시작 & 조준선/슬라이더 켜기
                //     if (!isSlowApplied && stateInfo.normalizedTime >= 0.5f)
                //     {
                //         isSlowApplied = true; // 슬로우 시작

                //         // 현재 패턴 받아치기 성공 횟수에 따라 슬로우가 더 빨라지게 함
                //         float tProgress = (float)currentSuccessCount / Mathf.Max(1, _maxRepetitions - 1);
                //         currentSlowTimeScale = Mathf.Lerp(FirstSlowTimeScale, FinalSlowTimeScale, tProgress);

                //         // 슬로우 적용
                //         Time.timeScale = currentSlowTimeScale;
                //         Time.fixedDeltaTime = 0.02f * Time.timeScale;

                //         // 크로스헤어 UI 켜기, 현재 오프셋에 따른 타겟 존 위치 업데이트, 발사 가능하게 하기
                //         // panicCrosshair.ToggleCrosshair(true);
                //         panicCrosshair.SetTargetZonePos(randomOffsetDir);
                //         panicCrosshair.SetFireAvailable(true);

                //         // 발사 가능 남은 시간 슬라이더 UI 보이게 하기 & 1로 초기화
                //         if (fireAvailableTimeSlider != null)
                //         {
                //             fireAvailableTimeSlider.gameObject.SetActive(true);
                //             fireAvailableTimeSlider.value = 1f;
                //         }
                //         Debug.Log("!!! " + fireAvailableTimeSlider.value);
                //     }

                //     // 0.5f 지점 지나서 슬로우 적용된 동안 -> 애니메이션 normalizedTime에 직접 슬라이더 동기화
                //     if (isSlowApplied)
                //     {
                //         timer += Time.deltaTime;

                //         // 0.5f 지점부터 0.8f 지점(거의 물 때) 도달하기까지 현재 애니메이션 진행 비율
                //         float progress = Mathf.InverseLerp(0.5f, 0.8f, stateInfo.normalizedTime);

                //         // 발사 가능 남은 시간 슬라이더 UI에 남은 시간 업데이트
                //         if (fireAvailableTimeSlider != null) fireAvailableTimeSlider.value = 1f - progress;
                //         Debug.Log("??? " + fireAvailableTimeSlider.value);

                //         // 시간 안에 눌렀는데, 구역 안이 아니라서 성공하지 못한 경우 -> 재시도 허용
                //         if (isFiredInTime && !isSuccessInZone)
                //         {
                //             isFiredInTime = false; // 플래그 리셋하여 남아있는 시간 동안 다시 발사 가능하게 함
                //         }
                //     }

                // // 0.8f 지점(거의 물 때) -> 시간 초과로 게임 오버
                // if (stateInfo.normalizedTime >= 0.8f)
                // {
                //     if (fireAvailableTimeSlider != null) fireAvailableTimeSlider.value = 0f; // 슬라이더 UI 확실하게 0으로 만듦
                //     isTimedOut = true; // 시간 초과 변수 true
                //     break; // 판정 검사 종료 -> 아래에서 최종 판정
                // }
                // }

                yield return null;
            }

            // 발사 가능 남은 시간 슬라이더 UI 비활성화
            if (fireAvailableTimeSlider != null) fireAvailableTimeSlider.gameObject.SetActive(false);

            // 최종 판정

            // 실패: 시간 초과나 성공 영역 맞추지 못한 경우 게임 오버
            if (isTimedOut || !isSuccessInZone)
            {
                yield return StartCoroutine(GameOverSequenceCoroutine()); // 게임 오버 관련 연출 코루틴 실행
                yield break; // 현재 패턴 루프 코루틴은 그냥 종료
            }

            // 성공

            currentSuccessCount++; // 성공 횟수 증가
            // panicCrosshair.ToggleCrosshair(false); // 크로스헤어 UI 끄기

            // 시간 배속 복원
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // 연출 (능력 O -> 가시 공격으로 인한 폭발 / X -> 플레이어가 기포 발사)
            if (GameManager.instance.IsAbilityActive)
                PlayerManager.Instance.playerAbility.ExplosionParticle.Play(); // 폭발 효과 재생
            else
                bubbleVFX.Play();

            // 괴물 맞고 후퇴
            monsterAnimator.SetTrigger("GetHit"); // 맞는 애니메이션 재생
            monsterTransform.DOMove(calculatedFarPos, 0.5f).SetEase(Ease.OutQuad); // 아까 오프셋 적용해서 계산한 먼 위치로 다시 이동

            // 현재 성공 횟수가 패턴 반복 횟수와 같다면 (= 패턴 모두 성공했다면)
            if (currentSuccessCount == _maxRepetitions)
                break; // 루프 종료

            // 시야가 가려졌다가 차차 걷히는 대기 시간
            yield return new WaitForSeconds(0.5f);
        }

        // 최종 패턴 클리어
        OnPatternClear();
    }

    /// <summary>
    /// 실패 시 게임 오버 연출 코루틴 -> 먹히면서 눈 감기고 게임 오버
    /// </summary>
    private IEnumerator GameOverSequenceCoroutine()
    {
        panicCrosshair.ToggleCrosshair(false); // 크로스헤어 UI 끄기
        Time.timeScale = 0.05f; // 시간 느리게 하기
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(0.1f); // 입이 닫히는 아주 짧은 타이밍 대기

        // 연출 시작
        Sequence gameOverSequence = DOTween.Sequence();
        gameOverSequence.SetUpdate(true); // timeScale 영향을 받지 않도록 unscaled time 설정

        // 눈 감기는 듯한 UI 연출
        gameOverSequence.Append(topBlackScreen.DOAnchorPosY(-540f, 0.15f).SetEase(Ease.OutQuad));
        gameOverSequence.Join(bottomBlackScreen.DOAnchorPosY(540f, 0.15f).SetEase(Ease.OutQuad));

        // 오디오 페이드 아웃
        gameOverSequence.Join(DOTween.To(() => _audioSource.volume, x => _audioSource.volume = x, 0f, 1f));

        // 해당 연출이 완전히 끝날 때까지 코루틴 대기
        yield return gameOverSequence.WaitForCompletion();

        // 슬로우 해제 (남은 몇 프레임동안 정상 속도로 입이 콱 닫힘)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        yield return new WaitForSeconds(1f); // 1초 후

        // 게임 오버
        Debug.Log("게임 오버! 플레이어가 괴물에게 삼켜졌습니다.");
        GameManager.instance.GameOver(EEndingType.MonsterDeath);
        yield break;
    }

    /// 패턴 클리어 시 -> 
    private void OnPatternClear()
    {
        Debug.Log("<color=cyan>★ 최종 패턴 클리어!</color>");

        _isClear = true; // 클리어 변수 true
        IsPatternRunning = false; // 패턴 진행 중 아님으로 설정

        DOTween.To(() => finalPatternVolume.weight, x => finalPatternVolume.weight = x, 0f, 1f); // 볼륨 되돌리기
        DOTween.To(() => _currentRig.weight, x => _currentRig.weight = x, 0f, 1f); // 리그 타겟 비활성화
        DOTween.To(() => _audioSource.volume, x => _audioSource.volume = x, 0f, 2f); // 오디오 페이드 아웃

        monsterTransform.gameObject.SetActive(false); // 괴물 비활성화
        finalPatternUI.SetActive(false); // UI 비활성화

        // 시간 원래대로 되돌리기
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // 크로스헤어 UI 끄기
        panicCrosshair.ToggleCrosshair(false);

        // 플레이어 이동 가능 & 카메라 조작 가능
        deepSeaPlayerMove.SetMoveable(true);
        deepSeaCameraController.SetInputEnabled(true);
        deepSeaCameraController.cameraLight.color = Color.white; // 카메라 손전등 하얀 빛으로 되돌리기

        // 능력 사용 게임일 때 -> 애니메이션 원상 복구, 그에 따른 카메라 위치 조정
        if (GameManager.instance.IsAbilityActive)
        {
            Vector3 newCamPos = deepSeaCameraController.CameraPos.position;
            newCamPos.z -= 0.25f;
            deepSeaCameraController.CameraPos.position = newCamPos;
            PlayerManager.Instance.playerMoveBase.animator.SetLayerWeight(2, 0f);
        }
    }
}