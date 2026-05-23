using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬에 배치된 아이템 오브젝트를 나타냄
/// <para> Item 데이터를 보관하며, 다른 스크립트에서 참조 가능하게 제공 </para>
/// </summary>
public class ItemPickUp : MonoBehaviour
{
    [SerializeField] private Item item;
    public Item Item => item;

    [Header("세이브 시스템 식별용")]
    public string uniqueID;
    public bool isDropped = false;
}
