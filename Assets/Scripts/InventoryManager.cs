using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary> 인벤토리 관리자
/// <para> - 인벤토리 UI의 활성화 제어 </para> 
/// <para> - 인벤토리 슬롯 초기화 및 전역 접근 관리 </para>
/// <para> - 키보드 입력과 UI 연동 </para>
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [SerializeField] protected GameObject mInventoryUI; // 인벤토리 UI 게임 오브젝트
    [SerializeField] protected GameObject mInventorySlotsParent; // 인벤토리 슬롯 부모 오브젝트
    [SerializeField] protected Sprite mInventorySlot; // 인벤토리 슬롯 기본 이미지
    [SerializeField] protected Sprite mInventorySlotSelected; // 인벤토리 슬롯 선택 이미지

    private InventorySlot[] mInventorySlots; // 인벤토리 슬롯 배열

    private static bool IsInventoryOpen = false; // 인벤토리 활성화 상태
    private bool mbIsTHeld = false; // T키 눌림 상태
    private int mSelectedSlotIndex = -1; // 선택된 슬롯 인덱스
    private PlayerInput mPlayerInput;   // 플레이어 입력 컴포넌트

    void Awake()
    {
        // 인벤토리 UI 초기화
        if (mInventoryUI.activeSelf)
        {
            mInventoryUI.SetActive(false);
        }

        // 자식 오브젝트로부터 InventorySlot 컴포넌트들을 찾아 배열에 저장
        mInventorySlots = mInventorySlotsParent.GetComponentsInChildren<InventorySlot>();

        mPlayerInput = GetComponent<PlayerInput>();
        // 입력 액션에 이벤트 핸들러 등록
        mPlayerInput.actions["ToggleInventory"].performed += OnToggleInventory;
        mPlayerInput.actions["SelectSlot"].performed += OnSelectSlot;
        mPlayerInput.actions["THold"].performed += OnTHold;
        mPlayerInput.actions["THold"].canceled += OnTHold;
        mPlayerInput.actions["NumberPress"].performed += OnNumberPress;
    }

    /// <summary>
    /// 인벤토리를 I키를 눌러 활성화/비활성화
    /// </summary>
    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!IsInventoryOpen)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }
    }

    /// <summary>
    /// 인벤토리 활성화
    /// </summary>
    private void OpenInventory()
    {
        mInventoryUI.SetActive(true);
        IsInventoryOpen = true;
    }

    /// <summary>
    /// 인벤토리 비활성화
    /// </summary>
    private void CloseInventory()
    {
        mInventoryUI.SetActive(false);
        IsInventoryOpen = false;
    }

    /// <summary>
    /// 숫자키(1~6)로 인벤토리 슬롯 선택
    /// </summary>
    private void OnSelectSlot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!IsInventoryOpen || mbIsTHeld)
            {
                return;
            }
            else
            {
                for (int i = 1; i <= 6; i++)
                {
                    if (context.control.name == i.ToString())
                    {
                        SelectSlot(i - 1);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 선택된 인벤토리 슬롯 업데이트
    /// </summary>
    private void SelectSlot(int slotIndex)
    {
        if (mSelectedSlotIndex == slotIndex)
        {
            return; // 이미 선택된 슬롯이면 아무 작업도 하지 않음
        }

        mSelectedSlotIndex = slotIndex;

        // 모든 슬롯 이미지 업데이트
        for (int i = 0; i < mInventorySlots.Length; i++)
        {
            var slotImage = mInventorySlots[i].GetComponent<UnityEngine.UI.Image>();
            if (i == mSelectedSlotIndex)
            {
                slotImage.sprite = mInventorySlotSelected;
            }
            else
            {
                slotImage.sprite = mInventorySlot;
            }
        }
    }

    /// <summary>
    /// T키 눌림 상태 업데이트
    /// </summary>
    private void OnTHold(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            mbIsTHeld = true;
            Debug.Log("T키 눌림");
        }
        else if (context.canceled)
        {
            mbIsTHeld = false;
            Debug.Log("T키 떼짐");
        }
    }

    /// <summary>
    /// T키를 누른 후 숫자키(1~6)로 선택된 슬롯과 다른 슬롯 간의 아이템 교체
    /// </summary>
    private void OnNumberPress(InputAction.CallbackContext context)
    {
        if (context.performed && mbIsTHeld)
        {
            for (int i = 1; i <= 6; i++)
            {
                if (context.control.name == i.ToString())
                {
                    SwapSlots(mSelectedSlotIndex, i - 1);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 선택된 슬롯과 교체를 원하는 슬롯 간의 인벤토리 슬롯 업데이트
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

    /// <summary>
    /// 인벤토리에 아이템 추가
    /// </summary>
    /// <returns>인벤토리에 아이템 추가 성공 여부</returns>
    public bool AddItemToInventory(Item newItem, int count = 1)
    {
        // 중첩 가능한 아이템이라면 기존 슬롯에 개수만 업데이트
        if (newItem.canOverlap)
        {
            foreach (var slot in mInventorySlots)
            {
                if (slot.item != null && slot.item.itemID == newItem.itemID)
                {
                    slot.UpdateItemCount(slot.itemCount + count);
                    return true; // 아이템 추가 성공
                }
            }
        }

        // 빈 슬롯을 찾아 아이템 추가
        foreach (var slot in mInventorySlots)
        {
            if (slot.item == null)
            {
                slot.AddItem(newItem, count);
                return true; // 아이템 추가 성공
            }
        }

        // 빈 슬롯이 없으면 아이템 추가 실패
        Debug.Log("인벤토리가 가득 찼습니다.");
        return false; // 아이템 추가 실패
    }

    public Item[] testItems = new Item[6]; // 테스트용 아이템 배열

    public void TestAddItem1()
    {
        AddTestItem(0);
    }

    public void TestAddItem2()
    {
        AddTestItem(1);
    }

    public void TestAddItem3()
    {
        AddTestItem(2);
    }

    public void TestAddItem4()
    {
        AddTestItem(3);
    }

    public void TestAddItem5()
    {
        AddTestItem(4);
    }

    public void TestAddItem6()
    {
        AddTestItem(5);
    }

    public void AddTestItem(int index)
    {
        if (testItems == null || index < 0 || index >= testItems.Length)
        {
            return;
        }

        Item item = testItems[index];
        if (item == null)
        {
            Debug.LogError($"{index}번 테스트 아이템이 비어있습니다!");
            return;
        }

        bool success = AddItemToInventory(item, 1);

        if (success)
            Debug.Log($"{item.itemName} 획득!");
    }
}
