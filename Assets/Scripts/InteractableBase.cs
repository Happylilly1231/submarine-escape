using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상호작용 가능한 오브젝트의 기본 클래스
/// <para>- IInteractable 인터페이스 추상화</para>
/// <para>- 상호작용 가능한 오브젝트들의 공통 로직 관리(아이템 필요 여부 검사)</para>
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    protected InventoryManager _inventoryManager;
    protected ItemEquipController _itemEquipController;

    protected virtual void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
    }

    // IInteractable 인터페이스
    public abstract string GetInteractText();
    public abstract void Interact();
    public abstract bool CanInteractwithSelectedItem(Item item);

    /// <summary>
    /// 상호작용에 필요한 아이템이 선택되어있는지 여부 반환하는 함수
    /// </summary>
    /// <returns></returns>
    protected bool IsRequiredItemSelected()
    {
        Item selectedItem = null;
        if (_itemEquipController.HeldItemData != null)
        {
            selectedItem = _itemEquipController.HeldItemData;
        }
        else if (_inventoryManager.SelectedSlotIndex >= 0 && _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex] != null)
        {
            selectedItem = _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item;
        }

        return CanInteractwithSelectedItem(selectedItem);
    }
}
