using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Unity.VisualScripting;

public class CrewRoom1KeyPad : MonoBehaviour, IInteractable
{
    [SerializeField] private KeyPadBatterySlot[] batterySlots;
    [SerializeField] private Item[] batteryItems;
    [SerializeField] private Transform DoorPivot;

    [Header("Focus View Settings")]
    [SerializeField] private Vector3 focusViewPos;
    [SerializeField] private Vector3 focusViewRot;
    [SerializeField] private float duration = 1.0f;

    [Header("Success View Settings")]
    [SerializeField] private Vector3 successViewPos;
    [SerializeField] private Vector3 successViewRot;
    [SerializeField] private float successDuration = 3.0f;

    private PlayerCameraController _playerCameraController;
    private ItemEquipController _itemEquipController;
    private InventoryManager _inventoryManager;
    private PlayerInteractor _playerInteractor;
    private PlayerStat _playerStat;

    private PlayerInput _playerInput;
    private InputAction _exitKeyPadAction;
    private InputAction _selectBatteryAction;
    private InputAction _exchangeBatteryAction;
    private Camera _mainCamera;
    private bool _isFocused;
    private KeyPadBatterySlot _currentSelectedBatterySlot = null;

    void Awake()
    {
        _playerCameraController = FindObjectOfType<PlayerCameraController>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _playerInteractor = FindObjectOfType<PlayerInteractor>();
        _playerStat = SubmarineInGameManager.instance.player.GetComponent<PlayerStat>();
        _playerInput = SubmarineInGameManager.instance.player.GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            _exitKeyPadAction = _playerInput.actions["ExitKeyPad"];
            _selectBatteryAction = _playerInput.actions["SelectScrew"];
            _exchangeBatteryAction = _playerInput.actions["RemoveScrew"];
        }
        _mainCamera = Camera.main;
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        //if (_isFocused) return "";
        return "keyPad [E]";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 키패드 패널 확대
    /// </summary>
    public void Interact()
    {
        //if (_isFocused) return;
        StartFocusMode();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    private void OnEnable()
    {
        if (_exitKeyPadAction != null)
        {
            _exitKeyPadAction.performed += OnExitKeyPad;
            _selectBatteryAction.performed += OnClickSlot;
            _exchangeBatteryAction.performed += OnExchangeBattery;
        }
    }

    private void OnDisable()
    {
        if (_exitKeyPadAction != null)
        {
            _exitKeyPadAction.performed -= OnExitKeyPad;
            _selectBatteryAction.performed -= OnClickSlot;
            _exchangeBatteryAction.performed -= OnExchangeBattery;
        }
    }

    public void StartFocusMode()
    {
        _isFocused = true;

        SubmarineInGameManager.instance.SetPlayerGeoActive(false);
        _playerCameraController.enabled = false;
        _playerInteractor.IsPuzzleActive = true;
        _playerInteractor.ClearDetectionText();

        foreach (var slot in batterySlots)
        {
            var col = slot.GetComponent<BoxCollider>();
            if (col != null) col.enabled = true;
        }

        _playerInput.currentActionMap.Disable();
        string[] allowedActions = { "ToggleInventory", "THold", "SlotKeyPress", "ItemUse", "ReturnToSlot", "SelectScrew", "RemoveScrew", "ExitKeyPad" };
        foreach (string action in allowedActions)
        {
            _playerInput.actions[action].Enable();
        }

        _mainCamera.transform.DOMove(focusViewPos, duration).SetEase(Ease.InOutSine);
        _mainCamera.transform.DORotate(focusViewRot, duration).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        });
    }

    /// <summary>
    /// 키패드 패널 축소 및 플레이어 복귀
    /// </summary>
    public void OnExitKeyPad(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;

        ForceExitKeyPad();
    }

    private void ForceExitKeyPad()
    {
        if (!_isFocused) return;
        _isFocused = false;

        foreach (var slot in batterySlots)
        {
            var col = slot.GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
        }
        if (_currentSelectedBatterySlot != null)
        {
            _currentSelectedBatterySlot.DeSelect();
            _currentSelectedBatterySlot = null;
        }

        _playerCameraController.enabled = true;
        _playerInteractor.IsPuzzleActive = false;
        SubmarineInGameManager.instance.SetPlayerGeoActive(true);
        _playerInput.currentActionMap.Enable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnClickSlot(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.TryGetComponent(out KeyPadBatterySlot batterySlot))
            {
                Debug.Log("Clicked Battery Slot");

                if (_currentSelectedBatterySlot != null && _currentSelectedBatterySlot != batterySlot)
                {
                    _currentSelectedBatterySlot.DeSelect();
                }

                _currentSelectedBatterySlot = batterySlot;
                _currentSelectedBatterySlot.Select();
            }
        }
    }

    public void OnExchangeBattery(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed || _currentSelectedBatterySlot == null) return;

        Item heldBattery = _itemEquipController.HeldItemData;
        Debug.Log(heldBattery);
        if (heldBattery == null) return;
        if (!IsBatteryItem(heldBattery)) return;

        _playerInput.actions["ItemUse"].Disable();
        _playerInput.actions["ReturnToSlot"].Disable();
        _playerInput.actions["ExitKeyPad"].Disable();
        _playerInput.actions["SelectScrew"].Disable();

        if (_currentSelectedBatterySlot.transform.childCount == 1)
        {
            _inventoryManager.ConsumeItemInSlot(heldBattery);
            _currentSelectedBatterySlot.InstallBattery(heldBattery, () =>
            {
                EnableActions();
                CheckPuzzleCompletion();
            });
        }
        else if (_currentSelectedBatterySlot.IsBurnt)
        {
            _currentSelectedBatterySlot.RemoveBattery(true, () =>
            {
                _inventoryManager.ConsumeItemInSlot(heldBattery);
                _currentSelectedBatterySlot.InstallBattery(heldBattery, () =>
                {
                    EnableActions();
                    CheckPuzzleCompletion();
                });
            });
        }
        else
        {
            Item oldBattery = _currentSelectedBatterySlot.CurrentBatteryData;
            _currentSelectedBatterySlot.RemoveBattery(false, () =>
            {
                _inventoryManager.ConsumeItemInSlot(heldBattery);
                _inventoryManager.AddItemToInventory(oldBattery);
                _currentSelectedBatterySlot.InstallBattery(heldBattery, () =>
                {
                    EnableActions();
                    CheckPuzzleCompletion();
                });
            });
        }
    }

    private void EnableActions()
    {
        _playerInput.actions["ReturnToSlot"].Enable();
        _playerInput.actions["ItemUse"].Enable();
        _playerInput.actions["ExitKeyPad"].Enable();
        _playerInput.actions["SelectScrew"].Enable();
    }

    private bool IsBatteryItem(Item item)
    {
        if (item == null) return false;
        foreach (var battery in batteryItems)
        {
            if (item == battery) return true;
        }
        return false;
    }

    private void CheckPuzzleCompletion()
    {
        if (!_currentSelectedBatterySlot.IsCorret)
        {
            StartCoroutine(DelayedFailRoutine());
            return;
        }

        bool allCorrect = true;
        foreach (var slot in batterySlots)
        {
            if (!slot.IsCorret)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            Debug.Log("Complete!");
            HandleSuccess();
        }
    }

    private IEnumerator DelayedFailRoutine()
    {
        _playerInput.actions["RemoveScrew"].Disable();
        _playerInput.actions["ExitKeyPad"].Disable();

        yield return new WaitForSeconds(3f);

        PuzzleFail();
    }

    private void PuzzleFail()
    {
        foreach (var slot in batterySlots)
        {
            slot.StopLED();
            slot.EjectBattery();
        }

        _playerStat.Damage(10);

        _playerInput.actions["RemoveScrew"].Enable();
        _playerInput.actions["ExitKeyPad"].Enable();

        DOVirtual.DelayedCall(0.5f, () => ForceExitKeyPad());
    }

    private void HandleSuccess()
    {
        _mainCamera.transform.DOMove(successViewPos, successDuration).SetEase(Ease.InOutCubic);
        _mainCamera.transform.DORotate(successViewRot, successDuration).SetEase(Ease.InOutCubic);

        DG.Tweening.Sequence successSeq = DOTween.Sequence();
        successSeq.AppendInterval(0.5f);
        successSeq.Append(DoorPivot.DORotate(new Vector3(0, -70f, 0), successDuration).SetEase(Ease.OutQuad));

        successSeq.OnComplete(() =>
        {
            DOVirtual.DelayedCall(1f, () => ForceExitKeyPad());
        });
    }
}
