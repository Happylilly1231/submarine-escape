using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class PowerController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    [SerializeField] private ElectricalBox electricalBox;
    [SerializeField] private GameObject powerPuzzle_panel;
    [SerializeField] private GameObject flashlight;
    [SerializeField] private PowerGridManager powerGridManager;
    [SerializeField] private GameObject statUI;

    [SerializeField] private Button hintButton; // 힌트 버튼
    [SerializeField] private TextMeshProUGUI hintText; // 힌트 텍스트

    // Localization Table Key
    private const string HINT_KEY = "Puzzle/PowerGrid/Hint";

    public bool IsComplete = false;
    private bool _isExiting = false; // 문 여는 모션 도중 Esc키 누르면 panel 안 보이게 하기 위해서

    public override void Start()
    {
        base.Start();

        hintText.gameObject.SetActive(false);
        hintButton.onClick.AddListener(OnHintClick);
    }

    /// <summary>
    /// 힌트 버튼 클릭 함수
    /// </summary>
    public void OnHintClick()
    {
        hintText.gameObject.SetActive(true);
    }

    #region 퍼즐 시작/종료
    public override void ActivatePuzzle()
    {
        statUI.SetActive(false);
        if (LightingManager.instance != null && !LightingManager.instance.IsPowerOn)
            flashlight.SetActive(true);
        inventoryManager.CloseInventory();
        itemEquipController.UnequipItem();

        hintText.text = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", HINT_KEY);

        base.ActivatePuzzle();
    }
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        if (!IsComplete)
        {
            // 입력 이벤트 바인딩
            Click.started += powerGridManager.OnPointerDown;
            Click.canceled += powerGridManager.OnPointerUp;
        }
        electricalBox.OpenDoor(() =>
        {
            if (!_isExiting && IsPuzzleStarted) powerPuzzle_panel.SetActive(true);
        }
        );
    }
    public override void ExitPuzzle()
    {
        if (_isExiting) return;
        _isExiting = true;

        if (!IsComplete)
        {
            // 입력 이벤트 해제
            Click.started -= powerGridManager.OnPointerDown;
            Click.canceled -= powerGridManager.OnPointerUp;
        }

        hintText.gameObject.SetActive(false); // 힌트 비활성화

        powerPuzzle_panel.SetActive(false);

        electricalBox.CloseDoor(() =>
        {
            base.ExitPuzzle();

            statUI.SetActive(true);
            inventoryManager.OpenInventory();
            flashlight.SetActive(false);
            _isExiting = false;
        });
    }
    #endregion

    public void OnCompletePuzzle()
    {
        IsComplete = true;
        ExitPuzzle();
    }
}
