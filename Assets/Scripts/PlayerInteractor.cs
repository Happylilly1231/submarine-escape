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
    [SerializeField] private GameObject interactorUI; // 상호작용 UI
    [SerializeField] private TMPro.TextMeshProUGUI interactorText; // 상호작용 텍스트

    private InventoryManager _inventoryManager; // 인벤토리 매니저
    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러
    private FadeUI _fadeUI; // 페이드 UI
    private ItemPickUp _currentItem; // 현재 감지된 아이템
    private IInteractable _currentFurniture; // 현재 감지된 가구
    private RaycastHit _sphereCastHit; // SphereCast로 감지된 오브젝트 정보
    private bool _canPickUp = false; // 아이템 줍기 가능 여부
    private bool _canInteractable = false; // 가구 상호작용 가능 여부
    public bool IsPuzzleActive = false; // 플레이어가 퍼즐을 풀고 있는 상태

    void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _fadeUI = interactorUI.GetComponent<FadeUI>();

        interactorUI.SetActive(true);
    }

    void Start()
    {
        _fadeUI.CanvasGroup.alpha = 0f;
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

        List<Item> candidateItems = new List<Item>();
        if (_itemEquipController.HeldItemData != null)
        {
            if (_itemEquipController.HeldItemData.ItemName == "KeyPad Manual") return;
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

        Item validItem = null;
        bool canInteract = false;

        if (candidateItems.Count > 0)
        {
            foreach (Item item in candidateItems)
            {
                if (_currentFurniture.CanInteractwithSelectedItem(item))
                {
                    validItem = item;
                    canInteract = true;
                    break;
                }
            }
        }

        // 선택된 아이템이 없거나 선택된 아이템으로 상호작용이 가능한 경우에만 상호작용 실행
        if (_inventoryManager.SelectedSlotIndex < 0 || _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item == null || canInteract)
        {
            _currentFurniture.Interact();
            if (canInteract && validItem.IsConsumable)
            {
                _inventoryManager.ConsumeItemInSlot(validItem);
            }
        }
    }

    /// <summary>
    /// 플레이어 앞에 있는 아이템 또는 가구 감지
    /// </summary>
    private void DetectObject()
    {
        if (Physics.SphereCast(playerCamera.transform.position, 0.1f, playerCamera.transform.forward, out _sphereCastHit, rayDistance))
        {
            if (_sphereCastHit.transform.TryGetComponent(out ItemPickUp item))
            {
                HandleItem(item);
                return;
            }

            if (_sphereCastHit.transform.TryGetComponent(out IInteractable furniture))
            {
                List<Item> candidateItems = new List<Item>();
                if (_itemEquipController.HeldItemData != null)
                {
                    if (_itemEquipController.HeldItemData.ItemName == "KeyPad Manual") return;
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
                    foreach (Item selectedItem in candidateItems)
                    {
                        if (furniture.CanInteractwithSelectedItem(selectedItem))
                        {
                            canInteract = true;
                            break;
                        }
                    }
                }

                // 인벤토리가 닫혀 있거나 선택된 아이템이 없거나 선택된 아이템으로 상호작용이 가능한 경우에만 상호작용 UI 표시
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
                return;
        }

        _currentItem = item;
        _currentFurniture = null;

        _canPickUp = true;
        _canInteractable = false;

        interactorText.text = $"{_currentItem.Item.ItemName} [F]";

        _fadeUI.FadeIn();
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

        _fadeUI.FadeIn();
    }

    /// <summary>
    /// 감지된 오브젝트가 없을 때 UI 및 상태 초기화
    /// </summary>
    private void ClearDetection()
    {
        if (!_canPickUp && !_canInteractable) return;

        _fadeUI.FadeOut();

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
        if (_inventoryManager.AddItemToInventory(_currentItem.Item))
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
