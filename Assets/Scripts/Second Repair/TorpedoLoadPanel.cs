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
    [SerializeField] private GameObject torpedoLoadUI; // 어뢰 탑재 UI
    [SerializeField] private GameObject stretcherGroup;
    [SerializeField] private GameObject stretcher;
    [SerializeField] private GameObject joystickMoveBone; // 조이스틱 움직이는 뼈대
    [SerializeField] private GameObject switchCube; // 카메라 전환 스위치 큐브
    [SerializeField] private GameObject torpedo; // 어뢰
    [SerializeField] private TorpedoTube[] torpedoTubes; // 어뢰 발사관 배열 문 포함 X
    [SerializeField] private MeshRenderer monitorScreenMeshRenderer;
    [SerializeField] private Material cameraMataerial;
    [SerializeField] private Material blackMaterial;

    private Outline _torpedoOutline;
    private Outline _stretcherOutline;
    private Outline[] _torpedoTubeOutlines = new Outline[4];

    private bool _isCurrentActive = false; // 현재 패널의 활성화 여부
    private bool _isJoystickDragging = false; // 현재 조이스틱이 드래그 되고 있는지 여부
    private Vector2 _joystickMoveStartPos; // 조이스틱 이동 시작 위치
    private Vector2 _joystickMove; // 조이스틱 이동 벡터
    private float _joystickMaxAngle = 30f; // 조이스틱 최대로 꺾이는 각도 크기
    private bool _isPressingMoveUpButton = false; // 위로 이동 버튼이 현재 눌리고 있는지 여부
    private bool _isPressingMoveDownButton = false; // 아래로 이동 버튼이 현재 눌리고 있는지 여부

    private float _realMoveSpeed = 3f; // 실제 이동 속도
    private float _inputThreshold = 0.5f; // 0.3 이하의 입력은 무시

    private int _currentCameraMode = 0; // 0: 3D / 1: 2D

    private bool _canSetUp = false; // 어뢰 들 것에 싣기 가능 여부
    private bool _isSetUp = false; // 현재 어뢰가 들 것에 실어져 있는지 여부

    private bool _canLoad = false; // 어뢰 탑재 가능 여부
    private bool _isLoadCompleted = false; // 어뢰 탑재 성공 여부
    private int _currentTorpedoTubeIndex = -1; // 현재 앞에 위치한 어뢰 발사관 인덱스 (없음: -1)
    private float[] _loadPosXArray = { -1f, 1f, -1f, 1f };
    private float[] _loadPosYArray = { 1.3f, 1.3f, -0.2f, -0.2f };

    public static event Action<bool> OnSetUpButtonStateChanged; // READY 버튼 상태(활성화 여부) 변경 이벤트
    public static event Action<bool> OnLoadButtonStateChanged; // LOAD 버튼 상태(활성화 여부) 변경 이벤트

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

        torpedoLoadUI.SetActive(false);
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

        // 호버 검사
        CheckHover();

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

        // 어뢰 탑재 아직 못 한 경우
        if (!_isLoadCompleted)
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
            return "Power Restoration Required";

        return "Activate Torpedo Load Panel [E]";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 패널 활성화
    /// </summary>
    public void Interact()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        ActivatePuzzle(); // 패널 활성화
    }
    #endregion

    #region PuzzleController
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        Click.started += OnClickStarted; // 클릭 시작 사용
        Click.performed += OnClickPerformed; // 클릭 performed 사용
        Click.canceled += OnClickCanceled; // 클릭 끝 사용
        Point.performed += OnPoint; // 마우스 좌표 사용

        if (!_isLoadCompleted)
        {
            ShowOutlineSafe(_torpedoOutline);
            ShowOutlineSafe(_stretcherOutline);
        }

        // 카메라 켜기
        torpedoLoadPanelCamera.enabled = true;
        monitorScreenMeshRenderer.material = cameraMataerial;
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        Click.started -= OnClickStarted;
        Click.performed -= OnClickPerformed;
        Click.canceled -= OnClickCanceled;
        Point.performed -= OnPoint;

        statUI.SetActive(true);
        // torpedoLoadUI.SetActive(false);
        joystickMoveBone.transform.localRotation = Quaternion.identity; // 조이스틱 회전 초기화
        _torpedoOutline.enabled = false;
        _stretcherOutline.enabled = false;
        if (_currentTorpedoTubeIndex != -1)
            _torpedoTubeOutlines[_currentTorpedoTubeIndex].enabled = false;

        // 카메라 끄기
        torpedoLoadPanelCamera.enabled = false;
        monitorScreenMeshRenderer.material = blackMaterial;
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
                    ToggleDoorOpenState(torpedoTubes[currentHoverButton.torpedoTubeId]);
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

    private void OnPoint(InputAction.CallbackContext context)
    {
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
    public override void ActivatePuzzle()
    {
        SubmarineInGameManager.instance.SetPuzzleFocus(true);
        SubmarineInGameManager.instance.SetPlayerGeoActive(false);
        statUI.SetActive(false); // 스탯 UI 비활성화 추가

        Sequence seq = DOTween.Sequence();
        seq.Append(Camera.main.transform.DOMove(viewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 1.5f)
            .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            StartPuzzle(); // 퍼즐 시작
        });
    }

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

        while (!IsCloseTo(torpedo.transform.localPosition.x, -3f))
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
        ShowOutlineSafe(_torpedoOutline);
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
            if (IsCloseTo(stretcherGroup.transform.localPosition.x, _loadPosXArray[i]) && IsCloseTo(stretcher.transform.localPosition.y, _loadPosYArray[i]))
            {
                // 가능
                if (!_canLoad)
                {
                    _canLoad = true;
                    ShowOutlineSafe(_torpedoTubeOutlines[i]);
                    _currentTorpedoTubeIndex = i;
                    OnLoadButtonStateChanged?.Invoke(true);
                }
                return;
            }
        }

        // 불가능
        if (_canLoad)
        {
            _canLoad = false;
            _torpedoTubeOutlines[_currentTorpedoTubeIndex].enabled = false;
            OnLoadButtonStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// 어뢰 탑재
    /// </summary>
    private IEnumerator LoadCoroutine()
    {
        Vector3 torpedoPos;

        SetInputLock(true);

        // 들 것 위치 딱 맞는 위치로 보정
        stretcherGroup.transform.localPosition = new Vector3(_loadPosXArray[_currentTorpedoTubeIndex], 0f, 0f);
        stretcher.transform.localPosition = new Vector3(0f, _loadPosYArray[_currentTorpedoTubeIndex], 0f);

        // 현재 2D면 3D 카메라로 전환
        if (_currentCameraMode == 1)
            SwitchCamera();

        while (!IsCloseTo(torpedo.transform.localPosition.z, 6f))
        {
            torpedoPos = torpedo.transform.localPosition;
            torpedoPos.z = torpedoPos.z + 1f * _realMoveSpeed * Time.deltaTime;
            torpedo.transform.localPosition = torpedoPos;

            yield return null;
        }
        torpedoPos = torpedo.transform.localPosition;
        torpedoPos.z = 6f;
        torpedo.transform.localPosition = torpedoPos;

        _torpedoOutline.enabled = false;
        torpedo.transform.SetParent(torpedoTubes[_currentTorpedoTubeIndex].transform);
        ShowOutlineSafe(_torpedoOutline);

        SetInputLock(false);
        _isLoadCompleted = true;

        ExitPuzzle();
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
        else // 닫혀있으면 -> 자동 열기 불가! 무조건 수동 잠금 해제하고 수동 유압으로 열어야 함
        {
            Debug.Log("자동 열기 불가!!!");
            torpedoTube.SetDoorOpenState(true); // 임시 코드
        }
    }
    #endregion

    public void ShowOutlineSafe(Outline outline)
    {
        float targetWidth = outline.OutlineWidth; // 원래 두께 값
        outline.OutlineWidth = 0f; // 일단 안 보이게 두께 0으로 함
        outline.enabled = true; // 활성화
        Debug.Log("!!! " + outline.OutlineWidth);
        StartCoroutine(EnableOutlineRoutine(outline, targetWidth));
    }

    private IEnumerator EnableOutlineRoutine(Outline outline, float targetWidth)
    {
        yield return new WaitForEndOfFrame(); // 깊이 계산을 기다리기 위해 한 프레임 대기
        outline.OutlineWidth = targetWidth; // 이제 두께가 정상적으로 보이게함으로써 진짜 활성화
        Debug.Log("!!!!!!!!!!!! " + outline.OutlineWidth);
    }
}