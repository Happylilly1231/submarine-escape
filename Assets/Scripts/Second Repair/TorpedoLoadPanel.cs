using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TorpedoLoadPanel : PuzzleController, IInteractable
{
    [SerializeField] private Camera torpedoLoadPanelCamera; // 어뢰 탑재 패널의 카메라
    [SerializeField] private GameObject statUI; // 스탯 UI
    [SerializeField] private GameObject stretcherGroup; // 들 것 그룹(들것 & 사슬 + 위의 4개의 큐브)
    [SerializeField] private GameObject stretcher; // 들 것(들 것 & 사슬)
    [SerializeField] private GameObject joystickMoveBone; // 조이스틱 움직이는 뼈대
    [SerializeField] private GameObject switchCube; // 카메라 전환 스위치 큐브
    [SerializeField] private GameObject torpedo; // 어뢰
    [SerializeField] private TorpedoTube[] torpedoTubes; // 어뢰 발사관 배열
    [SerializeField] private MeshRenderer monitorScreenMeshRenderer; // 모니터 화면 렌더러
    [SerializeField] private Material cameraMataerial; // 카메라 머티리얼(카메라 켜졌을 때)
    [SerializeField] private Material blackMaterial; // 검정 머티리얼(카메라 꺼졌을 때)
    [SerializeField] private TextMeshProUGUI infoText; // 정보 텍스트
    [SerializeField] private TorpedoTubeScrew torpedoTubeScrew; // 2번 어뢰 발사관의 나사
    [SerializeField] private Transform ejectViewPoint; // 나사 튀어나올 때 볼 위치

    private TorpedoAutoLoadSwitch _torpedoAutoLoadSwitch;
    //private InventoryManager _inventoryManager;

    protected override bool IsHoverRequired => true;
    protected override bool IsMouseRequiredAtFirst => true;

    // 아웃라인
    private Outline _torpedoOutline;
    private Outline _stretcherOutline;
    private Outline[] _torpedoTubeOutlines = new Outline[4];

    // 들 것 이동
    private bool _isJoystickDragging = false; // 현재 조이스틱이 드래그 되고 있는지 여부
    private Vector2 _joystickMoveStartPos; // 조이스틱 이동 시작 위치
    private Vector2 _joystickMove; // 조이스틱 이동 벡터
    private float _joystickMaxAngle = 30f; // 조이스틱 최대로 꺾이는 각도 크기
    private bool _isPressingMoveUpButton = false; // 위로 이동 버튼이 현재 눌리고 있는지 여부
    private bool _isPressingMoveDownButton = false; // 아래로 이동 버튼이 현재 눌리고 있는지 여부
    private float _realMoveSpeed = 3f; // 실제 이동 속도
    private float _inputThreshold = 0.5f; // 0.3 이하의 입력은 무시

    // 카메라 전환
    private int _currentCameraMode = 0; // 0: 3D / 1: 2D

    // 싣기
    private bool _canSetUp = false; // 어뢰 들 것에 싣기 가능 여부
    private bool _isSetUp = false; // 현재 어뢰가 들 것에 실어져 있는지 여부

    // 탑재
    private bool _canLoad = false; // 어뢰 탑재 가능 여부
    private bool _isLoadCompleted = false; // 어뢰 탑재 성공 여부
    private bool _isLoading = false; // 현재 탑재 중인지 여부
    private int _currentTorpedoTubeIndex = -1; // 현재 앞에 위치한 어뢰 발사관 인덱스 (없음: -1)
    private float[] _loadPosXArray = { -1f, 1f, -1f, 1f }; // 어뢰 발사관 앞 위치 x 좌표 배열
    private float[] _loadPosYArray = { 1.3f, 1.3f, -0.2f, -0.2f }; // 어뢰 발사관 앞 위치 y 좌표 배열

    // 이벤트
    public static event Action<bool> OnSetUpButtonStateChanged; // READY 버튼 상태(활성화 여부) 변경 이벤트
    public static event Action<bool> OnLoadButtonStateChanged; // LOAD 버튼 상태(활성화 여부) 변경 이벤트
    public static event Action<TorpedoState> OnLoaded; // 탑재 이벤트 (탑재된 어뢰 상태)

    private void Awake()
    {
        _torpedoAutoLoadSwitch = FindAnyObjectByType<TorpedoAutoLoadSwitch>();
        //_inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    public override void Start()
    {
        base.Start();

        // 필요한 아웃라인 컴포넌트 미리 가져오기 & 초기 비활성화
        _torpedoOutline = torpedo.GetComponent<Outline>();
        _stretcherOutline = stretcher.GetComponent<Outline>();
        _torpedoOutline.enabled = false;
        _stretcherOutline.enabled = false;
        for (int i = 0; i < torpedoTubes.Length; i++)
        {
            _torpedoTubeOutlines[i] = torpedoTubes[i].gameObject.GetComponent<Outline>();
            _torpedoTubeOutlines[i].enabled = false;
        }

        infoText.gameObject.SetActive(false);
        torpedoLoadPanelCamera.enabled = false; // 카메라 끄기

        // 들 것, 들 것 그룹 초기 위치로 이동
        stretcherGroup.transform.localPosition = new Vector3(3f, 0f, 6f);
        stretcher.transform.localPosition = new Vector3(0f, 0.5f, 0f);
    }

    private void Update()
    {
        // 퍼즐이 시작되지 않았으면 -> 아무것도 안 함
        if (!IsPuzzleStarted)
            return;

        // 패널이 활성화되어 있을 때

        // 탑재 중일 때는 아무것도 처리하지 않음
        if (_isLoading)
            return;

        // 조이스틱 드래그 중 -> 조이스틱, 들 것 움직이기
        if (_isJoystickDragging)
        {
            MoveJoystick();
            HandleRailMovement();
        }

        // 위 아래 이동 버튼 누른 경우 -> 위 아래 이동
        if (_isPressingMoveUpButton)
            MoveUpDown(1f);
        else if (_isPressingMoveDownButton)
            MoveUpDown(-1f);

        // 어뢰 탑재 아직 못 했고, 어뢰를 탑재하고 있는 중도 아닐 경우
        if (!_isLoadCompleted && !_isLoading)
        {
            // 어뢰가 들 것에 실어져 있지 않으면 -> 어뢰 들 것에 싣기 가능 여부 검사
            if (!_isSetUp)
                CheckCanSetUp();

            // 어뢰가 들 것에 실어져 있으면 -> 어뢰 탑재 가능 여부 검사
            if (_isSetUp)
                CheckCanLoad();
        }
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 전력 필요
            return LocalizationHelper.GetLocalizedInteractText("Interact/PowerRestorationRequired");

        if (_torpedoAutoLoadSwitch.IsSwitchOn) // 아직 어뢰 자동 탑재 스위치가 켜져 있는 경우 -> 상호작용 불가
            return LocalizationHelper.GetLocalizedInteractText("Interact/AutoMode");
        else
            return LocalizationHelper.GetLocalizedInteractText("Interact/ActivateTorpedoLoadPanel", "E");
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 패널 활성화
    /// </summary>
    public void Interact()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        if (_torpedoAutoLoadSwitch.IsSwitchOn) // 아직 어뢰 자동 탑재 스위치가 켜져 있는 경우 -> 상호작용 불가
            return;

        ActivatePuzzle(); // 패널 활성화
    }
    #endregion

    #region PuzzleController
    public override void ActivatePuzzle()
    {
        statUI.SetActive(false); // 스탯 UI 비활성화 추가
        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        Click.started += OnClickStarted; // 클릭 시작 사용
        Click.performed += OnClickPerformed; // 클릭 performed 사용
        Click.canceled += OnClickCanceled; // 클릭 끝 사용

        inventoryManager.CloseInventory(); // 인벤토리 숨기기

        if (!_isLoadCompleted)
        {
            _torpedoOutline.enabled = true;
            _stretcherOutline.enabled = true;
        }

        // 카메라 켜기
        torpedoLoadPanelCamera.enabled = true;
        monitorScreenMeshRenderer.material = cameraMataerial;

        infoText.gameObject.SetActive(true);

        _currentTorpedoTubeIndex = -1;
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        Click.started -= OnClickStarted;
        Click.performed -= OnClickPerformed;
        Click.canceled -= OnClickCanceled;

        inventoryManager.OpenInventory(); // 인벤토리 숨기기

        statUI.SetActive(true);
        joystickMoveBone.transform.localRotation = Quaternion.identity; // 조이스틱 회전 초기화
        _torpedoOutline.enabled = false;
        _stretcherOutline.enabled = false;
        if (_currentTorpedoTubeIndex != -1)
            _torpedoTubeOutlines[_currentTorpedoTubeIndex].enabled = false;

        // 카메라 끄기
        torpedoLoadPanelCamera.enabled = false;
        monitorScreenMeshRenderer.material = blackMaterial;

        infoText.gameObject.SetActive(false);
    }
    #endregion

    #region 입력 이벤트 함수
    private void OnClickStarted(InputAction.CallbackContext context)
    {
        TorpedoLoadPanelButton currentHoverButton = CurrentHover?.GetComponent<TorpedoLoadPanelButton>();
        if (currentHoverButton != null)
        {
            switch (currentHoverButton.ButtonType)
            {
                case TorpedoLoadPanelButtonType.Joystick:
                    _isJoystickDragging = true;
                    _joystickMoveStartPos = Mouse.current.position.ReadValue();
                    break;
                case TorpedoLoadPanelButtonType.MoveUp:
                    _isPressingMoveUpButton = true;
                    break;
                case TorpedoLoadPanelButtonType.MoveDown:
                    _isPressingMoveDownButton = true;
                    break;
            }
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        TorpedoLoadPanelButton currentHoverButton = CurrentHover?.GetComponent<TorpedoLoadPanelButton>();
        if (currentHoverButton != null && currentHoverButton.isCurrentActive)
        {
            switch (currentHoverButton.ButtonType)
            {
                case TorpedoLoadPanelButtonType.Power:
                    ExitPuzzle();
                    break;
                case TorpedoLoadPanelButtonType.SetUp:
                    if (_canSetUp)
                        StartCoroutine(SetUpCoroutine());
                    break;
                case TorpedoLoadPanelButtonType.Load:
                    if (_canLoad)
                        StartCoroutine(LoadCoroutine());
                    break;
                case TorpedoLoadPanelButtonType.CameraSwitch:
                    SwitchCamera();
                    break;
                case TorpedoLoadPanelButtonType.TorpedoTubeDoorButton:
                    ToggleDoorOpenState(currentHoverButton.LinkedTorpedoTube);
                    break;
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (_isJoystickDragging)
        {
            _isJoystickDragging = false;
            _joystickMove = Vector2.zero;
        }

        if (_isPressingMoveUpButton)
        {
            _isPressingMoveUpButton = false;
        }
        else if (_isPressingMoveDownButton)
        {
            _isPressingMoveDownButton = false;
        }
    }

    public override void OnPoint(InputAction.CallbackContext context)
    {
        base.OnPoint(context);

        // 조이스틱
        if (_isJoystickDragging)
        {
            Vector2 currentPos = context.ReadValue<Vector2>();

            // 드래그 거리 계산
            Vector2 delta = currentPos - _joystickMoveStartPos;

            // 조이스틱 느낌을 위해 10픽셀로 정규화
            float dragRadius = 10f;
            _joystickMove = Vector2.ClampMagnitude(delta, dragRadius) / dragRadius;
            return;
        }

        // 위로 이동 계속 누르고 있는지 검사
        if (_isPressingMoveUpButton)
        {
            TorpedoLoadPanelButton currentHoverButton = CurrentHover?.GetComponent<TorpedoLoadPanelButton>();
            if (currentHoverButton == null || currentHoverButton.ButtonType != TorpedoLoadPanelButtonType.MoveUp)
            {
                _isPressingMoveUpButton = false;
            }
        }

        // 아래로 이동 계속 누르고 있는지 검사
        if (_isPressingMoveDownButton)
        {
            TorpedoLoadPanelButton currentHoverButton = CurrentHover?.GetComponent<TorpedoLoadPanelButton>();
            if (currentHoverButton == null || currentHoverButton.ButtonType != TorpedoLoadPanelButtonType.MoveDown)
            {
                _isPressingMoveDownButton = false;
            }
        }
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 값과 목표 값의 차이가 임계값 이하인지 검사하는 함수
    /// </summary>
    /// <param name="value">값</param>
    /// <param name="targetValue">목표 값</param>
    /// <param name="threshold">임계값(기본: 0.1f)</param>
    /// <returns></returns>
    private bool IsCloseTo(float value, float targetValue, float threshold = 0.1f)
    {
        return Mathf.Abs(value - targetValue) < threshold;
    }
    #endregion

    #region 들 것 이동
    /// <summary>
    /// 조이스틱 움직이기
    /// </summary>
    private void MoveJoystick()
    {
        float rotZ = _joystickMove.y * _joystickMaxAngle;
        float rotX = _joystickMove.x * _joystickMaxAngle;
        joystickMoveBone.transform.localRotation = Quaternion.Euler(rotX, 0, rotZ);
    }

    /// <summary>
    /// 레일에 따른 제한적인 들 것 이동
    /// </summary>
    private void HandleRailMovement()
    {
        float x = stretcherGroup.transform.localPosition.x;
        float z = stretcherGroup.transform.localPosition.z;

        bool hasXInput = Mathf.Abs(_joystickMove.x) > _inputThreshold;
        bool hasYInput = Mathf.Abs(_joystickMove.y) > _inputThreshold;

        // 현재 구간 파악
        bool isOnLeftRail = IsCloseTo(x, -3f) && z > 0.01f;
        bool isOnRightRail = IsCloseTo(x, 3f) && z > 0.01f;
        bool isOnCenterRail = z <= 0.01f;

        float nextX = x;
        float nextZ = z;

        if (isOnCenterRail)
        {
            nextZ = 0f;
            bool atLeftCorner = IsCloseTo(x, -3f, 0.01f);
            bool atRightCorner = IsCloseTo(x, 3f, 0.01f);

            if ((atLeftCorner || atRightCorner) && _joystickMove.y > _inputThreshold)
            {
                nextZ = Mathf.Clamp(z + _joystickMove.y * _realMoveSpeed * Time.deltaTime, 0f, 6f);
            }
            else if (hasXInput)
            {
                nextX = Mathf.Clamp(x + _joystickMove.x * _realMoveSpeed * Time.deltaTime, -3f, 3f);
            }
        }
        else if (isOnLeftRail || isOnRightRail)
        {
            nextX = isOnLeftRail ? -3f : 3f;

            if (hasYInput)
            {
                nextZ = Mathf.Clamp(z + _joystickMove.y * _realMoveSpeed * Time.deltaTime, 0f, 6f);
            }

            if (nextZ <= 0.01f)
            {
                nextZ = 0f;
                if (hasXInput)
                {
                    nextX = Mathf.Clamp(x + _joystickMove.x * _realMoveSpeed * Time.deltaTime, -3f, 3f);
                }
            }
        }

        stretcherGroup.transform.localPosition = new Vector3(nextX, 0, nextZ);
    }

    /// <summary>
    /// 위아래 이동
    /// </summary>
    /// <param name="inputY">입력받은 y값(위: 1f / 아래 -1f)</param>
    private void MoveUpDown(float inputY)
    {
        float nextY = Mathf.Clamp(stretcher.transform.localPosition.y + inputY * _realMoveSpeed * Time.deltaTime, -0.2f, 2.25f);
        stretcher.transform.localPosition = new Vector3(0f, nextY, 0f);
    }
    #endregion

    #region 싣기
    /// <summary>
    /// 어뢰 들 것에 싣기 가능 여부 검사
    /// </summary>
    private void CheckCanSetUp()
    {
        if (IsCloseTo(stretcherGroup.transform.localPosition.x, -3f) && IsCloseTo(stretcher.transform.localPosition.y, 0.5f) && IsCloseTo(stretcherGroup.transform.localPosition.z, 6f))
        {
            // 가능
            if (!_canSetUp)
            {
                _canSetUp = true;
                _stretcherOutline.OutlineColor = Color.green;
                _torpedoOutline.OutlineColor = Color.green;
                OnSetUpButtonStateChanged?.Invoke(true);
            }
        }
        else
        {
            // 불가능
            if (_canSetUp)
            {
                _canSetUp = false;
                _stretcherOutline.OutlineColor = Color.white;
                _torpedoOutline.OutlineColor = Color.white;
                OnSetUpButtonStateChanged?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// 어뢰 들 것에 싣기
    /// </summary>
    private IEnumerator SetUpCoroutine()
    {
        Vector3 torpedoPos;

        SetInputLock(true);

        // 들 것 위치 딱 맞는 위치로 보정
        stretcherGroup.transform.localPosition = new Vector3(-3f, 0f, 6f);
        stretcher.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        while (!IsCloseTo(torpedo.transform.localPosition.x, -3f, 0.2f))
        {
            torpedoPos = torpedo.transform.localPosition;
            torpedoPos.x = torpedoPos.x + 1f * _realMoveSpeed * Time.deltaTime;
            torpedo.transform.localPosition = torpedoPos;

            yield return null;
        }
        torpedoPos = torpedo.transform.localPosition;
        torpedoPos.x = -3f;
        torpedo.transform.localPosition = torpedoPos;

        _torpedoOutline.enabled = false;
        torpedo.transform.SetParent(stretcher.transform);
        _torpedoOutline.enabled = true;
        _stretcherOutline.OutlineColor = Color.white;
        _torpedoOutline.OutlineColor = Color.white;
        OnSetUpButtonStateChanged?.Invoke(false);

        SetInputLock(false);
        _isSetUp = true;
    }
    #endregion

    #region 카메라 전환
    /// <summary>
    /// 카메라 2D, 3D 전환
    /// </summary>
    private void SwitchCamera()
    {
        Vector3 switchPos;
        Vector3 cameraPos;
        if (_currentCameraMode == 0) // 2D로 전환
        {
            _currentCameraMode = 1;
            switchPos = switchCube.transform.localPosition;
            switchPos.z = 0.25f;
            switchCube.transform.localPosition = switchPos;

            torpedoLoadPanelCamera.orthographic = true;
            cameraPos = torpedoLoadPanelCamera.transform.position;
            cameraPos.y = 8f;
            torpedoLoadPanelCamera.transform.position = cameraPos;
        }
        else // 3D로 전환
        {
            _currentCameraMode = 0;
            switchPos = switchCube.transform.localPosition;
            switchPos.z = -0.25f;
            switchCube.transform.localPosition = switchPos;

            torpedoLoadPanelCamera.orthographic = false;
            cameraPos = torpedoLoadPanelCamera.transform.position;
            cameraPos.y = 9f;
            torpedoLoadPanelCamera.transform.position = cameraPos;
        }
    }
    #endregion

    #region 탑재
    /// <summary>
    /// 어뢰 탑재 가능 여부 검사
    /// </summary>
    private void CheckCanLoad()
    {
        for (int i = 0; i < 4; i++)
        {
            // 앞에 어뢰 발사관이 있으면 -> 문이 열려있을 때는 가능 / 문이 닫혀있을 때는 불가능
            if (IsCloseTo(stretcherGroup.transform.localPosition.x, _loadPosXArray[i]) && IsCloseTo(stretcher.transform.localPosition.y, _loadPosYArray[i]))
            {
                if (torpedoTubes[i].IsOpened) // 문이 열려있는 경우 -> 가능
                {
                    // 불가능 상태였다면 -> 가능으로 변경
                    if (!_canLoad)
                    {
                        _canLoad = true;
                        _torpedoTubeOutlines[i].OutlineColor = Color.green;
                        _torpedoTubeOutlines[i].enabled = true;
                        _currentTorpedoTubeIndex = i;
                        OnLoadButtonStateChanged?.Invoke(true);
                    }
                }
                else // 문이 닫혀있는 경우 -> 불가능
                {
                    // 현재 앞에 있는 어뢰 발사관이 없는 상태였다면 -> 불가능으로 변경, 대신 현재 앞에 있는 어뢰 발사관은 현재 발사관으로 설정
                    if (_currentTorpedoTubeIndex == -1)
                    {
                        _canLoad = false;
                        _torpedoTubeOutlines[i].OutlineColor = Color.red;
                        _torpedoTubeOutlines[i].enabled = true;
                        _currentTorpedoTubeIndex = i;
                        OnLoadButtonStateChanged?.Invoke(false);
                    }
                }
                return;
            }
        }

        // 앞에 어떤 발사관도 없는 경우인데 이전에 앞에 있던 발사관이 있었다면 -> 불가능으로 변경 후 현재 앞에 있는 어뢰 발사관이 없음으로 설정
        if (_currentTorpedoTubeIndex != -1)
        {
            _canLoad = false;
            _torpedoTubeOutlines[_currentTorpedoTubeIndex].enabled = false;
            _currentTorpedoTubeIndex = -1;
            OnLoadButtonStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// 어뢰 탑재
    /// </summary>
    private IEnumerator LoadCoroutine()
    {
        Vector3 torpedoPos;

        _isLoading = true;

        SetInputLock(true);

        TorpedoTube.OnClosed += ExitAfterSuccess;

        // 들 것 위치 딱 맞는 위치로 보정
        stretcherGroup.transform.localPosition = new Vector3(_loadPosXArray[_currentTorpedoTubeIndex], 0f, 0f);
        stretcher.transform.localPosition = new Vector3(0f, _loadPosYArray[_currentTorpedoTubeIndex], 0f);

        // 현재 2D면 3D 카메라로 전환
        if (_currentCameraMode == 1)
            SwitchCamera();

        bool isSoundPlayed = false;
        while (!IsCloseTo(torpedo.transform.localPosition.z, 6f))
        {
            torpedoPos = torpedo.transform.localPosition;
            torpedoPos.z = torpedoPos.z + 1f * _realMoveSpeed * Time.deltaTime;
            torpedo.transform.localPosition = torpedoPos;

            // 3번 발사관 -> 절반 도달했을 때 끼이익 소리 재생 후 얼마 더 간 뒤 폭발
            if (torpedoTubes[_currentTorpedoTubeIndex].TorpedoTubeNum == 3)
            {
                if (torpedo.transform.localPosition.z > 3f && !isSoundPlayed) // 절반 도달했을 때 -> 끼이익 소리 재생(1번만)
                {
                    // 끼이익 소리 재생
                    Debug.Log("끼이익 소리");
                    isSoundPlayed = true;
                }
                else if (torpedo.transform.localPosition.z > 4f) // 1f 더 갔을 때 -> 폭발
                {
                    // 폭발 사망 엔딩
                    GameManager.instance.GameOver(EEndingType.KeypadExplosion); // 일단 키패드 폭발 엔딩으로 함
                    yield break;
                }
            }

            yield return null;
        }
        torpedoPos = torpedo.transform.localPosition;
        torpedoPos.z = 6f;
        torpedo.transform.localPosition = torpedoPos;

        _torpedoOutline.enabled = false;
        torpedo.transform.SetParent(torpedoTubes[_currentTorpedoTubeIndex].transform);
        _torpedoOutline.enabled = true;

        torpedoTubes[_currentTorpedoTubeIndex].SetDoorOpenState(false);
    }

    /// <summary>
    /// 성공 후 종료
    /// </summary>
    private void ExitAfterSuccess()
    {
        TorpedoTube.OnClosed -= ExitAfterSuccess;

        switch (torpedoTubes[_currentTorpedoTubeIndex].TorpedoTubeNum)
        {
            case 2: // 2번 발사관
                if (torpedoTubeScrew.IsTightened) // 나사 조인 경우 -> 정상
                {
                    ObjectiveManager.Instance.CompleteObjective("LoadTorpedoTube");

                    SetInputLock(false);
                    _isLoadCompleted = true;
                    _isLoading = false;
                    OnLoaded?.Invoke(TorpedoState.Normal); // 정상 탑재되었음 알리기
                    ExitPuzzle();
                }
                else // 나사 조이지 않은 경우 -> 비정상
                {
                    ViewScrewEjectAndComplete();
                }
                break;
            case 4: // 4번 발사관 -> 비정상
                SetInputLock(false);
                _isLoadCompleted = true;
                _isLoading = false;
                OnLoaded?.Invoke(TorpedoState.Abnormal); // 비정상 탑재되었음 알리기
                ExitPuzzle();
                break;
            default:
                Debug.Log("기획 상의 경우가 아님");
                break;
        }
    }

    /// <summary>
    /// 나사 튀어나가는 것 보고 탑재 완료
    /// </summary>
    private void ViewScrewEjectAndComplete()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(Camera.main.transform.DOMove(ejectViewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.Join(Camera.main.transform.DORotateQuaternion(ejectViewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.AppendCallback(() =>
        {
            torpedoTubeScrew.Eject();
        });

        seq.AppendInterval(1.5f); // 나사 튀어나갈 때까지 대기

        seq.OnComplete(() =>
        {
            SetInputLock(false);
            _isLoadCompleted = true;
            _isLoading = false;

            OnLoaded?.Invoke(TorpedoState.Abnormal); // 비정상 탑재되었음 알리기

            ExitPuzzle();

            torpedoTubes[_currentTorpedoTubeIndex].SetDoorOpenState(true);
        });
    }

    /// <summary>
    /// 어뢰 사용해서 비활성화
    /// </summary>
    public void UseTorpedo()
    {
        torpedo.SetActive(false);
    }
    #endregion

    #region 문 열기/닫기
    // 문 열린 상태 전환
    private void ToggleDoorOpenState(TorpedoTube torpedoTube)
    {
        if (torpedoTube.IsOpened) // 열려있으면 -> 닫기
        {
            torpedoTube.SetDoorOpenState(false);
        }
        else // 닫혀있으면 -> 버튼으로 열기 불가
        {
            // 버튼으로 열기 불가
            Debug.Log("버튼으로 열기 불가");
            // 안된다는 삐빅 소리 재생 추가 필요
        }
    }
    #endregion
}