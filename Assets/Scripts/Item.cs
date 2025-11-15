using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EItemType
{
    Survival, // 생존형
    Equipment, // 장비형
    Puzzle // 퍼즐형
}

[CreateAssetMenu(fileName = "New Item", menuName = "New Item/Item")]
public class Item : ScriptableObject
{
    [Header("아이템 기본 정보")]
    [SerializeField] private int mItemID; // 아이템의 고유 ID
    [SerializeField] private string mItemName; // 아이템의 이름
    [SerializeField] private EItemType mItemType; // 아이템의 유형
    [SerializeField] private Sprite mItemImage; // 아이템의 이미지
    [SerializeField] private GameObject mItemPrefab; // 아이템의 프리팹
    [SerializeField] private bool mCanOverlap; // 아이템 중첩 가능 여부
    [SerializeField] private bool mIsConsumable; // 아이템 1회용 여부

    // 읽기 전용 접근
    public int itemID => mItemID;
    public string itemName => mItemName;
    public EItemType itemType => mItemType;
    public Sprite itemImage => mItemImage;
    public GameObject itemPrefab => mItemPrefab;
    public bool canOverlap => mCanOverlap;
    public bool isConsumable => mIsConsumable;

}
