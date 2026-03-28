using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ElectricalBox : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject electricalBox_door;
    [SerializeField] private GameObject powerPuzzle_panel;
    [SerializeField] private GameObject flashlight;
    [Header("Focus View Settings")]
    [SerializeField] private Vector3 focusViewPos;
    [SerializeField] private Vector3 focusViewRot;
    [SerializeField] private float duration = 1.0f;
    private bool _isFocused = false;
    public bool IsFocused => _isFocused;
    private bool _isComplete = false;
    public bool IsComplete => _isComplete;
    private Camera _mainCamera;
    private InventoryManager _inventoryManager;
    private ItemEquipController _itemEquipController;
    private PlayerCameraController _playerCameraController;
    private PowerSwitch _powerSwitch;
    private PlayerInput _playerInput; // 플레이어 입력 컴포넌트
    private InputAction _exitFocuseModeAction;

    void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _playerCameraController = FindObjectOfType<PlayerCameraController>();
        _powerSwitch = FindObjectOfType<PowerSwitch>();

        _playerInput = SubmarineInGameManager.instance.player.GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            _exitFocuseModeAction = _playerInput.actions["ExitKeyPad"];
        }

        _mainCamera = Camera.main;
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (_isFocused) return "";
        else return "open [E]";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 키패드 패널 확대
    /// </summary>
    public void Interact()
    {
        if (_isFocused) return;
        else StartFocusMode();
    }

    /// <summary>
    /// 선택된 아이템으로 상호작용 가능한지 여부
    /// </summary>
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    /// <summary>
    /// 확대 모드 시작
    /// </summary>
    private void StartFocusMode()
    {
        _isFocused = true;

        SubmarineInGameManager.instance.SetPlayerGeoActive(false);
        _itemEquipController.UnequipItem();
        _inventoryManager.CloseInventory();
        _playerCameraController.enabled = false;
        if (!_powerSwitch.IsPowerRestored) flashlight.SetActive(true);

        _playerInput.currentActionMap.Disable();
        _playerInput.actions["ExitKeyPad"].Enable();

        _mainCamera.transform.DOMove(focusViewPos, duration).SetEase(Ease.InOutSine);
        _mainCamera.transform.DORotate(focusViewRot, duration).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            Sequence doorSeq = DOTween.Sequence();

            doorSeq.Append(electricalBox_door.transform.DOLocalRotate(new Vector3(90, 0, -30), 0.8f).SetEase(Ease.OutBounce));
            doorSeq.InsertCallback(1.5f, () =>
            {
                powerPuzzle_panel.SetActive(true);
                if (!_isComplete)
                {
                    _playerInput.actions["PowerPuzzle"].Enable();

                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            });
        });
    }

    private void OnEnable()
    {
        if (_exitFocuseModeAction != null) _exitFocuseModeAction.performed += OnExitMode;
    }

    private void OnDisable()
    {
        if (_exitFocuseModeAction != null) _exitFocuseModeAction.performed -= OnExitMode;
    }

    public void OnExitMode(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;

        ForceExitMode(false);
    }

    /// <summary>
    /// 축소 및 플레이어 복귀
    /// </summary>
    public void ForceExitMode(bool isComplete)
    {
        if (!_isFocused) return;

        _isFocused = false;

        powerPuzzle_panel.SetActive(false);

        Sequence doorSeq = DOTween.Sequence();

        doorSeq.Append(electricalBox_door.transform.DOLocalRotate(new Vector3(90, 0, 66), 0.8f).SetEase(Ease.OutBounce));
        doorSeq.InsertCallback(1f, () =>
        {
            _playerCameraController.enabled = true;
            SubmarineInGameManager.instance.SetPlayerGeoActive(true);
            _playerInput.currentActionMap.Enable();
            flashlight.SetActive(false);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        });

        if (isComplete == true)
        {
            _isComplete = isComplete;
        }
    }
}
