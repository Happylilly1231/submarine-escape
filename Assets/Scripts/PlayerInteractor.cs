using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어가 오브젝트(아이템, 문, 서랍 등)와 상호작용할 수 있도록 감지하고 처리
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float rayDistance = 3.0f; // 상호작용 감지 거리
    [SerializeField] private Camera playerCamera; // 플레이어 카메라
    [SerializeField] private TMPro.TextMeshProUGUI interactorText; // 상호작용 UI - 감지된 오브젝트와의 상호작용 키 표시
    [SerializeField] private GameObject interactorUI; // 상호작용 UI
    [SerializeField] private GameObject aimUI; // 조준점 UI

    private InventoryManager _inventoryManager; // 인벤토리 매니저
    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러
    private PlayerMutation _playerMutation; // 플레이어 괴물화

    private ItemPickUp _currentItem; // 현재 감지된 아이템
    private LabEquipment _currentEquipment; // 현재 감지된 실험기구
    public LabEquipment CurrentEquipment => _currentEquipment;
    private IInteractable _currentFurniture; // 현재 감지된 가구
    private RaycastHit _sphereCastHit; // SphereCast로 감지된 오브젝트 정보
    private bool _canPickUp = false; // 아이템 줍기 가능 여부
    private bool _canInteractable = false; // 가구 상호작용 가능 여부
    private bool _isHoldingEquipment = false;
    public bool IsHoldingEquipment => _isHoldingEquipment;
    private LabEquipment _heldEquipment;
    public LabEquipment HeldEquipment => _heldEquipment;
    public bool IsPuzzleActive = false; // 플레이어가 퍼즐을 풀고 있는 상태

    private int detectLayerMask; // 감지 레이어 (괴물 제외하기 위해 만듦)

    void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _playerMutation = FindObjectOfType<PlayerMutation>();

        detectLayerMask = ~(LayerMask.GetMask("Monster") | LayerMask.GetMask("Undectectable")); // 괴물과 감지 불가 레이어(선반 같은 경우 통째로 콜라이더가 있는데 선반 안에 아이템을 놓으면 선반 콜라이더에 가려져서 감지를 못하기 때문에 도입)는 감지 레이어에서 제외
    }

    void Update()
    {
        if (!IsPuzzleActive) DetectObject();
    }

    /// <summary>
    /// 상호작용 UI 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetActiveInteractorUI(bool isActive)
    {
        interactorUI.SetActive(isActive);
    }

    /// <summary>
    /// 조준점 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetActiveAimUI(bool isActive)
    {
        aimUI.SetActive(isActive);
    }

    /// <summary>
    /// 플레이어가 가구와 상호작용 가능한지 여부
    /// </summary>
    public bool IsFocusingInteractable() => _canInteractable && _currentFurniture != null;

    /// <summary>
    /// 선택된 아이템으로 현재 감지된 가구와 상호작용 가능한지 여부
    /// </summary>
    public bool IsMatchingItemFocused(Item item)
    {
        if (!IsFocusingInteractable()) return false;
        return _currentFurniture.CanInteractwithSelectedItem(item);
    }

    #region 입력 이벤트
    /// <summary>
    /// F키 입력으로 아이템 줍기
    /// </summary>
    public void OnItemPickUp(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (_canPickUp && _currentItem != null)
        {
            TryPickUpItem();
        }
    }

    /// <summary>
    /// 마우스 휠로 실험기구 슬롯 선택
    /// </summary>
    /// <param name="context"></param>
    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        if (!context.performed || _currentEquipment == null) return;

        Vector2 scrollValue = context.ReadValue<Vector2>();

        if (scrollValue.y != 0)
        {
            SampleSlotUIManager.Instance.UpdateSelection(scrollValue.y);
        }
    }

    /// <summary>
    /// E키 입력으로 가구/실험기구와 상호작용
    /// </summary>
    public void OnInteractor(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // 주사기를 손에 든 경우
        if (_heldEquipment != null)
        {
            if (_heldEquipment is Syringe syringe)
            {
                // 주사기 내부 데이터 삭제
                for (int i = 0; i < syringe.Slots.Length; i++)
                {
                    syringe.Remove(i);
                }
                _playerMutation.InjectSerum(syringe.IsSuccess); // 플레이어에게 주입
                if (_currentEquipment)
                    if (_isHoldingEquipment)
                    {
                        // 내려놓기
                        TryPlaceEquipment();
                    }
                _inventoryManager.UpdateActionText();
            }
        }

        // 생체 데이터 추출기를 들고 있지 않을 때만 -> LabEquipment와 상호작용 가능
        // (생체 데이터 추출기 아이템을 들고 있을 때는 페트리 접시의 IInteractable과 상호작용해야 하기 때문)
        if (!_itemEquipController.HasItem || _itemEquipController.HeldItemData.ItemName != "Bio Data Extractor")
        {
            // 1. 실험기구와 상호작용 중인 경우 
            if (_currentEquipment != null)
            {
                int selectedIdx = SampleSlotUIManager.Instance.CurrentSelectedIdx;

                // 괴물 피 든 페트리 접시 흔들어서 내용물 섞기
                if (_currentEquipment is PetriDish petriDish1)
                {
                    if (!petriDish1.isMonsterBloodMixed && petriDish1.isMonsterBloodDropped)
                    {
                        petriDish1.MixMonsterBlood();
                    }
                }
                // 현미경 카메라 뷰 전환
                if (_currentEquipment is Microscope microscope)
                {
                    if (!_currentEquipment.Slots[0].IsEmpty)
                    {
                        microscope.InteractMicroscope();
                        _inventoryManager.UpdateActionText();
                        return;
                    }
                }
                // 원심분리기 작동
                if (_currentEquipment is Centrifuge centrifuge)
                {
                    if (centrifuge.CanStartOperation())
                    {
                        centrifuge.StartCentrifuge();
                        _inventoryManager.UpdateActionText();
                        return;
                    }
                }
                if (!_currentEquipment.Slots[selectedIdx].IsEmpty) return;

                // 손에 실험기구를 들고 있는 경우 - 슬롯에 넣기
                if (_isHoldingEquipment && _heldEquipment != null)
                {
                    if (_heldEquipment is TestTube tube && tube.IsResultTube)
                    {
                        if (_currentEquipment is PetriDish petriDish)
                        {
                            petriDish.SetAsResult(true, tube.IsSuccess, tube.IsH1, tube.IsH2S1);
                            petriDish.Insert(0, tube);
                            _inventoryManager.UpdateActionText();
                            return;
                        }
                    }
                    if (_currentEquipment.CanInsert(_heldEquipment))
                    {
                        // 페트리 접시인 경우: 샘플만 옮기고 손에서 해제하지 않음
                        if (_heldEquipment is PetriDish && _currentEquipment is TestTube)
                        {
                            _currentEquipment.Insert(selectedIdx, _heldEquipment);
                        }
                        else if (_heldEquipment is TestTube && _currentEquipment is Syringe)
                        {
                            _currentEquipment.Insert(selectedIdx, _heldEquipment);
                        }
                        else // 그 외 기구(시험관)는 슬롯에 넣고 손에서 해제
                        {
                            UnselectedSlot();
                            _currentEquipment.Insert(selectedIdx, _heldEquipment);
                            _heldEquipment = null;
                            _isHoldingEquipment = false;
                            SetSlotKeyEnabled(true);
                        }
                    }
                }
                // 인벤토리에서 샘플 아이템을 선택한 경우
                else if (_itemEquipController.HasItem && _itemEquipController.HeldItemData.ItemType == EItemType.Sample)
                {
                    Item item = _itemEquipController.HeldItemData;
                    if (_currentEquipment.CanInsert(item))
                    {
                        UnselectedSlot();
                        _currentEquipment.Insert(selectedIdx, item);
                        _inventoryManager.ConsumeItemInSlot(item);
                    }
                }
                _inventoryManager.UpdateActionText();
            }
        }

        // 2. 일반 가구와 상호작용 중인 경우
        if (_canInteractable && _currentFurniture != null)
        {
            bool canInteract = false;
            Item validItem = null;

            if (_itemEquipController.HasItem)
            {
                if (_currentFurniture.CanInteractwithSelectedItem(_itemEquipController.HeldItemData))
                {
                    canInteract = true;
                    validItem = _itemEquipController.HeldItemData;
                }
                if (_itemEquipController.HeldItemData.ItemName == "Flashlight")
                {
                    canInteract = true;
                }
            }
            if (_inventoryManager.SelectedSlotIndex < 0 || _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item == null || canInteract)
            {
                _currentFurniture.Interact();
                if (canInteract && validItem != null && validItem.IsConsumable)
                {
                    _inventoryManager.ConsumeItemInSlot(validItem);
                }
            }
        }
    }

    /// <summary>
    /// R키 입력으로 현재 선택된 슬롯의 내용물을 빼냄
    /// <para> - 샘플이면 인벤토리로, 실험기구면 손으로 </para>
    /// </summary>
    /// <param name="context"></param>
    public void OnWithdrawItem(InputAction.CallbackContext context)
    {
        if (!context.performed || _currentEquipment == null) return;

        // 조합 결과물일때는 내용물 못 빼냄
        if (_currentEquipment is TestTube tube && tube.IsResultTube) return;
        if (_currentEquipment is PetriDish petriDish && petriDish.ContainsResult) return;

        int selectedIdx = SampleSlotUIManager.Instance.CurrentSelectedIdx;
        SlotData targetSlot = _currentEquipment.Slots[selectedIdx];

        if (targetSlot.IsEmpty) return;

        if (targetSlot.HasEqipment)
        {
            // 기구가 들어있다면 손으로 빼기 (이미 무언가 들고 있다면 불가)
            if (_isHoldingEquipment) return;
            // 원심분리기 작동 중에는 슬롯의 내용물 못뺌
            if (_currentEquipment is Centrifuge centrifuge)
            {
                if (centrifuge.IsOperating) return;
            }

            UnselectedSlot();

            _heldEquipment = targetSlot.equipment;
            _currentEquipment.Remove(selectedIdx);
            _isHoldingEquipment = true;
            SetSlotKeyEnabled(false);
        }
        else if (_inventoryManager.AddItemToInventory(targetSlot.sample))
        {
            // 샘플이 들어있다면 인벤토리로
            _currentEquipment.Remove(selectedIdx);
        }
        _inventoryManager.UpdateActionText();
    }

    /// <summary>
    /// G키 입력으로 실험기구 잡기
    /// </summary>
    /// <param name="context"></param>
    public void OnGrab(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_currentEquipment)

            if (_isHoldingEquipment)
            {
                // 내려놓기
                TryPlaceEquipment();
            }
            else if (_currentEquipment != null && _currentEquipment.IsGrabbable)
            {
                UnselectedSlot();

                _heldEquipment = _currentEquipment;
                _heldEquipment.PickUp();
                _isHoldingEquipment = true;
                SetSlotKeyEnabled(false);
                SampleSlotUIManager.Instance.HideSlotUI();
            }
        _inventoryManager.UpdateActionText();
    }
    #endregion

    private void UnselectedSlot()
    {
        _itemEquipController.UnequipItem();
        _inventoryManager.CloseInventory();
        _inventoryManager.OpenInventory();
    }

    private void SetSlotKeyEnabled(bool isEnable)
    {
        var slotKeyAction = SubmarineInGameManager.instance.playerInput.actions.FindAction("SlotKeyPress");
        if (slotKeyAction != null)
        {
            if (isEnable) slotKeyAction.Enable();
            else slotKeyAction.Disable();
        }
    }

    private void TryPlaceEquipment()
    {
        _heldEquipment.Place();
        _heldEquipment = null;
        _isHoldingEquipment = false;

        SetSlotKeyEnabled(true);

        _inventoryManager.UpdateActionText();
    }

    #region 오브젝트 감지
    /// <summary>
    /// 플레이어 앞에 있는 아이템 또는 가구 감지
    /// <para> - 아이템이 감지된 경우 아이템 줍기 UI 표시 </para>
    /// <para> - 실험기구가 감지된 경우 실험기구 슬롯 UI 표시 </para>
    /// <para> - 가구가 감지된 경우 상호작용 UI 표시. 가구와 상호작용이 안 되는 아이템을 든 경우 UI 표시 안됨. </para>
    /// <para> - 아무것도 감지되지 않은 경우 UI 초기화 </para>
    /// </summary>
    private void DetectObject()
    {
        ClearDetection(); // 추출기 들고 페트리 접시 바라보고 있을 때 추출기 내리면 상호작용 텍스트가 안 사라져서 먼저 초기화하게 해줌
        if (Physics.SphereCast(playerCamera.transform.position, 0.1f, playerCamera.transform.forward, out _sphereCastHit, rayDistance, detectLayerMask))
        {
            // 1. 아이템 감지
            if (_sphereCastHit.transform.TryGetComponent(out ItemPickUp item))
            {
                HandleItem(item);
                SampleSlotUIManager.Instance.HideSlotUI();
                return;
            }

            // 생체 데이터 추출기를 들고 있지 않을 때만 -> LabEquipment와 상호작용 가능
            // (생체 데이터 추출기 아이템을 들고 있을 때는 페트리 접시의 IInteractable과 상호작용해야 하기 때문)
            if (!_itemEquipController.HasItem || _itemEquipController.HeldItemData.ItemName != "Bio Data Extractor")
            {
                // 2. 실험 기구 감지
                if (_sphereCastHit.transform.TryGetComponent(out LabEquipment equipment))
                {
                    // 손에 든 기구와 동일한 기구라면 감지 무시
                    if (_heldEquipment == equipment) { ClearDetection(); return; }
                    HandleLabEquipment(equipment);
                    return;
                }
            }

            // 3. 가구 감지
            if (_sphereCastHit.transform.TryGetComponent(out IInteractable furniture))
            {
                bool canInteract = false;

                if (_itemEquipController.HasItem) // 현재 장착된 아이템이 가구와 상호작용 가능한지 또는 손전등인지 확인
                {
                    if (furniture.CanInteractwithSelectedItem(_itemEquipController.HeldItemData) || _itemEquipController.HeldItemData.ItemName == "Flashlight")
                    {
                        canInteract = true;
                    }
                }
                // 선택된 아이템이 없거나 선택된 아이템이 가구와 상호작용 가능한 경우 UI 표시
                if (_inventoryManager.SelectedSlotIndex < 0 || _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item == null || canInteract)
                {
                    HandleInteractable(furniture);
                    SampleSlotUIManager.Instance.HideSlotUI();
                    return;
                }
            }
        }
        SampleSlotUIManager.Instance.HideSlotUI();
    }
    #endregion

    #region UI 및 상태 처리
    /// <summary>
    /// 아이템 감지 시 UI 및 상태 처리
    /// </summary>
    private void HandleItem(ItemPickUp item)
    {
        if (_itemEquipController.HasItem && _itemEquipController.HeldItemObject.TryGetComponent(out ItemPickUp heldItem))
        {
            if (heldItem == item)
            {
                // Debug.Log("현재 장착된 아이템과 감지된 아이템이 같음 - 상호작용 UI 표시 안함");
                interactorText.text = "";
                return;
            }
        }

        _currentItem = item;
        _currentFurniture = null;
        _currentEquipment = null;

        _canPickUp = true;
        _canInteractable = false;

        interactorText.text = $"{_currentItem.Item.DisplayName} [F]";
    }

    /// <summary>
    /// 실험기구 감지 시 UI 표시
    /// </summary>
    /// <param name="equipment"></param>
    private void HandleLabEquipment(LabEquipment equipment)
    {
        _currentEquipment = equipment;
        _currentItem = null;
        _currentFurniture = null;

        _canPickUp = false;
        _canInteractable = false;

        // 실험기구 슬롯 UI 표시
        SampleSlotUIManager.Instance.ShowLabUI(equipment);
    }

    /// <summary>
    /// 가구 감지 시 UI 및 상태 처리
    /// </summary>
    private void HandleInteractable(IInteractable furniture)
    {
        _currentFurniture = furniture;
        _currentItem = null;
        _currentEquipment = null;

        _canInteractable = true;
        _canPickUp = false;

        interactorText.text = furniture.GetInteractText();
    }

    /// <summary>
    /// 감지된 오브젝트가 없을 때 UI 및 상태 초기화
    /// </summary>
    private void ClearDetection()
    {
        if (!_canPickUp && !_canInteractable) return;

        interactorText.text = "";

        _currentItem = null;
        _currentEquipment = null;
        _currentFurniture = null;

        _canPickUp = false;
        _canInteractable = false;
    }

    public void ClearDetectionText()
    {
        interactorText.text = "";
    }
    #endregion

    /// <summary>
    /// 현재 감지된 아이템을 인벤토리에 추가하고 오브젝트 제거
    /// </summary>
    private void TryPickUpItem()
    {
        IStatableItem statableItem = _currentItem.GetComponent<IStatableItem>();
        if (_inventoryManager.AddItemToInventory(_currentItem.Item, 1, statableItem))
        {
            WarehouseManager.instance.ReportDestroyed(_currentItem.gameObject);
            Debug.Log(_currentItem.Item.ItemName + " 획득");
            Destroy(_currentItem.gameObject);
        }
        else
        {
            Debug.Log("인벤토리 꽉참");
        }

        ClearDetection();
    }
}
