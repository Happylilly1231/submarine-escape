using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    [SerializeField] private ElectricalBox electricalBox;
    [SerializeField] private GameObject powerPuzzle_panel;
    [SerializeField] private GameObject flashlight;
    [SerializeField] private PowerGridManager powerGridManager;
    [SerializeField] private GameObject statUI;

    public bool IsComplete = false;
    private bool _isExiting = false; // 문 여는 모션 도중 Esc키 누르면 panel 안 보이게 하기 위해서

    public override void Start()
    {
        base.Start();
    }

    #region 퍼즐 시작/종료
    public override void ActivatePuzzle()
    {
        statUI.SetActive(false);
        if (LightingManager.instance != null && !LightingManager.instance.IsPowerOn)
            flashlight.SetActive(true);
        inventoryManager.CloseInventory();
        itemEquipController.UnequipItem();

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
