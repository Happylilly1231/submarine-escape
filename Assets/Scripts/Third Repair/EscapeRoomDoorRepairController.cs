using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class EscapeRoomDoorRepairController : PuzzleController, IInteractable
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => false;

    [SerializeField] private GameObject boxDoor;
    [SerializeField] private GameObject valve;
    [SerializeField] private Item escapeDoorHydraulicValveItem;
    [SerializeField] private Renderer redLightRenderer;
    [SerializeField] private Renderer greenLightRenderer;
    [SerializeField] private Material darkRedMaterial; // 어두운 빨간색으로 변경할 때 필요
    [SerializeField] private Material greenLightMaterial; // 초록빛으로 변경할 때 필요
    [SerializeField] private Door escapeRoomDoor; // 탈출실 문

    private bool _isComplete = false;

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!_isComplete)
            return "Open [E]";
        else
            return "";
    }

    public void Interact()
    {
        if (_isComplete)
            return;

        ActivatePuzzle(); // 패널 활성화
    }
    #endregion

    #region PuzzleController
    // public override void ActivatePuzzle()
    // {
    //     // SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화
    //     // itemEquipController.UnequipItem(); // 아이템 장착 해제
    //     // SubmarineInGameManager.instance.InteractorUI.SetActive(true); // 상호작용 UI 활성화

    //     base.ActivatePuzzle();
    // }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        KeyE.performed += OnKeyEPerformed;

        OpenDoor(); // 함 문 열기
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        KeyE.performed -= OnKeyEPerformed;

        if (!_isComplete) // 완료되면 밸브 때문에 닫을 수 X
            CloseDoor(); // 함 문 닫기
    }
    #endregion

    #region 입력 이벤트 함수
    /// <summary>
    /// 마우스 좌클릭 -> 스위치를 클릭했을 때만 Off 가능
    /// </summary>
    /// <param name="context"></param>
    private void OnKeyEPerformed(InputAction.CallbackContext context)
    {
        ItemEquipController itemEquipController = SubmarineInGameManager.instance.ItemEquipController;
        InventoryManager inventoryManager = SubmarineInGameManager.instance.InventoryManager;

        Item selectedItem = null;
        if (itemEquipController.HeldItemData != null)
        {
            selectedItem = itemEquipController.HeldItemData;
        }
        else if (inventoryManager.SelectedSlotIndex >= 0 && inventoryManager.InventorySlots[inventoryManager.SelectedSlotIndex] != null)
        {
            selectedItem = inventoryManager.InventorySlots[inventoryManager.SelectedSlotIndex].Item;
        }

        if (selectedItem == escapeDoorHydraulicValveItem)
        {
            Success(); // 성공
        }
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 함 문 열기
    /// </summary>
    public void OpenDoor()
    {
        boxDoor.transform.DOLocalRotate(new Vector3(0f, -90f, 0f), 0.8f)
            .SetEase(Ease.OutBounce);
    }

    /// <summary>
    /// 함 문 닫기
    /// </summary>
    public void CloseDoor()
    {
        boxDoor.transform.DOLocalRotate(Vector3.zero, 0.8f)
            .SetEase(Ease.OutBounce);
    }

    public void Success()
    {
        // 입력 막기
        SetInputLock(true);

        // 아이템 사용
        SubmarineInGameManager.instance.InventoryManager.ConsumeItemInSlot(escapeDoorHydraulicValveItem);

        // 밸브 보이게 활성화
        valve.SetActive(true);

        // 밸브 끼우고 돌리기
        Sequence seq = DOTween.Sequence();
        seq.Append(valve.transform.DOLocalMoveZ(-0.015f, 1f)); // 끼우기
        seq.Append(valve.transform.DOLocalRotate(new Vector3(360f, 90f, 90f), 1f, RotateMode.FastBeyond360)); // 1바퀴 회전
        seq.OnComplete(() =>
        {
            escapeRoomDoor.isRepairNeed = false; // 탈출실 문 수리 필요하지 않음으로 변경
            _isComplete = true; // 완료됨으로 설정

            // LED 색 변경
            redLightRenderer.material = darkRedMaterial; // 어두운 빨간색으로 변경
            greenLightRenderer.material = greenLightMaterial; // 초록빛으로 변경

            SetInputLock(false); // 입력 막기 해제
        });
    }
    #endregion
}
