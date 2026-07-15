using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EscapeRoomHydraulicSystemPanel : IInteractable
{
    private bool _isBroken = false; // 고장 여부

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (_isBroken)
            return "Escape Hydraulic Panel is broken.";
        else
            return "Escape Room Hydraulic System Panel";
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
    }
}
