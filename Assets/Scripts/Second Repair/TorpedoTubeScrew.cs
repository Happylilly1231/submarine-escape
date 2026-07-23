using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TorpedoTubeScrew : MonoBehaviour, IInteractable
{
    [SerializeField] private Item driverItem; // 드라이버 아이템
    [SerializeField] private TorpedoTube torpedoTube; // 어뢰 발사관
    [SerializeField] private Transform ejectViewPoint; // 나사 튀어나갈 때 볼 위치

    public bool IsTightened { get; private set; } = false;
    private float _duration = 2f;
    private bool _canInteract = true;

    private ItemEquipController _itemEquipController;
    private InventoryManager _inventoryManager;

    void Start()
    {
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _inventoryManager = FindObjectOfType<InventoryManager>();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        if (IsTightened || !_canInteract) return false; // 조여졌거나 상호작용이 불가능하면 -> 상호작용 X
        return item == driverItem; // 조이려면 -> 드라이버 아이템 필요
    }

    public string GetInteractText()
    {
        if (IsTightened || !_canInteract) // 조여졌거나 상호작용이 불가능하면 -> 상호작용 X
            return "";

        // 조이지 않았으면 - 드라이버가 선택되어있을 때 -> 나사 조이기 / 선택 안됨 -> 드라이버 필요 메시지
        if (IsDriverSelected())
            return LocalizationHelper.GetLocalizedInteractText("Interact/TightenScrew", "E");
        else
            return LocalizationHelper.GetLocalizedTextWithParameter("Interact/ItemRequired", driverItem.LocalizedDisplayName);
    }

    public void Interact()
    {
        if (IsTightened) // 조여졌으면 -> 상호작용 X
            return;

        // 드라이버가 선택되어있을 때 -> 나사 조이기
        if (IsDriverSelected())
        {
            TightenScrew();
        }
    }

    /// <summary>
    /// 드라이버 선택되었는지 여부
    /// </summary>
    private bool IsDriverSelected()
    {
        Item selectedItem = null;
        if (_itemEquipController.HeldItemData != null)
        {
            selectedItem = _itemEquipController.HeldItemData;
        }
        else if (_inventoryManager.SelectedSlotIndex >= 0 && _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex] != null)
        {
            selectedItem = _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item;
        }

        return CanInteractwithSelectedItem(selectedItem);
    }

    /// <summary>
    /// 나사 조이기
    /// </summary>
    private void TightenScrew()
    {
        // 앞으로 이동
        transform.DOLocalMoveX(-0.62f, _duration);

        // 회전
        transform.DOLocalRotate(new Vector3(0, 0, -360f), _duration, RotateMode.LocalAxisAdd)
        .OnComplete(() =>
        {
            IsTightened = true;
        });
    }

    /// <summary>
    /// 나사 튀어나오기
    /// </summary>
    public void Eject()
    {
        _canInteract = false;

        transform.SetParent(null); // 부모 null로 초기화 
        Rigidbody rb = gameObject.AddComponent<Rigidbody>(); // 리지드바디 추가
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 ejectDirection = transform.right + (Random.insideUnitSphere * 0.2f);
        rb.AddForce(ejectDirection * 5f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        torpedoTube.SetDoorOpenState(true);
        torpedoTube.IsCloseAvailable = false;
    }
}
