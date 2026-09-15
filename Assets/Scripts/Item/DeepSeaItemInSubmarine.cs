using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepSeaItemInSubmarine : MonoBehaviour
{
    private ItemPickUp _itemPickUp;
    private ItemEquipController _itemEquipController;
    private bool _isWetsuitEquipped = false; // 잠수복 착용 여부

    private void Awake()
    {
        _itemPickUp = GetComponent<ItemPickUp>();
        _itemEquipController = GetComponent<ItemEquipController>();
    }

    public void Use(Item item)
    {
        Debug.Log($"Consumed item: {item.ItemName}");

        switch (item.ItemName)
        {
            case "DivingSuit":
                _isWetsuitEquipped = !_isWetsuitEquipped;
                if (_isWetsuitEquipped)
                {
                    transform.localPosition = new Vector3(0, -0.21f, 0);
                    transform.localRotation = Quaternion.Euler(0, 90, 0);
                }
                else
                {
                    _itemEquipController?.UnequipItem();

                    if (_itemPickUp?.Item != null)
                    {
                        transform.localPosition = _itemPickUp.Item.GripPositionOffset;
                        transform.localRotation = Quaternion.Euler(_itemPickUp.Item.GripRotationOffset);
                    }
                }
                Debug.Log(_isWetsuitEquipped ? "잠수복 착용" : "잠수복 벗음");
                break;
        }
    }
}
