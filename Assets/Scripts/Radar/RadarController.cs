using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadarController : PuzzleController
{
    public RadarTarget deepSeaMonster = new RadarTarget(TargetType.DeepSeaMonster); // 심해 괴물 (데이터)
    public RadarTarget submarine2 = new RadarTarget(TargetType.Submarine2); // 다른 잠수함 (데이터)

    [SerializeField] private TorpedoTube loadAvailableTorpedoTube; // 탑재 가능한 어뢰 발사관
    [SerializeField] private TelegraphKey telegraphKey; // 신호기(통신)

    [Header("Sound")]
    [SerializeField] private AudioClip deepSeaMonsterCloseSound;
    [SerializeField] private AudioClip deepSeaImpactSound; // 충격(흔들림) 소리
    [SerializeField] private AudioClip radarOpenSound;
    [SerializeField] private AudioClip radarCloseSound;
    [SerializeField] private AudioClip modeChangeFailSound;
    [SerializeField] private AudioClip modeChangeSuccessSound;
    [SerializeField] private AudioClip submarineLeaveSound;
    [SerializeField] private AudioSource monsterAudioSource;
    public AudioSource MonsterAudioSource => monsterAudioSource;
    public AudioSource RadarControlPanelAudioSource { get; set; }

    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => false;

    // 수치
    public float RadarRadius { get; set; } = 0.225f; // 레이더 화면 너비 절반
    public float MaxPlanarDistance { get; private set; } = 100f; // 원점으로부터의 2차원 상 최대 거리
    public float MaxHeight { get; private set; } = 50f;  // 최대 높이(Z 최대 표현값)

    // 기본
    public bool IsUpdateStart { get; set; } = false;
    private bool _isTurnOn = false; // 켜졌는지 여부 (처음 딱 한 번 켜면, 이후 계속 켜져있도록 할 것임)

    // 수동 전환
    private bool _isManualModeActive = false; // 수동 모드 활성화 여부
    private bool _canType = true; // 코드 입력 가능 여부
    private const string CORRECT_CODE = "XJHU"; // 정답 코드
    private string _currentInput = ""; // 현재 입력
    public const int MAX_LENGTH = 4; // 입력 제한 길이

    // 선택
    public Vector2 CurrentSelectUIPos { get; set; } // 현재 선택 위치
    public bool IsPlanarPosSelected { get; private set; } = false;
    public bool IsHeightSelected { get; private set; } = false;
    private bool _isSelectingHeight = false;
    private float _currentHeightLeverAngle = -45f;
    private float _currentSelectHeight = 0f;

    // 괴물
    public int CurrentMonsterPeriodIndex { get; set; } = 0; // 심해 괴물 주기 인덱스(몇번째 출현인가)
    public float MonsterStartAngle { get; set; } = 50f; // 시작 각도(심해 괴물의 시작 위치 변경 시 사용)
    private float _monsterTimer = 0f; // 심해 괴물의 타이머(다시 나타날 때 0으로 초기화)
    public int LastShakeMinute { get; set; } = -1; // 마지막으로 카메라가 흔들린 분(시간)
    private float _monsterCloseTime = 300f; // 심해 괴물 가까워져서 소리 나기 시작하는 시간: 5분
    public float CurrentMonsterAppearTime { get; set; } = 0f; // 현재 심해 괴물 등장 시간

    // 발사
    public bool IsFiring { get; set; } = false;
    public int CurrentTorpedoIndex { get; set; } = 0; // 현재 어뢰 인덱스
    public TorpedoState CurrentTorpedoState { get; private set; } // 현재 어뢰 상태

    // 위험 거리 경보
    private float _dangerDistance = 90f; // 위험 거리
    private float _maxSpeedMultiplier = 2f; // 통신 시 괴물이 빨라질 최대 배속
    private float _accelerationSpeed = 1f; // 속도가 변하는 부드러운 정도 (Lerp용)
    private float _currentSpeedMultiplier = 1f; // 현재 괴물의 속도 배율
    private float _bonusMonsterTime = 0f; // 통신으로 인해 추가된 패널티 시간 누적치
    private bool _isDangerAlertActive = false; // 경보 상태 플래그

    private RadarDisplay _radarDisplay;
    private RadarLauncher _radarLauncher;

    private void Awake()
    {
        _radarDisplay = GetComponent<RadarDisplay>();
        _radarLauncher = GetComponent<RadarLauncher>();

        deepSeaMonster.SetRadarController(this);
        submarine2.SetRadarController(this);

        _radarDisplay.SetCodeInputText(_currentInput);
    }

    public override void Start()
    {
        base.Start();

        SetCurrentTorpedoState(TorpedoState.Normal);

        // 선택 여부 초기화
        SelectPlanarPos(false);
        SelectHeight(true); // 높이는 선택됨으로 설정

        GameTime.Instance.ReserveEvent(3600f, LeaveSubmarine, false);
    }

    private void OnEnable()
    {
        TorpedoLoadPanel.OnLoaded += SetCurrentTorpedoState;
        loadAvailableTorpedoTube.OnDoorOpenStateChanged += SetTorpedoStateByDoor;
    }

    private void Update()
    {
        // 정지 중이거나 아직 업데이트 시작 안됐을 때(전력 복구 X) -> 아무것도 안 함
        if (GameManager.instance.IsPausing || !IsUpdateStart)
            return;

        // 심해 괴물 타이머 계산 (보여지는 중 아닐 때는 사라졌을 때이므로 계산 X) & 위치 갱신
        if (deepSeaMonster.IsCurrentActive)
        {
            // 가속분만큼 패널티 시간 누적
            if (_currentSpeedMultiplier > 1f)
            {
                _bonusMonsterTime += Time.deltaTime * (_currentSpeedMultiplier - 1f);
            }

            // 가속도가 반영된 타이머 계산 및 괴물 위치 업데이트
            _monsterTimer = GameTime.Instance.TimeSinceStart - CurrentMonsterAppearTime + _bonusMonsterTime;
            deepSeaMonster.UpdatePosition(_monsterTimer);
            deepSeaMonster.UpdatePosUI();

            // 심해 괴물과 현 잠수함 사이 2차원 거리(화면 기준) 계산
            float currentDistance = Vector2.Distance(deepSeaMonster.CurrentPos, Vector2.zero);

            // 괴물이 위험 거리 내에 있는지 판정
            bool isDanger = currentDistance <= _dangerDistance;

            // 위험 
            if (isDanger && telegraphKey.IsCommunicating)
            {
                // 가까이 있고 통신 중이면 최대 속도로 부드럽게 가속
                _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, _maxSpeedMultiplier, _accelerationSpeed * Time.deltaTime);
            }
            else if (_currentSpeedMultiplier > 1f)
            {
                // 멀어지거나 통신이 끝나면 원래 속도(1배속)로 부드럽게 감소
                _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, 1f, _accelerationSpeed * Time.deltaTime);
            }

            // 위험 -> 빨간색 배경 / 아니면 기본 초록색 배경
            if (isDanger)
            {
                if (!_isDangerAlertActive)
                {
                    _isDangerAlertActive = true;
                    _radarDisplay.SetDangerBackround(true);
                    // 깜빡거림 추가 예정
                    Debug.Log($"<color=red>[경보]</color> 괴물 접근 감지! 현재 거리: {currentDistance}m (경보 발령)");
                }
            }
            else
            {
                if (_isDangerAlertActive)
                {
                    _isDangerAlertActive = false;
                    _radarDisplay.SetDangerBackround(false);
                    Debug.Log($"<color=green>[안정]</color> 괴물이 멀어졌습니다. 현재 거리: {currentDistance}m (경보 해제)");
                }
            }


            // 심해 괴물이 잠수함에 다가오기까지 5분 남으면
            if (_monsterTimer >= deepSeaMonster.MonsterPeriods[CurrentMonsterPeriodIndex] - _monsterCloseTime)
            {
                // 현재 심해 괴물이 잠수함에 도착하는 시간이 되면(첫 주기 10분, 그 후 17분) -> 심해 괴물 잠수함에 도착, 게임 종료
                if (_monsterTimer >= deepSeaMonster.MonsterPeriods[CurrentMonsterPeriodIndex])
                {
                    Debug.Log("폭발!!! " + _monsterTimer);
                    AudioManager.Instance.PlayDeepSeaMonsterExplosionSound(); // 폭발 소리
                    GameManager.instance.GameOver(EEndingType.SubmarineExplode);
                    return;
                }

                // 가까워지는 소리 재생
                float v = Mathf.InverseLerp(deepSeaMonster.MonsterPeriods[CurrentMonsterPeriodIndex] - _monsterCloseTime, deepSeaMonster.MonsterPeriods[CurrentMonsterPeriodIndex], _monsterTimer); // 시간에 따라 점점 커짐(최대 소리 - 도달하는 시간대)
                monsterAudioSource.volume = v; // 0~1
                AudioManager.Instance.PlaySoundSafe(monsterAudioSource, deepSeaMonsterCloseSound);

                // 1분 간격으로 카메라 흔들림
                int currentMinute = Mathf.FloorToInt(_monsterTimer / 60f); // 현재 몬스터 시간 분 단위로 변환
                if (currentMinute > LastShakeMinute) // 1분 간격마다만 실행됨
                {
                    LastShakeMinute = currentMinute; // 마지막으로 흔들린 분(시간)을 현재 분(시간)으로 갱신
                    AudioManager.Instance.PlayGlobalOneShot(deepSeaImpactSound); // 충격(흔들림) 소리 재생 (볼륨 달라지지 않음)
                    if (DOTween.IsTweening(Camera.main.transform))
                    {
                        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 포커스 상태로 변경
                        // SubmarineInGameManager.instance.SetFocus(true); // 포커스 <- 카메라 흔들림을 위해서
                        Camera.main.transform.DOShakeRotation(2f, 0.2f, 10, 90f) // 카메라 흔들림
                            .OnComplete(() => // 끝나면
                            {
                                FocusManager.Instance.PopFocusState(); // 이전 포커스 복구
                                // SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제
                            });
                    }
                }
            }
        }

        // 다른 잠수함 위치 갱신
        if (submarine2.IsCurrentActive)
        {
            submarine2.UpdatePosition(GameTime.Instance.TimeSinceStart);
            submarine2.UpdatePosUI();
        }
    }

    private void LateUpdate()
    {
        // 심해 괴물 활성화 중 -> 심해 괴물 좌표 텍스트가 구역 바깥으로 나가서 가려지지 않도록 보정
        if (deepSeaMonster.IsCurrentActive)
            deepSeaMonster.ClampPosTextsInside();
    }

    private void OnDisable()
    {
        TorpedoLoadPanel.OnLoaded -= SetCurrentTorpedoState;
        loadAvailableTorpedoTube.OnDoorOpenStateChanged -= SetTorpedoStateByDoor;
    }

    #region PuzzleController
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        // 인게임 UI 비활성화
        SubmarineInGameManager.instance.SetActiveInGameUI(false);

        // 수동 전환이 아직 되지 않았을 때 -> 키 입력 이벤트 구독(수동 전환 코드 입력 위해서)
        if (!_isManualModeActive)
        {
            Keyboard.current.onTextInput += OnTextInput;
        }
        else
        {
            Click.performed += OnClickPerformed;
            Click.started += OnClickStarted;
            Click.canceled += OnClickCanceled;
            Point.performed += OnPoint;
        }

        // 화면 꺼져있으면 -> 켜기
        if (!_isTurnOn)
            SetTurnOnScreen(true);

        // 인벤토리 숨기기
        inventoryManager.CloseInventory();
        itemEquipController.UnequipItem(); // 아이템 장착 해제

        // 선택 여부 초기화
        SelectPlanarPos(false);
        SelectHeight(true); // 높이는 선택됨으로 설정
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        // 인게임 UI 활성화
        SubmarineInGameManager.instance.SetActiveInGameUI(true);

        // 수동 전환이 아직 되지 않았을 때 -> 키 입력 이벤트 구독 해제
        if (!_isManualModeActive)
        {
            Keyboard.current.onTextInput -= OnTextInput;
            _currentInput = "";
            _radarDisplay.SetCodeInputText(_currentInput);
        }
        else
        {
            Click.performed -= OnClickPerformed;
            Click.started -= OnClickStarted;
            Click.canceled -= OnClickCanceled;
            Point.performed -= OnPoint;
        }

        // 화면 끄지 않음

        // 선택 여부 초기화
        SelectPlanarPos(false);
        SelectHeight(true); // 높이는 선택됨으로 설정

        // 인벤토리 다시 보이게 하기
        inventoryManager.OpenInventory();

        // 발사 애니메이션 재생 중지
        _radarLauncher.StopFireAnimationLoop();
    }
    #endregion

    #region 입력 이벤트 함수
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // 발사 버튼을 누른 경우
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == _radarDisplay.fireButton)
            {
                // 발사 가능 -> 발사
                if (CheckFireAvailable())
                {
                    _radarLauncher.RealFire();
                }
            }
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == _radarDisplay.heightLever)
            {
                _isSelectingHeight = true;
                _radarLauncher.StopFireAnimationLoop(); // 레버 움직이기 시작하면 발사 애니메이션은 재생 중지
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (_isSelectingHeight)
        {
            _isSelectingHeight = false;
            if (CheckFireAvailable())
            {
                _radarLauncher.PlayFireAnimationLoop(); // 레버 움직이는 게 끝나면 발사 애니메이션 재생
            }
        }
    }

    public override void OnPoint(InputAction.CallbackContext context)
    {
        base.OnPoint(context);

        // 높이 설정 중
        if (_isSelectingHeight)
        {
            // Delta 값을 읽어서 각도 계산
            Vector2 delta = Mouse.current.delta.ReadValue();

            // 0 ~ -90도 제한 로직
            _currentHeightLeverAngle -= delta.y * 0.2f;
            _currentHeightLeverAngle = Mathf.Clamp(_currentHeightLeverAngle, -90f, 0f);

            // 현재 각도(_currentAngle)를 0 ~ 1 사이의 비율(t)로 변환
            float t = Mathf.InverseLerp(0f, -90f, _currentHeightLeverAngle);

            _currentSelectHeight = Mathf.RoundToInt(Mathf.Lerp(-50f, 50f, t)); // 실제 높이로 변환(정수로 반올림)
            float targetY = Mathf.Lerp(0.55f, -0.55f, t); // 비율(t)을 UI 이동 범위(0.55 ~ -0.55)로 변환

            // 높이 UI 갱신(레버, 눈금)
            _radarDisplay.UpdateHeightUI(_currentHeightLeverAngle, targetY);

            SelectHeight(true);
        }
    }
    #endregion

    #region 화면 켜기/끄기
    public void SetTurnOnScreen(bool isTurnOn)
    {
        _isTurnOn = isTurnOn;

        // 레이더 UI 활성화 여부 설정
        _radarDisplay.SetRadarUIActive(isTurnOn);

        if (isTurnOn)
        {
            if (!_isManualModeActive)
                _radarDisplay.SetLockedUIActive(true);

            // 레이더 켜지는 소리 재생
            RadarControlPanelAudioSource.PlayOneShot(radarOpenSound);
        }
        else
        {
            _radarDisplay.SetLockedUIActive(false);

            // 레이더 꺼지는 소리 재생
            RadarControlPanelAudioSource.PlayOneShot(radarCloseSound);
        }
    }
    #endregion

    #region 수동 모드 전환
    /// <summary>
    /// 텍스트 입력될 때 실행되는 함수
    /// </summary>
    /// <param name="c">입력된 문자</param>
    private void OnTextInput(char c)
    {
        // 입력 불가 시 or 게임 정지 중 -> 아무것도 안 하고 종료
        if (!_canType || GameManager.instance.IsPausing) return;

        // 엔터 -> 제출
        if (c == '\n' || c == '\r')
        {
            StartCoroutine(SubmitCoroutine()); // 제출
            return;
        }

        // 백스페이스 -> 지우기
        if (c == '\b')
        {
            if (_currentInput.Length > 0)
                _currentInput = _currentInput[..^1];

            _radarDisplay.SetCodeInputText(_currentInput);

            return;
        }

        // 현재 길이가 4글자 이상이면 -> 더 이상 입력 불가
        if (_currentInput.Length >= MAX_LENGTH)
            return;

        // 알파벳 아닌 경우 -> 입력 불가
        if (!(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z'))
        {
            return;
        }

        // 대문자로 변경 후 현재 입력 텍스트에 추가
        _currentInput += char.ToUpper(c);

        // 로그 텍스트 변경
        _radarDisplay.SetCodeInputText(_currentInput);
    }

    /// <summary>
    /// 제출 코루틴(성공 / 실패)
    /// </summary>
    /// <returns></returns>
    IEnumerator SubmitCoroutine()
    {
        SetInputLock(true);

        if (_currentInput == CORRECT_CODE) // 정답 코드
        {
            // 수동 모드로 전환
            _isManualModeActive = true;

            // 입력 이벤트 구독
            Click.performed += OnClickPerformed;
            Click.started += OnClickStarted;
            Click.canceled += OnClickCanceled;
            Point.performed += OnPoint;

            _radarDisplay.UpdateUIAfterSubmit(true);

            // 키 입력 불가
            _canType = false;
            Keyboard.current.onTextInput -= OnTextInput; // 입력 이벤트 구독 해제

            AudioManager.Instance.PlayGlobalOneShot(modeChangeSuccessSound);

            // 1.5초 대기
            yield return new WaitForSeconds(1.5f);

            // 잠금 UI 제거
            _radarDisplay.SetLockedUIActive(false);

            // 텍스트 초기화
            _currentInput = "";
            _radarDisplay.SetCodeInputText(_currentInput);

            SetMouseRequired(true);
        }
        else // 실패
        {
            // 실패 메시지 띄우기
            _radarDisplay.UpdateUIAfterSubmit(false);

            AudioManager.Instance.PlayGlobalOneShot(modeChangeFailSound);

            // 1.5초 대기(대기하는 동안 입력 불가)
            _canType = false;
            yield return new WaitForSeconds(1.5f);
            _canType = true;

            // 텍스트 초기화
            _currentInput = "";
            _radarDisplay.SetCodeInputText(_currentInput);
            _radarDisplay.ResetHeaderText();
        }

        SetInputLock(false);
    }
    #endregion

    #region 선택
    /// <summary>
    /// 평면 상 위치(좌표) 선택
    /// </summary>
    /// <param name="isSelect">선택 여부</param>
    public void SelectPlanarPos(bool isSelect)
    {
        IsPlanarPosSelected = isSelect;
        _radarDisplay.UpdateFireButtonActive(); // 발사 버튼 활성화 여부 갱신

        if (CheckFireAvailable())
        {
            _radarLauncher.PlayFireAnimationLoop();
        }
    }

    /// <summary>
    /// 높이 선택
    /// </summary>
    /// <param name="isSelect">선택 여부</param>
    public void SelectHeight(bool isSelect)
    {
        IsHeightSelected = isSelect;
        _radarDisplay.UpdateFireButtonActive(); // 발사 버튼 활성화 여부 갱신
    }
    #endregion

    #region 발사
    /// <summary>
    /// 발사 가능 여부 검사
    /// </summary>
    /// <returns>발사 가능 여부</returns>
    public bool CheckFireAvailable()
    {
        if (!IsPlanarPosSelected || !IsHeightSelected || CurrentTorpedoIndex >= 3 || IsFiring || SubmarineInGameManager.instance.IsAlerting || CurrentTorpedoState != TorpedoState.Normal)
            return false;
        else
            return true;
    }

    /// <summary>
    /// 현재 선택 위치 실제 좌표 가져오기
    /// </summary>
    /// <returns>선택 위치 실제 좌표</returns>
    public Vector3 GetCurrentRealSelectPos()
    {
        return new Vector3(
                    CurrentSelectUIPos.x / RadarRadius * MaxPlanarDistance,
                    CurrentSelectUIPos.y / RadarRadius * MaxPlanarDistance,
                    _currentSelectHeight);
    }

    /// <summary>
    /// 해당 실제 좌표를 UI 좌표로 변환
    /// </summary>
    /// <returns>실제 좌표</returns>
    public Vector3 GetUIPos(Vector2 realPos)
    {
        return new Vector2(
                    realPos.x / MaxPlanarDistance * RadarRadius,
                    realPos.y / MaxPlanarDistance * RadarRadius);
    }

    /// <summary>
    /// 현재 어뢰 상태 설정
    /// </summary>
    /// <param name="torpedoState">어뢰 상태</param>
    public void SetCurrentTorpedoState(TorpedoState torpedoState)
    {
        CurrentTorpedoState = torpedoState;
        _radarDisplay.UpdateCurrentTorpedoStateUI();
    }

    /// <summary>
    /// 문 열린 여부에 따른 현재 어뢰 상태 설정
    /// </summary>
    public void SetTorpedoStateByDoor(bool isOpen)
    {
        if (isOpen)
            SetCurrentTorpedoState(TorpedoState.Unloaded);
        else
            SetCurrentTorpedoState(TorpedoState.Normal);
    }

    // /// <summary>
    // /// 괴물 타이머 초기화
    // /// </summary>
    // public void ResetMonsterTimer()
    // {
    //     _monsterTimer = 0f;
    // }
    #endregion

    #region 기타
    /// <summary>
    /// 본부 잠수함 떠나는 함수
    /// </summary>
    public void LeaveSubmarine()
    {
        AudioManager.Instance.PlayGlobalOneShot(submarineLeaveSound); // 떠나는 소리 재생

        // 본부 잠수함 위치 갱신 중 아님으로 설정
        submarine2.IsCurrentActive = false;

        // 본부 잠수함 좌표 텍스트 초기화
        submarine2.posText.text = "";

        // 후퇴
        StartCoroutine(_radarDisplay.FadeInOut(false, submarine2, 3f)); // 본부 잠수함 페이드 아웃되면서 물러남
        Debug.Log("본부 잠수함이 떠났습니다.");

        telegraphKey.IsSubmarineLeft = true; // 이제 더 이상 통신 신호를 주고받을 수 없도록 플래그 차단
    }
    #endregion
}