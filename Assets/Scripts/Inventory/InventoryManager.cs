using System.Collections;
using System.Collections.Generic;
using InnerMonsterStates;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Text;
using System;

/// <summary> 인벤토리 관리자
/// <para> - 플레이어 인벤토리 전체를 관리 </para> 
/// <para> - 인벤토리 UI 표시, 슬롯 선택, 아이템 추가/교체/사용/버리기/인벤토리에 반환 등 인벤토리의 모든 기능을 제어 </para>
/// <para> - 모든 인벤토리 관련 입력은 PlayerAction(Input System)의 'Player' 액션 맵에서 처리 </para>
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("인벤토리 UI 및 슬롯")]
    [SerializeField] private GameObject inventoryUI; // 인벤토리 전체 UI 오브젝트
    [SerializeField] private InventorySlot[] inventorySlots; // 인벤토리 슬롯 배열
    [SerializeField] private TMPro.TextMeshProUGUI actionText; // 상호작용 UI - 아이템 사용, 버리기, 맵 열기 키 표시
    [Header("인벤토리 슬롯 스프라이트")]
    [SerializeField] private Sprite slotSprite; // 슬롯 기본 스프라이트
    [SerializeField] private Sprite selectedSlotSprite; // 슬롯 선택 스프라이트
    [Header("플레이어 오른손 위치")]
    [SerializeField] private Transform rightHandTransform; // 플레이어 오른손 위치 (아이템 버리기 위치 계산에 사용)

    public InventorySlot[] InventorySlots => inventorySlots;
    private int _selectedSlotIndex = -1; // 선택된 슬롯 인덱스
    public int SelectedSlotIndex => _selectedSlotIndex;
    private bool _isSwapMode = false; // T키 눌림 상태
    private bool _hasRegisteredMap = false; // 지도 아이템 사용 여부
    private bool _isViewingUI = false; // UI 아이템 사용으로 UI를 보고 있는 상태 여부
    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러
    private StatableItemManager _statableItemManager; // 개별 상태를 가지는 아이템 관리하는 매니저

    /// <summary>
    /// 인벤토리 UI를 초기화하고 슬롯 배열을 구성
    /// </summary>
    void Awake()
    {
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _statableItemManager = GetComponent<StatableItemManager>();
    }

    /// <summary>
    /// 인벤토리 비활성화
    /// </summary>
    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        SelectSlot(-1);
    }

    /// <summary>
    /// 인벤토리 활성화
    /// </summary>
    public void OpenInventory()
    {
        inventoryUI.SetActive(true);
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
    /// 숫자키(1~6) 입력으로 인벤토리 슬롯 선택해서 아이템 들기 / 아이템 회수 / 슬롯 교체
    /// <para> - T키가 눌린 상태라면 선택된 슬롯과 교체 </para>
    /// <para> - T키가 눌리지 않은 상태라면 해당 슬롯 선택 후 아이템 들기 </para>
    /// <para> - 아이템을 든 상태에서 같은 슬롯 번호를 누르면 아이템 회수 </para>
    /// </summary>
    public void OnSlotKeyPress(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int slotIndex = context.control.name[0] - '1';

        if (_isSwapMode && _selectedSlotIndex >= 0) // 슬롯 교체
        {
            SwapSlots(_selectedSlotIndex, slotIndex);
        }
        else if (!_isSwapMode && _selectedSlotIndex != slotIndex) // 슬롯 선택 및 선택된 슬롯에 있는 아이템 들기
        {
            SelectSlot(slotIndex);
            _itemEquipController.EquipItem(inventorySlots[slotIndex].Item, inventorySlots[slotIndex].ItemInstanceNum);
        }
        else if (!_isSwapMode && _selectedSlotIndex == slotIndex) // 아이템 회수
        {
            SelectSlot(-1);
            _itemEquipController.UnequipItem();
        }
    }

    /// <summary>
    /// 선택된 슬롯의 테두리를 네온 스프라이트로 변경
    /// </summary>
    private void SelectSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventorySlots.Length) inventorySlots[slotIndex].GetComponent<Image>().sprite = selectedSlotSprite;
        if (_selectedSlotIndex >= 0) inventorySlots[_selectedSlotIndex].GetComponent<Image>().sprite = slotSprite;
        _selectedSlotIndex = slotIndex;
        _isViewingUI = false;
        UpdateActionText();
    }

    private void UpdateActionText()
    {
        StringBuilder sb = new StringBuilder();
        Item currentItem = _selectedSlotIndex >= 0 ? inventorySlots[_selectedSlotIndex].Item : null;

        if (currentItem != null)
        {
            sb.AppendLine("Drop [Q]");

            switch (currentItem.ItemType)
            {
                case EItemType.Toggle:
                    sb.AppendLine("On/Off [Mouse LMB]");
                    break;
                case EItemType.Consumable:
                    sb.AppendLine("Use [E]");
                    break;
                case EItemType.UI:
                    if (currentItem.ItemName == "Map")
                    {
                        sb.AppendLine("Register Map[E]");
                    }
                    else if (_isViewingUI)
                    {
                        sb.AppendLine("Close [E]");
                    }
                    else
                    {
                        sb.AppendLine("View [E]");
                    }
                    break;
                case EItemType.Wearable:
                    sb.AppendLine("Wear [E]");
                    break;
            }
        }

        if (_hasRegisteredMap) sb.AppendLine("View Map [Tab]");

        actionText.text = sb.ToString();
    }

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템과 개수를 서로 교환
    /// </summary>
    private void SwapSlots(int selectedSlotIndex, int swapSlotIndex)
    {
        Debug.Log($"슬롯 {selectedSlotIndex + 1} 과 슬롯 {swapSlotIndex + 1} 교체");
        var selectedSlot = inventorySlots[selectedSlotIndex];
        var swapSlot = inventorySlots[swapSlotIndex];

        var tempItem = selectedSlot.Item;
        var tempCount = selectedSlot.ItemCount;
        var tempItemInstanceNum = selectedSlot.ItemInstanceNum;

        selectedSlot.SetSlot(swapSlot.Item, swapSlot.ItemCount, swapSlot.ItemInstanceNum);
        swapSlot.SetSlot(tempItem, tempCount, tempItemInstanceNum);
    }

    /// <summary> 인벤토리에 아이템 추가
    /// <para> - 중첩 가능한 아이템인 경우 기존 슬롯에 개수만 업데이트 </para>
    /// <para> - 중첩 불가능한 아이템이거나 기존에 없는 아이템인 경우 빈 슬롯에 새로 추가 </para>
    /// <para> - 빈 슬롯이 없으면 아이템 추가 실패 </para>
    /// </summary>
    /// <returns>인벤토리에 아이템 추가 성공 여부</returns>
    public bool AddItemToInventory(Item newItem, int count = 1, IStatableItem statableItem = null)
    {
        if (newItem.CanOverlap)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot.Item != null && slot.Item.ItemName == newItem.ItemName)
                {
                    slot.UpdateItemCount(count);
                    return true;
                }
            }
        }

        // 개별 상태 저장 아이템의 경우
        if (statableItem != null)
        {
            // 해당 아이템이 아직 개별 상태 저장 딕셔너리에 올라가 있지 않다면 -> 딕셔너리에 등록
            if (statableItem.ItemInstanceNum == 0)
            {
                // 개별 상태 저장 딕셔너리에 현재 아이템 인스턴스 상태 데이터 등록
                _statableItemManager.RegisterItemState(statableItem);
            }
        }

        // 선택된 슬롯이 비어있는 경우 해당 슬롯에 추가 및 아이템 들기
        if (_selectedSlotIndex >= 0 && inventorySlots[_selectedSlotIndex].Item == null)
        {
            if (statableItem != null)
            {
                inventorySlots[_selectedSlotIndex].AddItem(newItem, count, statableItem.ItemInstanceNum); // 개별 상태 저장 아이템을 위해 슬롯에 현재 아이템 인스턴스 번호도 저장(개별 상태 저장 아이템이 아니면 자동으로 0)
            }
            else
            {
                inventorySlots[_selectedSlotIndex].AddItem(newItem, count);
            }
            _itemEquipController.EquipItem(inventorySlots[_selectedSlotIndex].Item, inventorySlots[_selectedSlotIndex].ItemInstanceNum);
            UpdateActionText();
            return true;
        }

        // 선택된 슬롯이 없는 경우 앞 슬롯에 추가
        foreach (var slot in inventorySlots)
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
    /// <para> - 인벤토리에서 아이템 개수 1개 감소 </para>
    /// <para> - 아이템 장착 해제 </para>
    public void ConsumeItemInSlot(Item item)
    {
        if (item == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].Item == item)
            {
                var targetSlot = inventorySlots[i];
                targetSlot.UpdateItemCount(-1);
                _itemEquipController.UnequipItem();
                return;
            }
        }
        UpdateActionText();
    }

    /// <summary>
    /// E키 입력으로 아이템 사용
    /// <para> - 손전등은 마우스 좌클릭으로 사용 </para>
    /// </summary>
    public void OnItemUse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_selectedSlotIndex < 0) return;

        var selectedSlot = inventorySlots[_selectedSlotIndex];
        if (selectedSlot.Item == null || selectedSlot.Item.ItemPrefab == null) return;

        GameObject currentEquippedItem = _itemEquipController.HeldItemObject;

        switch (selectedSlot.Item.ItemType)
        {
            case EItemType.Consumable:
                if (currentEquippedItem.TryGetComponent<ConsumableItem>(out var consumable))
                {
                    if (consumable.Use(selectedSlot.Item))
                    {
                        // 소비 아이템 사용에 성공한 경우에만 인벤토리에서 개수 감소 및 장착 해제
                        ConsumeItemInSlot(selectedSlot.Item);
                    }
                }
                // if (FindAnyObjectByType<ConsumableItem>().Use(selectedSlot.Item))
                // {
                //     // 소비 아이템은 사용 시 바로 소비
                //     ConsumeItemInSlot(selectedSlot.Item);
                // }
                break;
            case EItemType.UI:
                if (currentEquippedItem.TryGetComponent<UIItem>(out var uiItem))
                {
                    uiItem.Use(selectedSlot.Item, _isViewingUI);
                }
                if (selectedSlot.Item.ItemName == "Map")
                {
                    _hasRegisteredMap = true;
                    // UI 아이템 중 지도는 사용 시 바로 소비
                    ConsumeItemInSlot(selectedSlot.Item);
                }
                else
                {
                    _isViewingUI = !_isViewingUI;
                }
                break;
            case EItemType.Wearable:
                //FindAnyObjectByType<WearableItem>()?.Use(selectedSlot.Item);
                break;
        }

        UpdateActionText();
    }

    /// <summary>
    /// 마우스 좌클릭으로 손전등 사용
    /// </summary>
    public void OnToggleItemUse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_selectedSlotIndex < 0) return;

        var selectedSlot = inventorySlots[_selectedSlotIndex];
        if (selectedSlot.Item == null || selectedSlot.Item.ItemPrefab == null) return;

        if (selectedSlot.Item.ItemName == "Flashlight") FindAnyObjectByType<Flashlight>()?.Use();
        else if (selectedSlot.Item.ItemName == "Therometer") FindAnyObjectByType<Therometer>()?.Use();
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

        if (_selectedSlotIndex < 0) return;

        InventorySlot targetSlot = inventorySlots[_selectedSlotIndex];

        if (targetSlot == null || targetSlot.Item == null) return;

        Debug.Log($"슬롯 {Array.IndexOf(inventorySlots, targetSlot) + 1} 의 아이템 버리기: {targetSlot.Item.ItemName}");

        Vector3 dropPosition = rightHandTransform.position + rightHandTransform.forward * 0.5f;
        GameObject droppedItemObject = Instantiate(targetSlot.Item.ItemPrefab, dropPosition, Quaternion.identity);
        StartCoroutine(ApplyRigidbody(droppedItemObject, 2f));

        _statableItemManager.RestoreItemState(droppedItemObject, targetSlot.ItemInstanceNum); // 해당 아이템 오브젝트(인스턴스)의 저장된 상태가 있다면, 저장된 상태로 복원

        if (_itemEquipController.HasItem)
        {
            _itemEquipController.UnequipItem();
        }

        targetSlot.UpdateItemCount(-1);
        UpdateActionText();
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
}
