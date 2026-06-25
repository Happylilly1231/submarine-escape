using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ManualHydraulicSystem : PuzzleController, IInteractable
{
    [SerializeField] private Slider gaugeBarSlider;
    [SerializeField] private Transform leverTransform;
    [SerializeField] private HydraulicSystemDoorButton[] doorButtons;
    [SerializeField] private TextMeshProUGUI topInfoText;

    private TorpedoAutoLoadSwitch _torpedoAutoLoadSwitch;

    protected override bool IsHoverRequired => true;
    protected override bool IsMouseRequiredAtFirst => true;

    private float _currentProgress = 0f; // 0 ~ 100
    private bool _isLeverMoving = false;
    private float _increaseAmount = 10f;
    private float _decreaseSpeed = 5f;
    private int _currentSelectDoorIndex = -1; // -1은 아무것도 선택되지 않은 상태

    private void Awake()
    {
        gaugeBarSlider.gameObject.SetActive(false);

        _torpedoAutoLoadSwitch = FindAnyObjectByType<TorpedoAutoLoadSwitch>();
    }

    private void Update()
    {
        // 정지 중일 때 -> 아무것도 안 함
        if (GameManager.instance.IsPausing)
            return;

        // 퍼즐이 시작되지 않았으면 -> 아무것도 안 함
        if (!IsPuzzleStarted)
            return;

        if (_currentProgress > 0)
        {
            if (_currentProgress == 100f) // 달성 -> 해당 문 열고 퍼즐 종료
            {
                doorButtons[_currentSelectDoorIndex].ViewDoorOpen();
            }
            else
            {
                // 게이지 진행도 서서히 감소
                _currentProgress -= _decreaseSpeed * Time.deltaTime;
                _currentProgress = Mathf.Clamp(_currentProgress, 0, 100);

                // 게이지 바 슬라이더 UI 갱신
                gaugeBarSlider.value = _currentProgress / 100f;
            }
        }
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 전력 필요
            return "Power Restoration Required";

        if (_torpedoAutoLoadSwitch.IsSwitchOn) // 아직 어뢰 자동 탑재 스위치가 켜져 있는 경우 -> 상호작용 불가
            return "Auto Mode";
        else
            return "Open Torpedo Tube Door [E]";
    }

    public void Interact()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        if (_torpedoAutoLoadSwitch.IsSwitchOn) // 아직 어뢰 자동 탑재 스위치가 켜져 있는 경우 -> 상호작용 불가
            return;

        ActivatePuzzle();
    }
    #endregion

    #region PuzzleController
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        Click.performed += OnClickPerformed; // 클릭 performed 사용
        Space.performed += OnSpace; // 스페이스 사용
        TorpedoTube.OnOpened += ExitAfterSuccess;

        _currentProgress = 0f;
        gaugeBarSlider.gameObject.SetActive(true);
        gaugeBarSlider.value = 0f;
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        Click.performed -= OnClickPerformed;
        Space.performed -= OnSpace;
        TorpedoTube.OnOpened -= ExitAfterSuccess;

        gaugeBarSlider.gameObject.SetActive(false);
        _currentSelectDoorIndex = -1;
        topInfoText.text = "";
    }
    #endregion

    #region 입력 이벤트 함수
    private void OnSpace(InputAction.CallbackContext context)
    {
        // 현재 선택된 문이 있음 & 레버 움직이고 있지 않을 때만 -> 펌프질
        if (_currentSelectDoorIndex != -1 && !_isLeverMoving && _currentProgress < 100f)
            PumpAction();
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        HydraulicSystemDoorButton doorButton = CurrentHover?.GetComponent<HydraulicSystemDoorButton>();
        if (doorButton != null && doorButton.IsActive)
        {
            if (_currentSelectDoorIndex != doorButton.DoorIndex)
            {
                if (_currentSelectDoorIndex != -1)
                {
                    doorButtons[_currentSelectDoorIndex].UnselectButton();
                }
                _currentSelectDoorIndex = doorButton.DoorIndex;
                doorButtons[_currentSelectDoorIndex].SelectButton();
            }
        }
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 펌프질
    /// </summary>
    private void PumpAction()
    {
        _isLeverMoving = true;

        _currentProgress += _increaseAmount;

        // 레버 왕복 운동
        leverTransform.DOLocalMove(new Vector3(0f, -12f, 12f), 0.2f / 2)
        .SetLoops(2, LoopType.Yoyo) // 왕복(아래로 내려갔다 다시 올라옴)
        .OnComplete(() =>
        {
            _isLeverMoving = false;
        });
    }

    /// <summary>
    /// 성공 후 종료
    /// </summary>
    private void ExitAfterSuccess()
    {
        ExitPuzzle();
    }
    #endregion
}
