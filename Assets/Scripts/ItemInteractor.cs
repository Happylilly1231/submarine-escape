using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어가 아이템을 감지하고 줍는 기능
/// </summary>
public class ItemInteractor : MonoBehaviour
{
    [SerializeField] private float mRayDistance = 2.0f; // 아이템 감지 거리
    [SerializeField] private Camera mPlayerCamera; // 플레이어 카메라
    [SerializeField] private GameObject mPickUpUI; // 아이템 줍기 UI
    [SerializeField] private TMPro.TextMeshProUGUI mPickUpText; // 아이템 줍기 텍스트

    private InventoryManager mInventoryManager; // 인벤토리 매니저
    private FadeUI mFadeUI; // 페이드 UI
    private ItemPickUp mCurrentItem; // 현재 감지된 아이템
    private RaycastHit mRaycastHit; // 레이캐스트 히트 정보
    private bool mbCanPickUp = false; // 아이템 줍기 가능 여부

    void Awake()
    {
        mInventoryManager = FindObjectOfType<InventoryManager>();
        mFadeUI = mPickUpUI.GetComponent<FadeUI>();

        mPickUpUI.SetActive(true);
        mFadeUI.mCanvasGroup.alpha = 0f;
    }

    void Update()
    {
        DetectItem();
    }

    /// <summary>
    /// F키 입력으로 아이템 줍기
    /// </summary>
    public void OnItemPickUp(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (mbCanPickUp && mCurrentItem != null)
        {
            TryPickUpItem();
        }
    }

    /// <summary>
    /// 플레이어 앞에 있는 아이템 감지
    /// </summary>
    private void DetectItem()
    {
        if (Physics.SphereCast(mPlayerCamera.transform.position, 0.1f, mPlayerCamera.transform.forward, out mRaycastHit, mRayDistance))
        {
            if (mRaycastHit.transform.CompareTag("Item"))
            {
                ItemPickUp hitItem = mRaycastHit.transform.GetComponent<ItemPickUp>();

                if (mCurrentItem == hitItem)
                {
                    return;
                }

                mCurrentItem = hitItem;
                mbCanPickUp = true;

                mPickUpText.text = $"{mCurrentItem.item.itemName} [F]";

                mFadeUI.FadeIn();
                return;
            }
        }

        ClearDetection();
    }

    /// <summary>
    /// 아이템 감지 상태 초기화
    /// </summary>
    private void ClearDetection()
    {
        if (!mbCanPickUp) return;

        mFadeUI.FadeOut();

        mPickUpText.text = "";
        mCurrentItem = null;
        mbCanPickUp = false;
    }

    /// <summary>
    /// 현재 감지된 아이템을 인벤토리에 추가 시도
    /// </summary>
    private void TryPickUpItem()
    {
        if (mInventoryManager.AddItemToInventory(mCurrentItem.item))
        {
            Debug.Log(mCurrentItem.item.itemName + " 획득");
            Destroy(mCurrentItem.gameObject);
        }
        else
        {
            Debug.Log("인벤토리 꽉참");
        }

        ClearDetection();
    }
}
