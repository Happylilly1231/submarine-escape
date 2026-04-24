using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class KeyPadController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    public enum EPuzzleState { PanelScrewing, BatteryExchanging, Success }
    [Header("Current State")]
    [SerializeField] private EPuzzleState currentState = EPuzzleState.PanelScrewing;

    [Header("References")]
    [SerializeField] private GameObject flashlight;
    [SerializeField] private Transform doorPivot;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openingDoorSound;

    [Header("Panel Phase (Screws)")]
    [SerializeField] private CrewRoom1KeyPadPanel outerPanel;
    //[SerializeField] private GameObject outerPanel;
    [SerializeField] private KeyPadScrew[] screws;
    [SerializeField] private Item screwdriverItem;
    [SerializeField] private Transform itemViewRoot;

    [Header("KeyPad Phase (Batteries)")]
    [SerializeField] private CrewRoom1KeyPad interPanel;
    [SerializeField] private KeyPadBatterySlot[] batterySlots;
    [SerializeField] private Item[] batteryItems;
    [SerializeField] private Transform successViewPoint;
    [SerializeField] private GameObject successFlashlight;

    private KeyPadScrew _selectedScrew;
    private KeyPadBatterySlot _selectedBatterySlot;

    private int _removedScrewCount = 0;
    public bool IsActionProcessing { get; private set; } = false; // 애니메이션 중 입력 방지

    #region 퍼즐 시작/종료
    public override void ActivatePuzzle()
    {
        flashlight.SetActive(true);

        base.ActivatePuzzle();
    }
    public override void StartPuzzle()
    {
        ToggleColliders(true);

        base.StartPuzzle();

        Click.performed += OnPointerClick;
        KeyE.performed += OnExecuteAction;
    }

    public override void ExitPuzzle()
    {
        if (IsActionProcessing)
        {
            Debug.Log("모션 진행 중에는 퍼즐을 나갈 수 없습니다.");
            return;
        }

        if (_selectedScrew != null) _selectedScrew.DeSelect();
        if (_selectedBatterySlot != null) _selectedBatterySlot.DeSelect();

        flashlight.SetActive(false);
        ToggleColliders(false);

        Click.performed -= OnPointerClick;
        KeyE.performed -= OnExecuteAction;

        base.ExitPuzzle();
    }
    #endregion

    #region 입력 이벤트
    public void OnPointerClick(InputAction.CallbackContext context)
    {
        if (!IsPuzzleStarted || IsActionProcessing) return;

        Ray ray = Camera.main.ScreenPointToRay(Point.ReadValue<Vector2>());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (currentState == EPuzzleState.PanelScrewing)
            {
                if (hit.transform.TryGetComponent(out KeyPadScrew screw) && !screw.IsRemoved)
                {
                    if (_selectedScrew != null) _selectedScrew.DeSelect();
                    _selectedScrew = screw;
                    _selectedScrew.Select();
                }
            }
            else if (currentState == EPuzzleState.BatteryExchanging)
            {
                if (hit.transform.TryGetComponent(out KeyPadBatterySlot slot))
                {
                    if (_selectedBatterySlot != null) _selectedBatterySlot.DeSelect();
                    _selectedBatterySlot = slot;
                    _selectedBatterySlot.Select();
                }
            }
        }
    }

    private void OnExecuteAction(InputAction.CallbackContext context)
    {
        if (!IsPuzzleStarted || IsActionProcessing) return;

        if (currentState == EPuzzleState.PanelScrewing && _selectedScrew != null)
        {
            RemoveScrew();
        }
        else if (currentState == EPuzzleState.BatteryExchanging && _selectedBatterySlot != null)
        {
            ExchangeBattery();
        }
    }
    #endregion

    #region 나사 제거 로직
    private void RemoveScrew()
    {
        GameObject toolObj = GetToolObjectFromView(screwdriverItem);
        if (toolObj == null) return;

        IsActionProcessing = true;

        _selectedScrew.RemoveWithTool(toolObj, () =>
        {
            _removedScrewCount++;
            _selectedScrew = null;
            if (toolObj != null) itemEquipController.EquipItem(screwdriverItem);

            if (_removedScrewCount >= screws.Length)
            {
                outerPanel.IsCompleted = true;
                currentState = EPuzzleState.BatteryExchanging;
                RemovePanel();
                ToggleColliders(true);
            }

            IsActionProcessing = false;
        });
    }

    private GameObject GetToolObjectFromView(Item target)
    {
        foreach (Transform child in itemViewRoot)
        {
            if (child.TryGetComponent(out ItemPickUp p) && p.Item == target) return child.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 키패드 패널 제거
    /// </summary>
    private void RemovePanel()
    {
        IsActionProcessing = true;

        outerPanel.transform.DOMove(outerPanel.transform.position - outerPanel.transform.forward * 0.05f, 1.0f)
            .SetEase(Ease.OutQuad)
            .SetLink(outerPanel.gameObject)
            .OnComplete(() =>
        {
            Rigidbody rb = outerPanel.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.mass = 0.1f;
                rb.drag = 0.5f;
            }

            DOVirtual.DelayedCall(0.5f, () =>
            {
                IsActionProcessing = false;
                Debug.Log("패널 제거 완료. 현재 맵: " + SubmarineInGameManager.instance.playerInput.currentActionMap.name);

                if (SubmarineInGameManager.instance.playerInput.currentActionMap.name != "Puzzle")
                    SubmarineInGameManager.instance.playerInput.SwitchCurrentActionMap("Puzzle");
            });
        });
    }
    #endregion

    #region 배터리 교체 로직
    /// <summary>
    /// interPanel과 batterySlots의 콜라이드 토글
    /// </summary>
    /// <param name="isActive">퍼즐 활성화 상태</param>
    private void ToggleColliders(bool isActive)
    {
        if (currentState != EPuzzleState.BatteryExchanging) return;

        interPanel.GetComponent<BoxCollider>().enabled = !isActive;
        foreach (var slot in batterySlots)
        {
            var col = slot.GetComponent<BoxCollider>();
            if (col != null) col.enabled = isActive;
        }
    }
    private void ExchangeBattery()
    {
        Item heldBattery = itemEquipController.HeldItemData;

        if (heldBattery == null || !IsBatteryItem(heldBattery) || _selectedBatterySlot == null) return;

        IsActionProcessing = true;
        _selectedBatterySlot.DeSelect();

        KeyPadBatterySlot targetSlot = _selectedBatterySlot;
        _selectedBatterySlot = null;

        if (targetSlot.transform.childCount == 1) // 해당 슬롯에 배터리가 없는 경우 - 손에 든 배터리 넣음
        {
            inventoryManager.ConsumeItemInSlot(heldBattery);
            InstallNew(heldBattery, targetSlot);
        }
        else if (targetSlot.IsBurnt) // 해당 슬롯에 탄 배터리가 있는 경우 - 탄 배터리는 없애고 손에 든 배터리 넣음
        {
            targetSlot.RemoveBattery(true, () =>
            {
                inventoryManager.ConsumeItemInSlot(heldBattery);
                InstallNew(heldBattery, targetSlot);
            });
        }
        else // 해당 슬롯에 red/blue/green/yellow 배터리가 있는 경우 - 기존 배터리와 교체
        {
            Item currentBattery = targetSlot.CurrentBatteryData;
            targetSlot.RemoveBattery(false, () =>
            {
                inventoryManager.ConsumeItemInSlot(heldBattery);
                inventoryManager.AddItemToInventory(currentBattery);
                InstallNew(heldBattery, targetSlot);
            });
        }
    }

    private void InstallNew(Item battery, KeyPadBatterySlot targetSlot) => targetSlot.InstallBattery(battery, () => FinishExchange(targetSlot));

    private void FinishExchange(KeyPadBatterySlot targetSlot)
    {
        IsActionProcessing = false;
        CheckBatteryCompletion(targetSlot);
    }

    private void CheckBatteryCompletion(KeyPadBatterySlot targetSlot)
    {
        if (!targetSlot.IsCorret)
        {
            StartCoroutine(BatteryFailRoutine());
            return;
        }

        bool allClear = true;
        foreach (var slot in batterySlots) if (!slot.IsCorret) { allClear = false; break; }

        if (allClear)
        {
            interPanel.IsCompleted = true;
            HandleSuccess(targetSlot);
        }
    }

    private IEnumerator BatteryFailRoutine()
    {
        IsActionProcessing = true;
        yield return new WaitForSeconds(2.0f);

        foreach (var slot in batterySlots) { slot.StopAllEffects(); slot.EjectBattery(); }

        SubmarineInGameManager.instance.player.GetComponent<PlayerStat>().Damage(10, EEndingType.KeypadExplosion);

        Camera.main.transform.DOLocalMoveX(Camera.main.transform.localPosition.x + 0.7f, 0.1f)
            .SetLoops(2, LoopType.Yoyo) // 갔다가 다시 제자리로 살짝 돌아옴
            .SetEase(Ease.OutQuad);

        Camera.main.transform.DOShakePosition(0.5f, strength: 0.3f, vibrato: 30, randomness: 90);

        IsActionProcessing = false;
        DOVirtual.DelayedCall(0.8f, () => ExitPuzzle());
    }

    private void HandleSuccess(KeyPadBatterySlot targetSlot)
    {
        currentState = EPuzzleState.Success;
        IsActionProcessing = true;
        if (targetSlot) targetSlot.DeSelect();

        flashlight.SetActive(false);
        successFlashlight.SetActive(true);

        GameManager.instance.SetHaveToShowCursor(false);
        GameManager.instance.SetCursorVisible(false);

        if (audioSource) { audioSource.clip = openingDoorSound; audioSource.Play(); }

        Sequence seq = DOTween.Sequence();

        // 1. 카메라 이동 및 회전
        seq.Append(Camera.main.transform.DOMove(successViewPoint.position, 1.5f)
            .SetEase(Ease.OutQuad));
        seq.Join(Camera.main.transform.DORotateQuaternion(successViewPoint.rotation, 1.5f)
            .SetEase(Ease.OutQuad));

        // 2. 0.5초 대기 후 문 열림
        seq.AppendInterval(0.5f);
        seq.Append(doorPivot.DORotate(new Vector3(0, 70f, 0), 3f).SetEase(Ease.OutQuad));

        // 3. 1초 대기 후 퍼즐 비활성화
        seq.AppendInterval(0.5f);
        seq.OnComplete(() =>
        {
            doorPivot.GetChild(0).GetComponent<Door>().isLocked = false; // 문 잠금 해제
            IsActionProcessing = false;
            ExitPuzzle();
        });
    }

    private bool IsBatteryItem(Item item)
    {
        foreach (var battery in batteryItems)
        {
            if (item == battery) return true;
        }
        return false;
    }

    #endregion
}
