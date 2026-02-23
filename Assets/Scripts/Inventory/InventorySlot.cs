using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary> 인벤토리 슬롯 하나를 관리하고 UI 제어
/// <para> - 슬롯에 아이템 객체와 개수를 저장하고 접근을 제공 </para>
/// <para> - 슬롯에 아이템 이미지&개수 업데이트, 슬롯 교체, 슬롯 초기화 기능을 제어 </para>
/// </summary>
public class InventorySlot : MonoBehaviour
{
    private Item _item; // 슬롯에 들어있는 아이템
    public Item Item => _item;

    private int _itemCount; // 슬롯에 들어있는 아이템 개수
    public int ItemCount => _itemCount;

    [Header("아이템 슬롯에 있는 UI 오브젝트")]
    [SerializeField] private Image itemImage; // 아이템 이미지 UI
    [SerializeField] private TMPro.TextMeshProUGUI itemCountText; // 아이템 개수 텍스트 UI

    /// <summary>
    /// 아이템과 개수를 해당 슬롯에 추가
    /// </summary>
    public void AddItem(Item newItem, int count)
    {
        _item = newItem;
        _itemCount = count;

        UpdateSlotUI();
    }

    /// <summary>
    /// 아이템의 개수만 업데이트
    /// <para> - 중첩 가능한 아이템을 추가하는 경우 개수 업데이트 </para>
    /// <para> - 아이템 개수가 0개 이하가 된 경우 슬롯 초기화 </para>
    /// </summary>
    public void UpdateItemCount(int count)
    {
        _itemCount += count;
        if (_itemCount <= 0)
        {
            ClearSlot();
        }
        else
        {
            itemCountText.text = _itemCount.ToString();
        }
    }

    /// <summary>
    /// 선택된 슬롯과 교체를 원하는 슬롯 간의 아이템 슬롯 업데이트
    /// </summary>
    public void SetSlot(Item newItem, int count)
    {
        _item = newItem;
        _itemCount = count;

        if (_item != null)
        {
            UpdateSlotUI();
        }
        else
        {
            ClearSlot();
        }

    }

    /// <summary>
    /// 슬롯 데이터 및 UI 업데이트
    /// </summary>
    public void ClearSlot()
    {
        _item = null;
        _itemCount = 0;

        itemImage.sprite = null;
        SetColor(0f);
        itemCountText.text = "";
    }

    /// <summary>
    /// 슬롯 데이터를 기반으로 슬롯 이미지와 개수 텍스트 UI 업데이트
    /// <para> - 해당 슬롯에 아이템이 없다면 UI 초기화 </para>
    /// </summary>
    private void UpdateSlotUI()
    {
        itemImage.sprite = _item.ItemImage;
        SetColor(1f);

        if (_item.CanOverlap)
        {
            itemCountText.text = _itemCount.ToString();
        }
        else
        {
            itemCountText.text = "";
        }
    }

    /// <summary>
    /// 아이템 이미지의 투명도 설정
    /// </summary>
    private void SetColor(float alpha)
    {
        var color = itemImage.color;
        color.a = alpha;
        itemImage.color = color;
    }
}
