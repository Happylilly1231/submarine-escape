using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary> 인벤토리 관리자
/// <para> - 플레이어 인벤토리 전체를 관리 </para> 
/// <para> - 인벤토리 UI 표시, 슬롯 선택, 아이템 추가/교체/사용/버리기/인벤토리에 반환 등 인벤토리의 모든 기능을 제어 </para>
/// <para> - 모든 인벤토리 관련 입력은 PlayerAction(Input System)의 'Player' 액션 맵에서 처리 </para>
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [SerializeField] private GameObject mInventoryUI; // 인벤토리 전체 UI 오브젝트
    [SerializeField] private GameObject mInventorySlotsParent; // 인벤토리 슬롯 부모 오브젝트
    [SerializeField] private Sprite mSlotSprite; // 슬롯 기본 스프라이트
    [SerializeField] private Sprite mSelectedSlotSprite; // 슬롯 선택 스프라이트

    [SerializeField] private Transform mRightHandTransform; // 아이템을 들고 있는 오른손 위치
    [SerializeField] private Transform mItemHoldTransform; // 아이템을 들고 있는 위치
    [SerializeField] public GameObject heldItemObject; // 현재 들고 있는 아이템 오브젝트

    private InventorySlot[] mInventorySlots; // 인벤토리 슬롯 배열

    private int mSelectedSlotIndex = -1; // 선택된 슬롯 인덱스
    private static bool mbIsInventoryOpen = false; // 인벤토리 활성화 상태
    private bool mbIsSwapMode = false; // T키 눌림 상태
    private HandController mHandController; // 플레이어 손 컨트롤러

    /// <summary>
    /// 인벤토리 UI를 초기화하고 슬롯 배열을 구성
    /// </summary>
    void Awake()
    {
        if (mInventoryUI.activeSelf)
        {
            mInventoryUI.SetActive(false);
        }

        mInventorySlots = mInventorySlotsParent.GetComponentsInChildren<InventorySlot>();
        mHandController = GetComponent<HandController>();
    }

    /// <summary>
    /// I키 입력으로 인벤토리를 활성화/비활성화
    /// </summary>
    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (!mbIsInventoryOpen)
        {
            OpenInventory();
        }
        else
        {
            CloseInventory();
        }
    }

    /// <summary>
    /// 인벤토리 활성화
    /// </summary>
    private void OpenInventory()
    {
        mInventoryUI.SetActive(true);
        mbIsInventoryOpen = true;
    }

    /// <summary>
    /// 인벤토리 비활성화
    /// </summary>
    private void CloseInventory()
    {
        mInventoryUI.SetActive(false);
        mbIsInventoryOpen = false;
    }

    /// <summary>
    /// T키 눌림 상태 업데이트 (슬롯 교체 기능에 사용)
    /// </summary>
    public void OnTHold(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            mbIsSwapMode = true;
        }
        else if (context.canceled)
        {
            mbIsSwapMode = false;
        }
    }

    /// <summary>
    /// 숫자키(1~6) 입력으로 인벤토리 슬롯 선택 또는 슬롯 교체
    /// <para> - T키가 눌린 상태라면 선택된 슬롯과 교체 </para>
    /// <para> - T키가 눌리지 않은 상태라면 해당 슬롯 선택 </para>
    /// </summary>
    public void OnSlotKeyPress(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!mbIsInventoryOpen) return;

        int slotIndex = context.control.name[0] - '1';

        if (mSelectedSlotIndex == slotIndex) return;
        if (mbIsSwapMode && mSelectedSlotIndex >= 0)
        {
            SwapSlots(mSelectedSlotIndex, slotIndex);
        }
        else
        {
            SelectSlot(slotIndex);
        }
    }

    /// <summary>
    /// 선택된 슬롯은 네온 스프라이트로 변경
    /// </summary>
    private void SelectSlot(int slotIndex)
    {
        mSelectedSlotIndex = slotIndex;

        for (int i = 0; i < mInventorySlots.Length; i++)
        {
            var slotImage = mInventorySlots[i].GetComponent<UnityEngine.UI.Image>();
            if (i == mSelectedSlotIndex)
            {
                slotImage.sprite = mSelectedSlotSprite;
            }
            else
            {
                slotImage.sprite = mSlotSprite;
            }
        }
    }

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템과 개수를 서로 교환
    /// </summary>
    private void SwapSlots(int selectedSlotIndex, int swapSlotIndex)
    {
        Debug.Log($"슬롯 {selectedSlotIndex + 1} 과 슬롯 {swapSlotIndex + 1} 교체");
        var selectedSlot = mInventorySlots[selectedSlotIndex];
        var swapSlot = mInventorySlots[swapSlotIndex];

        var tempItem = selectedSlot.item;
        var tempCount = selectedSlot.itemCount;

        selectedSlot.SetSlot(swapSlot.item, swapSlot.itemCount);
        swapSlot.SetSlot(tempItem, tempCount);
    }

    /// <summary> 인벤토리에 아이템 추가
    /// <para> - 중첩 가능한 아이템인 경우 기존 슬롯에 개수만 업데이트 </para>
    /// <para> - 중첩 불가능한 아이템이거나 기존에 없는 아이템인 경우 빈 슬롯에 새로 추가 </para>
    /// <para> - 빈 슬롯이 없으면 아이템 추가 실패 </para>
    /// </summary>
    /// <returns>인벤토리에 아이템 추가 성공 여부</returns>
    public bool AddItemToInventory(Item newItem, int count = 1)
    {
        if (newItem.canOverlap)
        {
            foreach (var slot in mInventorySlots)
            {
                if (slot.item != null && slot.item.itemID == newItem.itemID)
                {
                    slot.UpdateItemCount(slot.itemCount + count);
                    return true;
                }
            }
        }

        foreach (var slot in mInventorySlots)
        {
            if (slot.item == null)
            {
                slot.AddItem(newItem, count);
                return true;
            }
        }

        Debug.Log("인벤토리가 가득 찼습니다.");
        return false;
    }

    /// <summary>
    /// E키 입력으로 아이템 사용/장착
    /// </summary>
    public void OnItemUse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!mbIsInventoryOpen || mSelectedSlotIndex < 0) return;

        var selectedSlot = mInventorySlots[mSelectedSlotIndex];
        if (selectedSlot.item == null) return;
        if (selectedSlot.item.itemPrefab == null) return;

        mHandController.HoldItem(selectedSlot.item.itemPrefab);
    }

    /// <summary>
    /// Q키로 선택된 슬롯의 아이템 1개 버리기
    /// <para> - 아이템 프리팹을 현재 플레이어 앞에 생성하고 아이템 개수 1개 감소 </para>
    /// <para> - 손에 들고 있는 상태라면 아이템 오브젝트도 제거 </para>
    /// </summary>
    public void OnItemDrop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!mbIsInventoryOpen || mSelectedSlotIndex < 0) return;

        var selectedSlot = mInventorySlots[mSelectedSlotIndex];
        if (selectedSlot.item == null) return;
        if (selectedSlot.item.itemPrefab == null) return;

        Vector3 dropPosition = mRightHandTransform.position + mRightHandTransform.forward * 1f;
        GameObject dropItem = Instantiate(selectedSlot.item.itemPrefab, dropPosition, Quaternion.identity);

        StartCoroutine(ApplyRigidbody(dropItem, 1f));
        selectedSlot.UpdateItemCount(selectedSlot.itemCount - 1);

        if (heldItemObject != null)
        {
            Destroy(heldItemObject);
            heldItemObject = null;
            mHandController.ClearHand();
        }
    }

    /// <summary>
    /// 특정 시간 동안 오브젝트에 Rigidbody를 추가하여 중력을 적용한 후 제거
    /// </summary>
    private IEnumerator ApplyRigidbody(GameObject itemObject, float duration)
    {
        Rigidbody rb = itemObject.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDrag = 0.05f;
        rb.drag = 0f;
        rb.useGravity = true;

        yield return new WaitForSeconds(duration);

        if (rb != null)
        {
            Destroy(rb);
        }
    }

    /// <summary>
    /// R키로 아이템을 인벤토리 슬롯으로 반환 (장착 해제)
    /// <para> - 손에 들고 있는 아이템을 인벤토리에 추가 및 아이템 오브젝트 제거 </para>
    /// </summary>
    public void OnReturnToSlot(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (heldItemObject == null) return;

        mHandController.ClearHand();
    }
}
