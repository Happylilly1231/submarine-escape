using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CabinetLockType
{
    None,       // 그냥 열림
    KeyRequired // 열쇠 필요
}

/// <summary>
/// 캐비넷을 열고 닫을 수 있도록 하는 Interactable 컴포넌트
/// </summary>
public class Cabinet : InteractableBase
{
    [SerializeField] private CabinetLockType lockType; // 캐비넷 잠금 유형
    [SerializeField] private Item keyItem; // 열쇠 아이템 (KeyRequired일 때 필요)
    [SerializeField] private Transform cabinetDoor; // 실제로 움직이는 캐비넷 문
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float rotateDuration = 0.5f;

    private bool _isOpen = false; // 열린/닫힌 상태
    private bool _isMoving = false; // 이동 여부
    private Quaternion _closedRot; // 초기 회전값
    private Quaternion _openRot; // 열린 후 회전값

    void Start()
    {
        _closedRot = cabinetDoor.localRotation;
        _openRot = _closedRot * Quaternion.Euler(0, openAngle, 0);
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public override string GetInteractText()
    {
        if (_isOpen) return "close [E]";

        switch (lockType)
        {
            case CabinetLockType.None:
                return "open [E]";
            case CabinetLockType.KeyRequired:
                return IsRequiredItemSelected() ? "open [E]" : "Locked (Need Key)";
        }

        return "";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 캐비넷을 열거나 닫음
    /// </summary>
    public override void Interact()
    {
        if (_isMoving) return;
        if (lockType == CabinetLockType.KeyRequired && IsRequiredItemSelected())
        {
            lockType = CabinetLockType.None; // 열쇠 사용 후 잠금 해제
        }
        if (lockType == CabinetLockType.None)
        {
            if (_isOpen)
                StartCoroutine(RotateDoor(_openRot, _closedRot));
            else
                StartCoroutine(RotateDoor(_closedRot, _openRot));
            _isOpen = !_isOpen;
        }
    }

    /// <summary>
    /// 캐비넷 문을 부드럽게 이동시키는 코루틴
    /// </summary>
    private IEnumerator RotateDoor(Quaternion startRot, Quaternion endRot)
    {
        _isMoving = true;

        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);

            cabinetDoor.localRotation = Quaternion.Lerp(startRot, endRot, t);

            yield return null;
        }

        cabinetDoor.localRotation = endRot;
        _isMoving = false;
    }

    /// <summary>
    /// 플레이어가 열쇠를 소지하고 있는지 확인
    /// </summary>
    public override bool CanInteractwithSelectedItem(Item item)
    {
        if (lockType == CabinetLockType.None) return false;
        return item == keyItem;
    }
}
