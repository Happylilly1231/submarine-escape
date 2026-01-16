using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ItemEquipController : MonoBehaviour
{
    [SerializeField] private Transform itemViewRoot; // 아이템을 들고 있는 위치
    [SerializeField] private TwoBoneIKConstraint rightHandIK; // 오른손 IK
    private GameObject _heldItemObject; // 현재 장착된 아이템 오브젝트
    private Item _heldItemData; // 현재 장착된 아이템 데이터

    public bool HasItem => _heldItemObject != null;
    public Item HeldItemData => _heldItemData;
    public GameObject HeldItemObject => _heldItemObject;

    /// <summary>
    /// 아이템 장착
    /// </summary>
    public void EquipItem(Item item)
    {
        if (item == null || item.ItemPrefab == null) return;
        UnequipItem();

        _heldItemData = item;
        _heldItemObject = Instantiate(item.ItemPrefab, itemViewRoot);

        _heldItemObject.transform.localPosition = item.GripPositionOffset;
        _heldItemObject.transform.localRotation = Quaternion.Euler(item.GripRotationOffset);

        SetLayerRecursive(_heldItemObject, LayerMask.NameToLayer("HeldItem"));

        foreach (var col in _heldItemObject.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = false;
        }

        _heldItemObject.transform.SetParent(itemViewRoot);

        rightHandIK.weight = 1f;
    }

    /// <summary>
    /// 장착할 아이템의 모든 자식들까지 레이어 변경
    /// </summary>
    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// 아이템 장착 해제
    /// </summary>
    public void UnequipItem()
    {
        if (_heldItemObject != null)
        {
            Destroy(_heldItemObject);
            _heldItemObject = null;
            _heldItemData = null;
        }

        rightHandIK.weight = 0f;
    }
}
