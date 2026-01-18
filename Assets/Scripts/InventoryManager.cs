using System.Collections;
using System.Collections.Generic;
using InnerMonsterStates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary> 인벤토리 관리자
/// <para> - 플레이어 인벤토리 전체를 관리 </para> 
/// <para> - 인벤토리 UI 표시, 슬롯 선택, 아이템 추가/교체/사용/버리기/인벤토리에 반환 등 인벤토리의 모든 기능을 제어 </para>
/// <para> - 모든 인벤토리 관련 입력은 PlayerAction(Input System)의 'Player' 액션 맵에서 처리 </para>
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [SerializeField] private GameObject inventoryUI; // 인벤토리 전체 UI 오브젝트
    [SerializeField] private GameObject inventorySlotsParent; // 인벤토리 슬롯 부모 오브젝트
    [SerializeField] private Sprite slotSprite; // 슬롯 기본 스프라이트
    [SerializeField] private Sprite selectedSlotSprite; // 슬롯 선택 스프라이트
    [SerializeField] private Transform rightHandTransform; // 플레이어 오른손 위치 (아이템 버리기 위치 계산에 사용)

    private InventorySlot[] _inventorySlots; // 인벤토리 슬롯 배열
    public InventorySlot[] InventorySlots => _inventorySlots;
    private int _selectedSlotIndex = -1; // 선택된 슬롯 인덱스
    public int SelectedSlotIndex => _selectedSlotIndex;
    private static bool _isInventoryOpen = false; // 인벤토리 활성화 상태
    public bool IsInventoryOpen => _isInventoryOpen;
    private bool _isSwapMode = false; // T키 눌림 상태
    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러
    private int _heldItemSlotIndex = -1; // 손에 들고 있는 아이템의 슬롯 인덱스
    public int HeldItemSlotIndex => _heldItemSlotIndex;
    private CrewRoom1KeyPad _crewRoom1KeyPad;
    private CrewRoom1KeyPadPanel _crewRoom1KeyPadPanel;

    /// <summary>
    /// 인벤토리 UI를 초기화하고 슬롯 배열을 구성
    /// </summary>
    void Awake()
    {
        if (inventoryUI.activeSelf)
        {
            inventoryUI.SetActive(false);
        }

        _inventorySlots = inventorySlotsParent.GetComponentsInChildren<InventorySlot>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _crewRoom1KeyPadPanel = FindObjectOfType<CrewRoom1KeyPadPanel>();
        _crewRoom1KeyPad = FindObjectOfType<CrewRoom1KeyPad>();
    }

    /// <summary>
    /// I키 입력으로 인벤토리를 활성화/비활성화
    /// </summary>
    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (!_isInventoryOpen)
        {
            OpenInventory();
        }
        else
        {
            CloseInventory();
        }
    }

    /// <summary>
    /// 인벤토리 활성화
    /// </summary>
    private void OpenInventory()
    {
        inventoryUI.SetActive(true);
        _isInventoryOpen = true;
    }

    /// <summary>
    /// 인벤토리 비활성화
    /// </summary>
    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        _isInventoryOpen = false;
        SelectSlot(-1);
    }

    /// <summary>
    /// T키 눌림 상태 업데이트 (슬롯 교체 기능에 사용)
    /// </summary>
    public void OnTHold(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isSwapMode = true;
        }
        else if (context.canceled)
        {
            _isSwapMode = false;
        }
    }

    /// <summary>
    /// 숫자키(1~6) 입력으로 인벤토리 슬롯 선택 또는 슬롯 교체
    /// <para> - T키가 눌린 상태라면 선택된 슬롯과 교체 </para>
    /// <para> - T키가 눌리지 않은 상태라면 해당 슬롯 선택 </para>
    /// </summary>
    public void OnSlotKeyPress(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!_isInventoryOpen) return;

        int slotIndex = context.control.name[0] - '1';

        if (_selectedSlotIndex == slotIndex) return;
        if (_isSwapMode && _selectedSlotIndex >= 0)
        {
            SwapSlots(_selectedSlotIndex, slotIndex);
        }
        else
        {
            SelectSlot(slotIndex);
        }
    }

    /// <summary>
    /// 선택된 슬롯은 네온 스프라이트로 변경
    /// </summary>
    private void SelectSlot(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;

        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            var slotImage = _inventorySlots[i].GetComponent<UnityEngine.UI.Image>();
            if (i == _selectedSlotIndex)
            {
                slotImage.sprite = selectedSlotSprite;
            }
            else
            {
                slotImage.sprite = slotSprite;
            }
        }
    }

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템과 개수를 서로 교환
    /// </summary>
    private void SwapSlots(int selectedSlotIndex, int swapSlotIndex)
    {
        Debug.Log($"슬롯 {selectedSlotIndex + 1} 과 슬롯 {swapSlotIndex + 1} 교체");
        var selectedSlot = _inventorySlots[selectedSlotIndex];
        var swapSlot = _inventorySlots[swapSlotIndex];

        var tempItem = selectedSlot.Item;
        var tempCount = selectedSlot.ItemCount;

        selectedSlot.SetSlot(swapSlot.Item, swapSlot.ItemCount);
        swapSlot.SetSlot(tempItem, tempCount);
    }

    /// <summary> 인벤토리에 아이템 추가
    /// <para> - 중첩 가능한 아이템인 경우 기존 슬롯에 개수만 업데이트 </para>
    /// <para> - 중첩 불가능한 아이템이거나 기존에 없는 아이템인 경우 빈 슬롯에 새로 추가 </para>
    /// <para> - 빈 슬롯이 없으면 아이템 추가 실패 </para>
    /// </summary>
    /// <returns>인벤토리에 아이템 추가 성공 여부</returns>
    public bool AddItemToInventory(Item newItem, int count = 1)
    {
        if (newItem.CanOverlap)
        {
            foreach (var slot in _inventorySlots)
            {
                if (slot.Item != null && slot.Item.ItemName == newItem.ItemName)
                {
                    slot.UpdateItemCount(slot.ItemCount + count);
                    return true;
                }
            }
        }

        foreach (var slot in _inventorySlots)
        {
            if (slot.Item == null)
            {
                slot.AddItem(newItem, count);
                return true;
            }
        }

        Debug.Log("인벤토리가 가득 찼습니다.");
        return false;
    }

    /// <summary> 1회용 아이템 소비
    /// <para> - 아이템 개수 1개 감소 </para>
    /// <para> - 아이템 장착 해제 </para>
    public void ConsumeItemInSlot(Item item)
    {
        if (item == null) return;

        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            if (_inventorySlots[i].Item == item)
            {
                var targetSlot = _inventorySlots[i];
                targetSlot.UpdateItemCount(targetSlot.ItemCount - 1);
                if (_heldItemSlotIndex == i)
                {
                    _itemEquipController.UnequipItem();
                    _heldItemSlotIndex = -1;
                }
                return;
            }
        }
    }

    /// <summary>
    /// E키 입력으로 아이템 사용/장착
    /// </summary>
    public void OnItemUse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!_isInventoryOpen || _selectedSlotIndex < 0) return;

        var selectedSlot = _inventorySlots[_selectedSlotIndex];
        if (selectedSlot.Item == null || selectedSlot.Item.ItemPrefab == null) return;
        if ((_crewRoom1KeyPad != null && _crewRoom1KeyPad.IsFocused) || (_crewRoom1KeyPadPanel != null && _crewRoom1KeyPadPanel.IsFocused))
        {

            if (selectedSlot.Item.ItemName == "Flashlight") return;
        }

        if (selectedSlot.Item.ItemName == "Map")
        {
            FindAnyObjectByType<MapViewController>().UnlockMap(); // 맵 잠금 해제
            ConsumeItemInSlot(selectedSlot.Item);
            Debug.Log("이제 지도를 Tab키로 열고 닫을 수 있습니다.");
            return;
        }

        _heldItemSlotIndex = _selectedSlotIndex;
        _itemEquipController.EquipItem(selectedSlot.Item);
        CloseInventory();
    }

    /// <summary>
    /// Q키로 선택된 슬롯의 아이템 1개 버리기
    /// <para> - 아이템 프리팹을 현재 플레이어 앞에 생성하고 아이템 개수 1개 감소 </para>
    /// <para> - 손에 들고 있는 상태라면 아이템 오브젝트도 제거 </para>
    /// </summary>
    public void OnItemDrop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (SubmarineInGameManager.instance.IsPausing) return;

        InventorySlot targetSlot = null;

        if (IsInventoryOpen)
        {
            if (_selectedSlotIndex < 0) return;
            targetSlot = _inventorySlots[_selectedSlotIndex];
        }
        else
        {
            if (_heldItemSlotIndex < 0) return;
            targetSlot = _inventorySlots[_heldItemSlotIndex];
        }

        if (targetSlot == null || targetSlot.Item == null) return;

        Vector3 dropPosition = rightHandTransform.position + rightHandTransform.forward * 0.5f;
        GameObject droppedItemObject = Instantiate(targetSlot.Item.ItemPrefab, dropPosition, Quaternion.identity);
        StartCoroutine(ApplyRigidbody(droppedItemObject, 2f));

        if ((!IsInventoryOpen && _itemEquipController.HasItem) || (IsInventoryOpen && _heldItemSlotIndex == _selectedSlotIndex))
        {
            _itemEquipController.UnequipItem();
            _heldItemSlotIndex = -1;
        }

        targetSlot.UpdateItemCount(targetSlot.ItemCount - 1);
    }

    /// <summary>
    /// 특정 시간 동안 오브젝트에 Rigidbody를 추가하여 중력을 적용한 후 제거
    /// </summary>
    private IEnumerator ApplyRigidbody(GameObject itemObject, float duration)
    {
        Rigidbody rb = itemObject.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDrag = 0.05f;
        rb.drag = 0f;
        rb.useGravity = true;

        yield return new WaitForSeconds(duration);

        if (rb != null)
        {
            Destroy(rb);
        }
    }

    /// <summary>
    /// R키로 아이템을 인벤토리 슬롯으로 반환 (장착 해제)
    /// <para> - 손에 들고 있는 아이템을 인벤토리에 추가 및 아이템 오브젝트 제거 </para>
    /// </summary>
    public void OnReturnToSlot(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!_itemEquipController.HasItem) return;

        _heldItemSlotIndex = -1;
        _itemEquipController.UnequipItem();
    }
}
