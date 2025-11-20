using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// 아이템을 들거나 손을 비우는 경우 플레이어 손을 제어
/// </summary>
public class HandController : MonoBehaviour
{
    [SerializeField] private TwoBoneIKConstraint mHandIKConstraint; // 손 위치 IK 컨스트레인트
    [SerializeField] private Transform mHandHoldTransform; // 손에 아이템을 들고 있는 위치

    private InventoryManager mInventoryManager;

    void Awake()
    {
        mInventoryManager = FindObjectOfType<InventoryManager>();
    }

    /// <summary>
    /// 아이템 생성하고 IK 활성화
    /// </summary>
    public void HoldItem(GameObject itemPrefab)
    {
        if (mInventoryManager.heldItemObject != null)
        {
            Destroy(mInventoryManager.heldItemObject);
        }
        mInventoryManager.heldItemObject = Instantiate(itemPrefab, mHandHoldTransform.position, mHandHoldTransform.rotation, mHandHoldTransform);

        mHandIKConstraint.weight = 1f;
    }

    /// <summary>
    /// 들고 있는 아이템 삭제하고 IK 비활성화
    /// </summary>
    public void ClearHand()
    {
        if (mInventoryManager.heldItemObject != null)
        {
            Destroy(mInventoryManager.heldItemObject);
            mInventoryManager.heldItemObject = null;
        }

        mHandIKConstraint.weight = 0f;
    }
}
