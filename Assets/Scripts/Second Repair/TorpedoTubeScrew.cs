using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TorpedoTubeScrew : MonoBehaviour, IInteractable
{
    [SerializeField] private Item driverItem; // 드라이버 아이템
    [SerializeField] private TorpedoTube torpedoTube; // 어뢰 발사관

    private bool _isTightened = false;
    private float _duration = 2f;

    private ItemEquipController _itemEquipController;
    private InventoryManager _inventoryManager;

    void Start()
    {
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _inventoryManager = FindObjectOfType<InventoryManager>();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        if (_isTightened) return false; // 조여졌으면 -> 상호작용 X
        return item == driverItem; // 조이려면 -> 드라이버 아이템 필요
    }

    public string GetInteractText()
    {
        if (_isTightened) // 조여졌으면 -> 상호작용 X
            return "";

        // 조이지 않았으면 - 드라이버가 선택되어있을 때 -> 나사 조이기 / 선택 안됨 -> 드라이버 필요 메시지
        return IsDriverSelected() ? "Tighten Screw [E]" : "Driver Required";
    }

    public void Interact()
    {
        if (_isTightened) // 조여졌으면 -> 상호작용 X
            return;

        // 드라이버가 선택되어있을 때 -> 나사 조이기
        if (IsDriverSelected())
        {
            TightenScrew();
        }
    }

    /// <summary>
    /// 드라이버 선택되었는지 여부
    /// </summary>
    private bool IsDriverSelected()
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

    /// <summary>
    /// 나사 조이기
    /// </summary>
    private void TightenScrew()
    {
        // 앞으로 이동
        transform.DOLocalMoveX(-0.62f, _duration);

        // 회전
        transform.DOLocalRotate(new Vector3(0, 0, -360f), _duration, RotateMode.LocalAxisAdd)
        .OnComplete(() =>
        {
            _isTightened = true;
            torpedoTube.IsNormal = true;
        });
    }
}
