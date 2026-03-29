using System.Collections;
using System.Collections.Generic;
using NavKeypad;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class CrewRoom1KeyPadPanel : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject keyPadPanel;
    [SerializeField] private KeyPadScrew[] screws;
    [SerializeField] private Item screwdriver;
    [SerializeField] private Transform itemViewRoot;
    [SerializeField] private GameObject flashlight;
    [Header("Focus View Settings")]
    [SerializeField] private Vector3 focusViewPos;
    [SerializeField] private Vector3 focusViewRot;
    [SerializeField] private float duration = 1.0f;

    private InventoryManager _inventoryManager;
    private ItemEquipController _itemEquipController;
    private PlayerCameraController _playerCameraController;
    private CrewRoom1KeyPad _crewRoom1KeyPad;
    private PlayerInput _playerInput; // 플레이어 입력 컴포넌트
    private InputAction _selectScrewAction;
    private InputAction _removeScrewAction;
    private InputAction _exitKeyPadAction;
    private bool _isFocused = false;
    public bool IsFocused => _isFocused;
    private int _removedScrewCount = 0;
    private Camera _mainCamera;
    private KeyPadScrew _currentSelectedScrew = null;
    private bool _isRemovingScrew = false;

    void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _playerInput = SubmarineInGameManager.instance.player.GetComponent<PlayerInput>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _playerCameraController = FindObjectOfType<PlayerCameraController>();
        _crewRoom1KeyPad = FindObjectOfType<CrewRoom1KeyPad>();

        if (_playerInput != null)
        {
            _selectScrewAction = _playerInput.actions["SelectScrew"];
            _removeScrewAction = _playerInput.actions["RemoveScrew"];
            _exitKeyPadAction = _playerInput.actions["ExitKeyPad"];
        }

        _mainCamera = Camera.main;
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (_isFocused) return "";
        if (IsScrewdriverSelected())
        {
            return "open [E]";
        }
        else
        {
            return "Locked (Need Screwdriver)";
        }
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 키패드 패널 확대
    /// </summary>
    public void Interact()
    {
        if (_isFocused) return;
        if (IsScrewdriverSelected())
        {
            StartFocusMode();
        }
    }

    /// <summary>
    /// 키패드 패널 확대 모드 시작
    /// </summary>
    private void StartFocusMode()
    {
        _isFocused = true;

        SubmarineInGameManager.instance.SetPlayerGeoActive(false);
        _playerCameraController.enabled = false;
        flashlight.SetActive(true);

        Debug.Log(_playerInput.currentActionMap);
        _playerInput.currentActionMap.Disable();
        string[] allowedActions = { "THold", "SlotKeyPress", "ItemUse", "SelectScrew", "RemoveScrew", "ExitKeyPad" };
        foreach (string action in allowedActions)
        {
            _playerInput.actions[action].Enable();
        }

        // Item heldBattery = _itemEquipController.HeldItemData;
        // if (heldBattery.ItemName == "Flashlight") _itemEquipController.UnequipItem();

        _mainCamera.transform.DOMove(focusViewPos, duration).SetEase(Ease.InOutSine);
        _mainCamera.transform.DORotate(focusViewRot, duration).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        });

    }

    /// <summary>
    /// 선택된 아이템으로 상호작용 가능한지 여부
    /// </summary>
    public bool CanInteractwithSelectedItem(Item item)
    {
        return item == screwdriver;
    }

    /// <summary>
    /// 드라이버가 선택되었는지 여부
    /// </summary>
    private bool IsScrewdriverSelected()
    {
        List<Item> candidateItems = new List<Item>();
        if (_itemEquipController.HeldItemData != null)
        {
            candidateItems.Add(_itemEquipController.HeldItemData);
        }
        if (_inventoryManager.SelectedSlotIndex >= 0 && _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex] != null)
        {
            Item slotItem = _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item;
            // 손에 든 아이템과 슬롯 아이템이 중복되지 않을 때만 추가
            if (!candidateItems.Contains(slotItem))
            {
                candidateItems.Add(slotItem);
            }
        }

        bool canInteract = false;

        if (candidateItems.Count > 0)
        {
            foreach (Item item in candidateItems)
            {
                if (CanInteractwithSelectedItem(item))
                {
                    canInteract = true;
                    break;
                }
            }
        }

        return canInteract;
    }

    void OnEnable()
    {
        if (_selectScrewAction != null)
        {
            _selectScrewAction.performed += OnSelectScrew;
        }
        if (_removeScrewAction != null)
        {
            _removeScrewAction.performed += OnRemoveScrew;
        }
        if (_exitKeyPadAction != null)
        {
            _exitKeyPadAction.performed += OnExitKeyPad;
        }
    }

    void OnDisable()
    {
        if (_selectScrewAction != null)
        {
            _selectScrewAction.performed -= OnSelectScrew;
        }
        if (_removeScrewAction != null)
        {
            _removeScrewAction.performed -= OnRemoveScrew;
        }
        if (_exitKeyPadAction != null)
        {
            _exitKeyPadAction.performed -= OnExitKeyPad;
        }
    }

    /// <summary> 나사 선택
    /// <para> - 선택된 나사의 outline을 표시 </para>
    /// </summary>
    public void OnSelectScrew(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;
        if (_isRemovingScrew) return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.TryGetComponent(out KeyPadScrew screw))
            {
                if (!screw.IsRemoved)
                {
                    if (_currentSelectedScrew != null && _currentSelectedScrew != screw)
                    {
                        _currentSelectedScrew.DeSelect();
                    }

                    _currentSelectedScrew = screw;
                    _currentSelectedScrew.Select();
                }
            }
        }
    }

    /// <summary>
    /// 드라이버를 이용해서 나사 제거
    /// </summary>
    public void OnRemoveScrew(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;
        if (_currentSelectedScrew == null) return;

        GameObject screwdriverObject = null;
        foreach (Transform child in itemViewRoot)
        {
            if (child.TryGetComponent(out ItemPickUp item))
            {
                if (item.Item == screwdriver)
                {
                    screwdriverObject = child.gameObject;
                    break;
                }
            }
        }

        if (screwdriverObject == null) return;
        _isRemovingScrew = true;
        // _playerInput.actions["ReturnToSlot"].Disable();
        _playerInput.actions["ItemUse"].Disable();

        _currentSelectedScrew.RemoveWithTool(screwdriverObject, () =>
        {
            _removedScrewCount++;
            _currentSelectedScrew = null;
            _itemEquipController.EquipItem(screwdriver);
            _isRemovingScrew = false;
            // _playerInput.actions["ReturnToSlot"].Enable();
            _playerInput.actions["ItemUse"].Enable();

            if (_removedScrewCount >= screws.Length)
            {
                RemovePanel();
                _crewRoom1KeyPad.StartFocusMode();
                _isFocused = false;
            }
        });
    }

    /// <summary>
    /// 나사 4개 전부 제거 후 키패드 패널 제거
    /// </summary>
    private void RemovePanel()
    {
        this.transform.DOMove(this.transform.position - this.transform.forward * 0.05f, 1.0f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.mass = 0.1f;
            rb.drag = 0.5f;
            Destroy(this);
        });
    }

    /// <summary>
    /// 키패드 패널 축소 및 플레이어 복귀
    /// </summary>
    public void OnExitKeyPad(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;

        _isFocused = false;

        if (_currentSelectedScrew != null)
        {
            _currentSelectedScrew.DeSelect();
            _currentSelectedScrew = null;
        }

        _playerCameraController.enabled = true;
        SubmarineInGameManager.instance.SetPlayerGeoActive(true);
        _playerInput.currentActionMap.Enable();
        flashlight.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
