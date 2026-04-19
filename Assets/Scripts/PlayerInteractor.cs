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

    private InventoryManager _inventoryManager; // 인벤토리 매니저
    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러
    private ItemPickUp _currentItem; // 현재 감지된 아이템
    private IInteractable _currentFurniture; // 현재 감지된 가구
    private RaycastHit _sphereCastHit; // SphereCast로 감지된 오브젝트 정보
    private bool _canPickUp = false; // 아이템 줍기 가능 여부
    private bool _canInteractable = false; // 가구 상호작용 가능 여부
    public bool IsPuzzleActive = false; // 플레이어가 퍼즐을 풀고 있는 상태

    private int detectLayerMask; // 감지 레이어 (괴물 제외하기 위해 만듦)

    void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();

        detectLayerMask = ~LayerMask.GetMask("Monster"); // 괴물은 감지 레이어에서 제외
    }

    void Update()
    {
        if (!IsPuzzleActive) DetectObject();
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
    /// E키 입력으로 가구와 상호작용
    /// </summary>
    public void OnFurnitureInteractor(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!_canInteractable || _currentFurniture == null) return;

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

    /// <summary>
    /// 플레이어 앞에 있는 아이템 또는 가구 감지
    /// <para> - 아이템이 감지된 경우 아이템 줍기 UI 표시 </para>
    /// <para> - 가구가 감지된 경우 상호작용 UI 표시. 가구와 상호작용이 안 되는 아이템을 든 경우 UI 표시 안됨. </para>
    /// <para> - 아무것도 감지되지 않은 경우 UI 초기화 </para>
    /// </summary>
    private void DetectObject()
    {
        if (Physics.SphereCast(playerCamera.transform.position, 0.1f, playerCamera.transform.forward, out _sphereCastHit, rayDistance, detectLayerMask))
        {
            if (_sphereCastHit.transform.TryGetComponent(out ItemPickUp item))
            {
                HandleItem(item);
                return;
            }

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
                    return;
                }
            }
        }
        ClearDetection();
    }

    /// <summary>
    /// 아이템 감지 시 UI 및 상태 처리
    /// </summary>
    private void HandleItem(ItemPickUp item)
    {
        if (_itemEquipController.HasItem && _itemEquipController.HeldItemObject.TryGetComponent(out ItemPickUp heldItem))
        {
            if (heldItem == item)
            {
                Debug.Log("현재 장착된 아이템과 감지된 아이템이 같음 - 상호작용 UI 표시 안함");
                interactorText.text = "";
                return;
            }
        }

        _currentItem = item;
        _currentFurniture = null;

        _canPickUp = true;
        _canInteractable = false;

        interactorText.text = $"{_currentItem.Item.ItemName} [F]";
    }

    /// <summary>
    /// 가구 감지 시 UI 및 상태 처리
    /// </summary>
    private void HandleInteractable(IInteractable furniture)
    {
        _currentFurniture = furniture;
        _currentItem = null;

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
        _currentFurniture = null;

        _canPickUp = false;
        _canInteractable = false;
    }

    public void ClearDetectionText()
    {
        interactorText.text = "";
    }

    /// <summary>
    /// 현재 감지된 아이템을 인벤토리에 추가하고 오브젝트 제거
    /// </summary>
    private void TryPickUpItem()
    {
        IStatableItem statableItem = _currentItem.GetComponent<IStatableItem>();
        if (_inventoryManager.AddItemToInventory(_currentItem.Item, 1, statableItem))
        {
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
