using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 슬롯 하나를 관리
/// <para> - 슬롯에 아이템 추가 및 개수 업데이트 </para>
/// <para> - 슬롯 초기화 </para>
/// </summary>
public class InventorySlot : MonoBehaviour
{
    private Item mItem; // 슬롯에 들어있는 아이템
    public Item item => mItem;

    private int mItemCount; // 슬롯에 들어있는 아이템 개수
    public int itemCount => mItemCount;

    [Header("아이템 슬롯에 있는 UI 오브젝트")]
    [SerializeField] private Image mItemImage; // 아이템 이미지 UI
    [SerializeField] private TMPro.TextMeshProUGUI mItemCountText; // 아이템 개수 텍스트 UI

    /// <summary>
    /// 아이템과 개수를 해당 슬롯에 추가
    /// </summary>
    public void AddItem(Item newItem, int count)
    {
        mItem = newItem;
        mItemCount = count;
        mItemImage.sprite = mItem.itemImage;

        // 아이템 이미지 보이도록 설정
        var color = mItemImage.color;
        color.a = 1f;
        mItemImage.color = color;

        // 아이템이 중첩 가능한 경우에만 아이템 개수 표시
        if (mItem.canOverlap)
        {
            mItemCountText.text = mItemCount.ToString();
        }
        else
        {
            mItemCountText.text = "";
        }
    }

    /// <summary>
    /// 중첩 가능한 아이템을 또 획득했다면 새로운 슬롯에 추가하지 않고 개수만 업데이트
    /// </summary>
    public void UpdateItemCount(int newCount)
    {
        mItemCount = newCount;
        mItemCountText.text = mItemCount.ToString();
        if (mItemCount <= 0)
        {
            ClearSlot();
        }
    }

    /// <summary>
    /// 선택된 슬롯과 교체를 원하는 슬롯 간의 아이템 슬롯 업데이트
    /// </summary>
    public void SetSlot(Item newItem, int count)
    {
        mItem = newItem;
        mItemCount = count;

        if (mItem != null)
        {
            mItemImage.sprite = mItem.itemImage;

            // 아이템이 중첩 가능한 경우에만 아이템 개수 표시
            if (mItem.canOverlap)
            {
                mItemCountText.text = mItemCount.ToString();
            }
            else
            {
                mItemCountText.text = "";
            }
        }
        else
        {
            mItemImage.sprite = null;
            mItemCount = 0;
            mItemCountText.text = "";
        }

    }

    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    public void ClearSlot()
    {
        mItem = null;
        mItemCount = 0;
        mItemImage.sprite = null;

        // 아이템 이미지 안보이도록 설정
        var color = mItemImage.color;
        color.a = 0f;
        mItemImage.color = color;

        mItemCountText.text = "";
    }
}
