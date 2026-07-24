using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TelegraphKey : PuzzleController, IInteractable
{
    [SerializeField] private GameObject telegraphUI; // 통신 UI
    [SerializeField] private GameObject morseCodeChartUI; // 모스부호 표
    [SerializeField] private GameObject communicationTextUI; // 하단 통신 텍스트 UI
    [SerializeField] private Item morseCodeChartItem; // 모스부호 표 아이템
    [SerializeField] private Renderer communicationLightRenderer; // 통신 신호등(모스부호 누르는 거에 따라 켜짐)
    [SerializeField] private TextMeshProUGUI contextText; // 통신 내용 텍스트
    [SerializeField] private RadarController radarController; // 레이더 컨트롤러
    [SerializeField] private Transform communicationText; // 통신 텍스트

    [Header("송신기 모션")]
    [SerializeField] private Transform transmitterTransform; // 송신기(들고 누르는 거) 트랜스폼
    [SerializeField] private Transform transmitterOriginalTransform; // 송신기 원래 위치 트랜스폼
    [SerializeField] private Transform transmitterPickUpTransform; // 송신기 들었을 때 위치 트랜스폼
    [SerializeField] private Transform transmitterButtonTransform; // 송신기 버튼 트랜스폼
    [SerializeField] private TwoBoneIKConstraint rightHandIK; // 오른손 IK
    private float _buttonOriginalY = 0.09396236f;
    private float _buttonPressY = 0.09f;
    private Sequence _buttonSequence;
    private float _transmitterWaypointZPos = 0.215f;


    [Header("Audio")]
    [SerializeField] private AudioSource morseAudioSource;
    [SerializeField] private AudioSource noiseAudioSource;

    private const string HAS_SIGNAL = ".... .ㅡ ...*.ㅡㅡㅡㅡ ㅡㅡㅡㅡㅡ*...ㅡㅡ ㅡㅡㅡㅡㅡ*..ㅡㅡㅡ ㅡㅡ...*...ㅡㅡ ㅡ...."; // HAS 10 30 27 36

    // 퍼즐 컨트롤러 필수 변수
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    // 통신 신호등
    private Material _communicationLightMaterial; // 통신 신호등 머티리얼
    private Color _shortPressColor = new Color(191f / 255f, 40f / 255f, 0f) * 4f; // 주황색 (기본)
    private Color _longPressColor = new Color(191f / 255f, 0f, 0f) * 4f; // 빨간색 (장음 전환 시)

    // 소리 관련
    private Coroutine _audioFadeCoroutine;
    private float _fadeOutDuration = 0.05f; // 소리가 꺼질 때 부드럽게 감쇄되는 시간 (초)
    private float _maxAudioVolume; // 오디오 소스의 기본 볼륨 저장용

    // 필요 변수
    private Coroutine _holdCheckCoroutine = null; // 홀드 체크 코루틴(시간 계산)
    private float _clickStartedTime; // 클릭 시작 시간(누른 시간 계산용)
    private float _holdThreshold = 0.2f;
    private float _maxHoldDuration = 0.3f;
    private StringBuilder _currentWord = new StringBuilder();
    public bool IsSubmarineLeft { get; set; } = false; // 본부 잠수함이 떠났는지 여부
    public bool IsSuccessed { get; private set; } = false;
    private bool _isHoldForceOver = false;
    private bool _isResponding = false; // 본부가 응답 중 여부
    public bool IsCommunicating { get; private set; } = false; // 통신 중 여부(심해 괴물 자극 판단에 씀)
    private Coroutine _currentPlayRecordingCoroutine = null; // 현재 녹음 재생 중인 코루틴
    private Coroutine _currentPlayMorseCoroutine = null; // 현재 모스부호 재생 코루틴
    private Coroutine _currentTypeTextCoroutine = null; // 현재 타이핑 코루틴

    // 세션(통신) 자동 종료
    private Coroutine _autoSessionEndCheckCoroutine = null; // 세션 자동 종료 검사 코루틴
    private float _autoSessionEndTime = 5f; // 세션 자동 종료 시간 (5초 동안 입력 없으면 종료)

    private bool _isPuzzleInputLocked = false;

    public bool IsBrokenWithJumpscare { get; private set; } = false;

    [SerializeField] private Renderer bodyRenderer;

    private void Awake()
    {
        telegraphUI.SetActive(false); // 통신 UI 비활성화
        morseCodeChartUI.SetActive(false); // 모스부호 UI 비활성화

        // 통신 조명 머티리얼 가져오기 & 조명 끄기
        _communicationLightMaterial = communicationLightRenderer.material;
        _communicationLightMaterial.DisableKeyword("_EMISSION");

        // 소리 초기화
        _maxAudioVolume = morseAudioSource.volume;
        morseAudioSource.loop = true; // 코드가 실행될 때 확실하게 루프 켜기
        morseAudioSource.volume = 0f;

        // 데이터와 화면 UI를 깨끗하게 비워줌
        _currentWord.Clear();
        UpdateMorseDisplayUI();
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (IsBrokenWithJumpscare) return LocalizationHelper.GetLocalizedInteractText("Interact/PermanentFailure");

        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 전력 필요
            return LocalizationHelper.GetLocalizedInteractText("Interact/PowerRestorationRequired");

        return LocalizationHelper.GetLocalizedInteractText("Interact/UseMorseRadio", "E");
    }

    public void Interact()
    {
        if (IsBrokenWithJumpscare) return;

        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        ActivatePuzzle();
    }
    #endregion

    #region PuzzleController
    public override void ActivatePuzzle()
    {
        SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화
        itemEquipController.UnequipItem(); // 아이템 장착 해제
        SubmarineInGameManager.instance.InteractorUI.SetActive(true); // 상호작용 UI 활성화

        telegraphUI.SetActive(true); // 통신 UI 활성화
        communicationTextUI.SetActive(false); // 하단 통신 텍스트 UI 비활성화
        // 모스부호 표 아이템 갖고 있는지 여부에 따라 모스부호 표 UI 활성화 여부 설정
        bool hasMorseCodeChartItem = inventoryManager.CheckHasItemInInventory(morseCodeChartItem);
        morseCodeChartUI.SetActive(hasMorseCodeChartItem);

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        if (IsSuccessed)
            KeyE.performed += OnKeyEPerformed;
        else
        {
            Click.started += OnClickStarted;
            Click.canceled += OnClickCanceled;
            RightClick.performed += OnRightClickPerformed;
        }

        PickUpTransmitter(); // 송신기 들기
    }

    public override void ExitPuzzle()
    {
        Click.started -= OnClickStarted;
        Click.canceled -= OnClickCanceled;
        RightClick.performed -= OnRightClickPerformed;
        if (IsSuccessed)
            KeyE.performed -= OnKeyEPerformed;

        // 데이터와 화면 UI를 깨끗하게 비워줌
        _currentWord.Clear();
        UpdateMorseDisplayUI();

        telegraphUI.SetActive(false); // 통신 UI 비활성화
        SubmarineInGameManager.instance.SetActiveInGameUI(true); // 인게임 UI 활성화

        // 플레이어가 모스 부호를 입력 중이었을 때 -> 피드백 종료
        if (_holdCheckCoroutine != null)
        {
            StopFeedback();
            _holdCheckCoroutine = null;
        }

        // 녹음 재생 중이었을 때 -> 종료 (맨 처음 응답은 녹음이 아니므로 퍼즐 종료할 때 종료하지 않음)
        if (_currentPlayRecordingCoroutine != null)
        {
            StopRecording();
        }

        // 세션 자동 종료 코루틴 실행 중이면 중지 (종료됐으니까)
        if (_autoSessionEndCheckCoroutine != null)
        {
            StopCoroutine(_autoSessionEndCheckCoroutine);
        }

        // // 퍼즐 인풋 잠금 일단 해제 (다른 퍼즐에 영향 주지 않기 위해서. 만약, 다시 퍼즐에 들어올 때 아직 잠겨져 있어야 하는 상태면 잠글 것임)
        // Click.Enable();
        // RightClick.Enable();

        // 송신기 내려놓기
        PutDownTransmitter();
    }
    #endregion

    #region 입력 이벤트 함수
    /// <summary>
    /// 좌클릭 시작 -> 한 글자 입력 시작
    /// </summary>
    /// <param name="ctx"></param>
    public void OnClickStarted(InputAction.CallbackContext ctx)
    {
        // 다른 입력 금지
        RightClick.Disable();
        KeyE.Disable();

        PressButton(true);

        _clickStartedTime = Time.time;

        _isHoldForceOver = false; // 홀드 강제 종료 여부 초기화

        PlayFeedback(); // 소리 & 조명 ON

        // 세션 자동 종료 검사 코루틴 시작 (진행 중이었으면 종료하고 다시 시작)
        if (_autoSessionEndCheckCoroutine != null)
        {
            StopCoroutine(_autoSessionEndCheckCoroutine);
        }
        _autoSessionEndCheckCoroutine = StartCoroutine(AutoSessionEndCheckCoroutine());

        // 홀드 체크 코루틴 시작
        _holdCheckCoroutine = StartCoroutine(HoldCheckCoroutine());
    }

    /// <summary>
    /// 좌클릭 끝 -> 한 글자 입력 완료
    /// </summary>
    /// <param name="ctx"></param>
    public void OnClickCanceled(InputAction.CallbackContext ctx)
    {
        // 다른 입력 금지 해제
        RightClick.Enable();
        KeyE.Enable();

        PressButton(false);

        if (_isHoldForceOver)
        {
            Debug.Log("이미 0.3초 제한으로 종료된 입력이므로 손을 떼도 무시합니다.");
            return;
        }

        // 홀드 체크 코루틴 종료
        if (_holdCheckCoroutine != null)
        {
            StopCoroutine(_holdCheckCoroutine);
            _holdCheckCoroutine = null;
        }

        // 누른 시간 검사
        float duration = Time.time - _clickStartedTime;
        Debug.Log($"버튼을 유지한 시간: {duration}초");

        // 누른 시간에 따라 탭 - 단음 / 홀드 - 장음 판정
        if (duration < _holdThreshold)
            ExecuteTapLogic(); // 단음(.) 추가
        else
            ExecuteHoldLogic(); // 장음(ㅡ) 추가

        // 피드백 종료
        StopFeedback();
    }

    // 마우스 우클릭 -> 전체 메시지 제출(메시지 송신)
    public void OnRightClickPerformed(InputAction.CallbackContext ctx)
    {
        // 세션 자동 종료 코루틴 실행 중이면 중지
        if (_autoSessionEndCheckCoroutine != null)
        {
            StopCoroutine(_autoSessionEndCheckCoroutine);
        }

        // 아무것도 입력 안 했을 때는 무시
        if (_currentWord.Length == 0) return;

        // 완성된 모스부호 전송
        StartCoroutine(SubmitCode(_currentWord.ToString()));
    }

    // E키 -> 녹음 재생
    public void OnKeyEPerformed(InputAction.CallbackContext ctx)
    {
        PlayRecording();
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 송신기 들기
    /// </summary>
    private void PickUpTransmitter()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(transmitterTransform.DOLocalMoveZ(_transmitterWaypointZPos, 0.5f)
        .SetEase(Ease.OutQuad));

        seq.Append(transmitterTransform.DOMove(transmitterPickUpTransform.position, 1f)
        .SetEase(Ease.OutQuad));
        seq.Join(transmitterTransform.DORotateQuaternion(transmitterPickUpTransform.rotation, 1f)
        .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            base.StartPuzzle();

            communicationTextUI.SetActive(true); // 하단 통신 텍스트 UI 활성화

            // 현재 퍼즐 인풋 잠금 여부에 따라 다시 설정
            SetMorseInputLock(_isPuzzleInputLocked);
        });
    }

    /// <summary>
    /// 송신기 내려놓기
    /// </summary>
    private void PutDownTransmitter()
    {
        SetInputLock(true);

        Sequence seq = DOTween.Sequence();

        Vector3 waypointPos = transmitterOriginalTransform.localPosition;
        waypointPos.z = _transmitterWaypointZPos;

        seq.Append(transmitterTransform.DOLocalMove(waypointPos, 1f)
        .SetEase(Ease.OutQuad));
        seq.Join(transmitterTransform.DORotateQuaternion(Quaternion.identity, 1f)
        .SetEase(Ease.OutQuad));

        seq.Append(transmitterTransform.DOLocalMoveZ(0f, 0.5f)
        .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            SetInputLock(false);

            base.ExitPuzzle();
        });
    }

    /// <summary>
    /// 버튼 누르기/떼기
    /// </summary>
    /// <param name="isPress">누르는 여부</param>
    private void PressButton(bool isPress)
    {
        float yPos = isPress ? _buttonPressY : _buttonOriginalY;

        if (_buttonSequence != null && _buttonSequence.IsActive())
        {
            _buttonSequence.Kill();
        }

        _buttonSequence = DOTween.Sequence();

        _buttonSequence.Append(transmitterButtonTransform.DOLocalMoveY(yPos, 0.05f)
        .SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 세션 자동 종료 검사 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator AutoSessionEndCheckCoroutine()
    {
        IsCommunicating = true;
        yield return new WaitForSeconds(_autoSessionEndTime); // 세션 자동 종료 시간이 지날 때까지 대기 (지나지 않으면 이후 로직 실행 X)

        IsCommunicating = false;

        SetMorseInputLock(true); // 모스 부호 입력 막음

        // 자동 종료 이유 설명
        yield return StartCoroutine(TypeText("5초 간 미입력. 미전송. 통신 자동 종료.", 0.05f));
        yield return new WaitForSeconds(1f);

        _currentWord.Clear();
        UpdateMorseDisplayUI();

        SetMorseInputLock(false); // 모스 부호 입력 막은 거 해제

        _autoSessionEndCheckCoroutine = null;
    }

    /// <summary>
    /// 홀드 체크 코루틴(시간 계산해서 홀드 판정 시 빨간 불 켜줌)
    /// </summary>
    /// <returns></returns>
    private IEnumerator HoldCheckCoroutine()
    {
        // 장음 기준 시간만큼 조용히 대기
        yield return new WaitForSeconds(_holdThreshold);

        SetLampFeedback(_longPressColor); // 빨간 불로 전환
        Debug.Log("<color=red>[실시간 알림]</color> 장음 도달!");

        // 최대 누르기 시간(0.3초)까지 남은 시간만큼 더 대기 (0.3초 - 0.2초 = 0.1초)
        float remainingTime = _maxHoldDuration - _holdThreshold;
        yield return new WaitForSeconds(remainingTime);

        // 0.3초가 지날 때까지 플레이어가 손을 안 뗐다면 여기서 강제 컷
        Debug.Log("<color=yellow>[타임아웃]</color> 0.3초 초과! 입력을 강제로 중단합니다.");

        _isHoldForceOver = true; // 홀드 강제 종료 여부 true로 설정

        ExecuteHoldLogic();  // 장음 추가
        StopFeedback();      // 피드백 종료

        _holdCheckCoroutine = null;
    }

    /// <summary>
    /// 단음 입력
    /// </summary>
    private void ExecuteTapLogic()
    {
        _currentWord.Append(".");

        // UI 텍스트가 연결되어 있다면 실시간으로 갱신
        UpdateMorseDisplayUI();

        // 개발자 디버그용 로그 (노란색으로 현재까지 쌓인 기호 표시)
        Debug.Log($"<color=yellow>[단음 . 입력]</color> 현재 기호: {_currentWord}");
    }

    /// <summary>
    /// 장음 입력
    /// </summary>
    private void ExecuteHoldLogic()
    {
        _currentWord.Append("ㅡ");

        // UI 텍스트가 연결되어 있다면 실시간으로 갱신
        UpdateMorseDisplayUI();

        // 개발자 디버그용 로그
        Debug.Log($"<color=red>[장음 ㅡ 입력]</color> 현재 기호: {_currentWord}");
    }

    /// <summary>
    /// UI 텍스트 갱신 함수
    /// </summary>
    private void UpdateMorseDisplayUI()
    {
        if (contextText != null)
        {
            contextText.text = _currentWord.ToString();
        }
    }

    /// <summary>
    /// 클릭된 순간 실행 (소리 ON, 단음색 ON)
    /// </summary>
    private void PlayFeedback()
    {
        // 페이드 아웃 코루틴 종료
        if (_audioFadeCoroutine != null)
        {
            StopCoroutine(_audioFadeCoroutine);
            _audioFadeCoroutine = null;
        }

        // 💡 처음 켤 때는 기본 주황색(shortPressColor)으로 세팅
        SetLampFeedback(_shortPressColor);

        if (morseAudioSource != null)
        {
            morseAudioSource.volume = _maxAudioVolume;
            if (!morseAudioSource.isPlaying) morseAudioSource.Play();
        }
    }

    /// <summary>
    /// 불빛과 텍스처 Emission을 동시에 제어
    /// </summary>
    /// <param name="targetColor"></param>
    private void SetLampFeedback(Color targetColor)
    {
        _communicationLightMaterial.SetColor("_EmissionColor", targetColor);
        _communicationLightMaterial.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// 버튼에서 손을 뗀 순간 실행 (소리 OFF, 불빛 OFF) ───
    /// </summary>
    private void StopFeedback()
    {
        // 소리 끄기 (뚝 끊기면 귀가 아프니 부드럽게 줄어들며 꺼지도록 코루틴 실행)
        if (morseAudioSource != null && morseAudioSource.isPlaying)
        {
            if (_audioFadeCoroutine != null) StopCoroutine(_audioFadeCoroutine);
            _audioFadeCoroutine = StartCoroutine(FadeOutAudio());
        }
    }

    /// <summary>
    /// 아주 빠르게 볼륨을 줄여 뚝 끊기는 느낌을 없애는 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeOutAudio()
    {
        float startVolume = morseAudioSource.volume;
        float timer = 0f;

        while (timer < _fadeOutDuration)
        {
            timer += Time.deltaTime;
            morseAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / _fadeOutDuration);
            yield return null;
        }

        // 응답 중이 아니라 플레이어가 입력한 경우에만
        if (!_isResponding)
            _communicationLightMaterial.DisableKeyword("_EMISSION"); // 소리 끝나는 타이밍에 불 끄기

        morseAudioSource.Stop();
        morseAudioSource.volume = _maxAudioVolume; // 다음 재생을 위해 원래 볼륨으로 리셋
    }

    /// <summary>
    /// 모스 부호 입력(+제출) 잠금 여부 설정
    /// </summary>
    /// <param name="isLock">잠금 여부</param>
    private void SetMorseInputLock(bool isLock)
    {
        _isPuzzleInputLocked = isLock;

        if (isLock)
        {
            Click.Disable();
            RightClick.Disable();
        }
        else
        {
            Click.Enable();
            RightClick.Enable();
        }
    }

    /// <summary>
    /// 전체 메시지 제출
    /// </summary>
    /// <param name="morseCode">코드 메시지</param>
    private IEnumerator SubmitCode(string morseCode)
    {
        SetMorseInputLock(true); // 모스부호 입력 막음

        // 데이터와 화면 UI를 깨끗하게 비워줌
        _currentWord.Clear();
        UpdateMorseDisplayUI();

        Debug.Log($"<color=green>[통신 전송 완료]</color> 최종 전송된 부호: {morseCode}");

        // 입력된 모스 부호에 따른 응답 로직
        if (IsSubmarineLeft) // 본부 잠수함이 이미 떠난 경우 -> 응답 없음
        {
            yield return StartCoroutine(TypeText("... ... 응답 없음.", 0.1f));
            SetMorseInputLock(false);
            yield break;
        }

        IsCommunicating = true;

        if (morseCode == "...ㅡㅡㅡ...") // SOS -> 응답 시퀀스 시작
        {
            Debug.Log("SOS 신호 성공! 본부 잠수함 응답 시퀀스 시작.");
            yield return StartCoroutine(SuccessResponse());
            // 모스부호 입력 막은 걸 풀어주지 않음 (이제 더 이상 입력할 일이 없으므로)
        }
        else
        {
            // 코드표에 등록된 모스 부호인지 사전에서 확인
            bool isValidCode = MorseDictionary.MorseToText.ContainsKey(morseCode);

            // 코드표에 있는 모스부호를 보낸 경우 -> SIL (침묵 명령)
            if (isValidCode)
            {
                Debug.Log($"코드표 확인 완료 ({MorseDictionary.MorseToText[morseCode]}). 본부: SIL 송신");
                yield return StartCoroutine(PlayMorseString("... .. .ㅡ.."));
            }
            // 코드표에 없는 잘못된 모스부호를 보낸 경우 -> RPT (재요청)
            else
            {
                Debug.Log("알 수 없는 부호. 본부: RPT 송신");
                yield return StartCoroutine(PlayMorseString(".ㅡ. .ㅡㅡ. ㅡ"));

            }
            SetMorseInputLock(false); // 모스부호 입력 막은 거 해제
        }

        IsCommunicating = false;
    }

    /// <summary>
    /// 성공 응답
    /// </summary>
    /// <returns></returns>
    private IEnumerator SuccessResponse()
    {
        SetInputLock(true); // 나가지 못하도록 입력 막기

        noiseAudioSource.Play(); // 노이즈 재생

        string sosDetails = "SOS... 본부 응답하라. 실험체가 폭주하여 괴물로 변이하였다. 본인을 제외한 전원 사망. 즉시 구출을 요청한다. 반복한다. 즉시 구출을...";

        yield return StartCoroutine(TypeText(sosDetails, 0.1f));
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(TypeText("... ...", 0.1f));
        yield return new WaitForSeconds(1f);
        contextText.text = ""; // 기존 텍스트 초기화

        noiseAudioSource.Stop(); // 노이즈 중지
        SetInputLock(false); // 입력 잠금 해제

        // HAS 문장 수신
        yield return StartCoroutine(PlayMorseString(HAS_SIGNAL));

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(TypeText("[통신 자동 녹음 완료]", 0.1f));

        // yield return new WaitForSeconds(2f);

        // 본부 잠수함이 FIN(..ㅡ. .. ㅡ.)을 보냄
        Debug.Log("본부 잠수함: FIN 송신 (연락 종료)");
        yield return StartCoroutine(PlayMorseString("..ㅡ. .. ㅡ."));

        yield return new WaitForSeconds(1f);

        // 본부 잠수함 떠남
        radarController.LeaveSubmarine();
        IsSuccessed = true;
        // IsSubmarineLeft = true; // 이제 더 이상 신호를 주고받을 수 없도록 플래그 차단

        KeyE.performed += OnKeyEPerformed; // E키 사용 가능
        inventoryManager.UpdateActionText(); // 액션 텍스트 업데이트
    }

    /// <summary>
    /// 본부 잠수함의 모스부호 응답을 소리와 불빛으로 재생하는 코루틴
    /// </summary>
    /// <param name="morsePattern">현재 응답</param>
    /// <returns></returns>
    private IEnumerator PlayMorseString(string morsePattern)
    {
        _isResponding = true;
        noiseAudioSource.Play();

        // 단음/장음 기준 시간 설정
        float dotDuration = 0.2f; // 1단위
        float dashDuration = 0.6f; // 3단위
        float symbolGap = 0.5f; // 1단위 + 0.05f(소리 페이드 아웃)
        float letterGap = 1.2f; // 3단위
        float wordGap = 2.5f; // 7단위

        // 응답 시작 시 초록불 켜주기
        SetLampFeedback(Color.green * 4f);
        yield return new WaitForSeconds(1f);

        foreach (char c in morsePattern)
        {
            if (c == ' ')
            {
                // 글자 사이의 공백
                yield return new WaitForSeconds(letterGap);
                continue;
            }
            else if (c == '*')
            {
                // 단어 사이의 공백
                yield return new WaitForSeconds(wordGap);
                continue;
            }

            // 1. 소리와 불빛 켜기
            morseAudioSource.Play();
            // SetLampFeedback(_shortPressColor); // 본부 신호는 기본 주황색으로 깜빡이게 처리

            // 2. 신호 종류(. 또는 ㅡ)에 따라 정해진 시간만큼 소리 유지
            if (c == '.')
            {
                yield return new WaitForSeconds(dotDuration);
            }
            else if (c == 'ㅡ')
            {
                yield return new WaitForSeconds(dashDuration);
            }

            StopFeedback();

            // 심볼(점, 선) 사이의 공백
            yield return new WaitForSeconds(symbolGap);
        }

        // 신호등, 소리 끄기
        yield return new WaitForSeconds(0.5f);
        _communicationLightMaterial.DisableKeyword("_EMISSION");
        noiseAudioSource.Stop();

        _isResponding = false;
    }

    /// <summary>
    /// 텍스트를 한 글자씩 빠르게 타이핑하는 코루틴
    /// </summary>
    private IEnumerator TypeText(string message, float typingDuration)
    {
        if (contextText == null) yield break;

        contextText.text = ""; // 기존 텍스트 초기화

        // 문자열을 한 글자씩 쪼개서 붙여나갑니다.
        foreach (char letter in message)
        {
            contextText.text += letter;

            // typingAudioSource.PlayOneShot(typeClickSound, 0.5f);

            yield return new WaitForSeconds(typingDuration);
        }
    }

    /// <summary>
    /// 녹음 재생
    /// </summary>
    public void PlayRecording()
    {
        _currentPlayRecordingCoroutine = StartCoroutine(PlayRecordingCoroutine());
    }

    /// <summary>
    /// 녹음 재생 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayRecordingCoroutine()
    {
        noiseAudioSource.Play();
        contextText.text = ""; // 기존 텍스트 초기화

        _currentPlayMorseCoroutine = StartCoroutine(PlayMorseString(HAS_SIGNAL));
        yield return _currentPlayMorseCoroutine;
        _currentPlayMorseCoroutine = null;

        yield return new WaitForSeconds(1f);

        _currentTypeTextCoroutine = StartCoroutine(TypeText("[녹음 재생 완료]", 0.1f));
        yield return _currentTypeTextCoroutine;
        _currentTypeTextCoroutine = null;

        noiseAudioSource.Stop();

        _currentPlayRecordingCoroutine = null;
    }

    private void StopRecording()
    {
        StopCoroutine(_currentPlayRecordingCoroutine);
        _currentPlayRecordingCoroutine = null;

        if (_currentPlayMorseCoroutine != null)
        {
            StopCoroutine(_currentPlayMorseCoroutine);
            _currentTypeTextCoroutine = null;
            _communicationLightMaterial.DisableKeyword("_EMISSION");
            _isResponding = false;

        }
        if (_currentTypeTextCoroutine != null)
        {
            StopCoroutine(_currentTypeTextCoroutine);
            _currentPlayRecordingCoroutine = null;
        }

        noiseAudioSource.Stop();
        _currentPlayRecordingCoroutine = null;
    }

    /// <summary>
    /// 점프스케어 시 즉시 파괴를 통한 고장
    /// </summary>
    public void BrokeWithJumpscare()
    {
        IsBrokenWithJumpscare = true;
        bodyRenderer.material.color = Color.black; // 검정색으로 변경
    }
    #endregion
}
