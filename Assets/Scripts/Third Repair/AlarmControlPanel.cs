using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AlarmControlPanel : PuzzleController, IInteractable
{
    protected override bool IsHoverRequired => false;

    protected override bool IsMouseRequiredAtFirst => true;

    [SerializeField] private GameObject activatedScreenUI;
    [SerializeField] private Button[] areaImgButtons;
    [SerializeField] private GameObject alarmingUI; // 경보 발생 중 UI
    [SerializeField] private TextMeshProUGUI infoText; // 정보 텍스트
    [SerializeField] private TextMeshProUGUI floorText; // 층 텍스트
    [SerializeField] private Button upButton; // 위 버튼
    [SerializeField] private Button downButton; // 아래 버튼
    [SerializeField] private GameObject secondFloorMap; // 2층 맵

    private AlertArea _currentSelectedArea = AlertArea.None; // 현재 선택된 구역 (경보 발생 중인 구역 의미 X)
    private Image _currentSelectedAreaImg = null; // 현재 선택된 구역 이미지 (경보 발생 중인 구역 의미 X)
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();

        upButton.onClick.AddListener(() => SetFloor(2));
        downButton.onClick.AddListener(() => SetFloor(1));

        for (int i = 0; i < areaImgButtons.Length; i++)
        {
            // 클로저 때문에 인덱스 복사해서 사용
            int index = i;

            Button button = areaImgButtons[index];
            Image image = button.GetComponent<Image>();

            button.onClick.AddListener(() => SelectArea((AlertArea)(index + 1), image));
        }

        SetFloor(1);

        infoText.text = "경보 발생 구역 미선택 (선택: 클릭)";

        activatedScreenUI.SetActive(false); // 활성화 화면 비활성화
    }

    private void OnEnable()
    {
        SubmarineInGameManager.instance.OnAlertStarted += ActivateAlarmingUI;
        SubmarineInGameManager.instance.OnAlertEnded += DeactivateAlarmingUI;
    }

    private void OnDisable()
    {
        SubmarineInGameManager.instance.OnAlertStarted -= ActivateAlarmingUI;
        SubmarineInGameManager.instance.OnAlertEnded -= DeactivateAlarmingUI;
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

        return "Access Alarm Control Panel [E]";
    }

    public void Interact()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        ActivatePuzzle(); // 패널 활성화
    }
    #endregion

    #region PuzzleController
    public override void ActivatePuzzle()
    {
        SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화
        itemEquipController.UnequipItem(); // 아이템 장착 해제
        SubmarineInGameManager.instance.InteractorUI.SetActive(true); // 상호작용 UI 활성화

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        KeyE.performed += OnKeyEPerformed;

        activatedScreenUI.SetActive(true); // 활성화 화면 활성화
        _collider.enabled = false; // 콜라이더 비활성화 (UI로 쏘는 레이를 가리지 않도록)
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        KeyE.performed -= OnKeyEPerformed;

        SubmarineInGameManager.instance.SetActiveInGameUI(true); // 인게임 UI 활성화

        activatedScreenUI.SetActive(false); // 활성화 화면 비활성화
        _collider.enabled = true;

        UnselectArea(); // 선택 해제
    }
    #endregion

    #region 입력 이벤트 함수
    private void OnKeyEPerformed(InputAction.CallbackContext context)
    {
        // 기계실이 아닌 다른 곳에서 경보가 울리고 있을 때 -> 경보 발생 불가능
        if (SubmarineInGameManager.instance.IsAlerting && SubmarineInGameManager.instance.CurrentAlertArea != AlertArea.MachinerySpace)
            return;

        if (_currentSelectedArea != AlertArea.None)
        {
            // 해당 구역 경보 발생
            if (_currentSelectedArea == AlertArea.ControlRoom)
                SubmarineInGameManager.instance.AlertOn(_currentSelectedArea, 3); // 조종실 선택되면 마지막 장비인 통신 장비 파괴 경보
            else
                SubmarineInGameManager.instance.AlertOn(_currentSelectedArea, 0); // 나머지 구역은 첫번째 장비(사물) 파괴 경보
        }
    }
    #endregion

    #region 퍼즐용 함수
    private void ActivateAlarmingUI()
    {
        // 기계실 문 경보 발생 경우 -> 패널에서는 경보 발생 중으로 인지하지 못함
        if (SubmarineInGameManager.instance.CurrentAlertArea == AlertArea.MachinerySpace)
            return;

        alarmingUI.SetActive(true);

        UnselectArea(); // 선택 해제
    }

    private void DeactivateAlarmingUI()
    {
        alarmingUI.SetActive(false);
    }

    /// <summary>
    /// 층 변경
    /// </summary>
    /// <param name="floor"></param>
    private void SetFloor(int floor)
    {
        if (floor == 1) // 1층으로
        {
            if (secondFloorMap.activeSelf)
            {
                secondFloorMap.SetActive(false); // 2층 끄기
                floorText.text = "1F";
                upButton.interactable = true;
                downButton.interactable = false;
            }
        }
        else // 2층으로
        {
            if (!secondFloorMap.activeSelf)
            {
                secondFloorMap.SetActive(true); // 2층 켜기
                floorText.text = "2F";
                upButton.interactable = false;
                downButton.interactable = true;
            }
        }
    }

    /// <summary>
    /// 구역 선택 해제
    /// </summary>
    private void UnselectArea()
    {
        if (_currentSelectedAreaImg != null)
        {
            // 선택 해제되면 색 약하게 변경
            Color c = _currentSelectedAreaImg.color;
            c.a = 36f / 255f;
            _currentSelectedAreaImg.color = c;

            _currentSelectedArea = AlertArea.None;
            _currentSelectedAreaImg = null;

            infoText.text = "경보 발생 구역 미선택 (선택: 클릭)";
        }
    }

    /// <summary>
    /// 구역 선택
    /// </summary>
    private void SelectArea(AlertArea alertArea, Image areaImg)
    {
        // 기계실이 아닌 다른 곳에서 경보가 울리고 있을 때 -> 클릭 불가능
        if (SubmarineInGameManager.instance.IsAlerting && SubmarineInGameManager.instance.CurrentAlertArea != AlertArea.MachinerySpace)
            return;

        if (_currentSelectedArea == alertArea) // 이미 선택된 것 선택 시 -> 선택 해제
        {
            UnselectArea();
        }
        else // 선택
        {
            Color c;

            // 이전 선택 구역 색 약하게 변경
            if (_currentSelectedAreaImg != null)
            {
                c = _currentSelectedAreaImg.color;
                c.a = 36f / 255f;
                _currentSelectedAreaImg.color = c;
            }

            _currentSelectedArea = alertArea;
            _currentSelectedAreaImg = areaImg;

            // 선택되면 색 진하게 변경
            c = areaImg.color;
            c.a = 136f / 255f;
            areaImg.color = c;

            infoText.text = $"[선택됨] 선택 구역: {_currentSelectedArea} - 선택 구역에서 경보 발생 [E]";
        }
    }
    #endregion
}
