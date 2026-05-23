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
    [SerializeField] private string itemName; // 아이템의 이름
    [SerializeField] private EItemType itemType; // 아이템의 유형
    [SerializeField] private Sprite itemImage; // 아이템의 이미지
    [SerializeField] private GameObject itemPrefab; // 아이템의 프리팹
    [SerializeField] private bool canOverlap; // 아이템 중첩 가능 여부
    [SerializeField] private bool isConsumable; // 아이템 1회용 여부

    [Header("Grip offset")]
    [SerializeField] private Vector3 gripPositionOffset; // 아이템을 잡았을 때 위치 오프셋
    [SerializeField] private Vector3 gripRotationOffset; // 아이템을 잡았을 때 회전 오프셋

    // 읽기 전용 접근
    public string ItemName => itemName;
    public EItemType ItemType => itemType;
    public Sprite ItemImage => itemImage;
    public GameObject ItemPrefab => itemPrefab;
    public bool CanOverlap => canOverlap;
    public bool IsConsumable => isConsumable;
    public Vector3 GripPositionOffset => gripPositionOffset;
    public Vector3 GripRotationOffset => gripRotationOffset;
}
