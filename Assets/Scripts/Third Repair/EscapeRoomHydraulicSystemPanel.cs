using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EscapeRoomHydraulicSystemPanel : MonoBehaviour, IInteractable
{
    [SerializeField] private Door escapeRoomDoor; // 탈출실 문
    private bool _isBroken = false; // 고장 여부

    private void Start()
    {
        // Broke(); 테스트용
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (_isBroken)
            return LocalizationHelper.GetLocalizedInteractText("Interact/EscpaeDoorHydraulicPanelBroken");
        else
            return LocalizationHelper.GetLocalizedInteractText("Interact/EscapeDoorHydraulicPanel");
    }

    public void Interact()
    {
        return;
    }
    #endregion

    /// <summary>
    /// 고장
    /// </summary>
    public void Broke()
    {
        _isBroken = true;
        escapeRoomDoor.isRepairNeed = true; // 탈출실 문 수리 필요로 변경
    }
}
