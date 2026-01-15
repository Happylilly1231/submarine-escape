using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 레이더 조작 패널
/// </summary>
public class RadarControlPanel : MonoBehaviour, IInteractable
{
    [SerializeField] private RadarController _radarController; // 레이더 컨트롤러
    [SerializeField] private Transform radarViewPoint; // 레이더 볼 때 카메라 위치
    [SerializeField] private Item toolKitItem; // 공구 상자 아이템 (파괴되어 고장났을 때 필요)

    private float _duration = 1.5f; // 레이더 볼 때 카메라 이동 시간
    private bool _isBroken = false; // 고장 여부

    private ItemEquipController _itemEquipController;
    private InventoryManager _inventoryManager;
    private PlayerInteractor _playerInteractor;

    void Start()
    {
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _playerInteractor = FindObjectOfType<PlayerInteractor>();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        if (!_isBroken) return false; // 고장 나지 않았을 때는 X
        return item == toolKitItem; // 고장 났을 때는 공구 상자 선택 여부에 따름
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (!_isBroken) // 고장 나지 않았을 때 -> 레이더 보기
        {
            return "View Radar [E]";
        }
        else // 고장 났을 때 - 공구 상자가 선택되어있을 때 -> 수리 / 선택 안됨 -> 수리 필요(공구 상자 필요) 메시지
        {
            return IsToolKitSelected() ? "Repair [E]" : "Repair Required (Tool Kit Required)";
        }
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 레이더 화면을 띄우거나 끔
    /// </summary>
    public void Interact()
    {
        if (!_isBroken) // 고장 나지 않았을 때 -> 레이더 보기
        {
            ViewRadar();
        }
        else if (_isBroken && IsToolKitSelected()) // 고장 났을 때는 공구 상자가 선택되어있을 때 -> 수리
        {
            StartCoroutine(Repair());
        }
        // 이외는 상호작용 X
    }

    /// <summary>
    /// 레이더 보기
    /// </summary>
    public void ViewRadar()
    {
        _playerInteractor.IsPuzzleActive = true;
        _playerInteractor.ClearDetectionText();
        SubmarineInGameManager.instance.SetFocusUI(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(Camera.main.transform.DOMove(radarViewPoint.position, _duration)
        .SetEase(Ease.OutQuad));

        seq.Join(Camera.main.transform.DORotateQuaternion(radarViewPoint.rotation, _duration)
            .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            _radarController.SetRadarVisible(true); // 레이더 보기
        });
    }

    /// <summary>
    /// 공구 상자 선택되었는지 여부
    /// </summary>
    private bool IsToolKitSelected()
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
    /// 고장 
    /// </summary>
    public void Broke()
    {
        _isBroken = true;
    }

    /// <summary>
    /// 수리
    /// </summary>
    private IEnumerator Repair()
    {
        float repairTime = 3f;
        float timer = repairTime;

        while (timer > 0)
        {
            Debug.Log($"수리 중...({timer:F0}초)");
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("수리 완료!");
        _isBroken = false;
    }
}
