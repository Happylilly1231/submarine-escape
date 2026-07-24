using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrewRoom1KeyPad : InteractableBase
{
    private readonly HashSet<string> interactableItemNames = new HashSet<string>
    {
        "Blue Battery", "Red Battery", "Yellow Battery", "Green Battery", "Flashlight"
    };
    public bool IsCompleted = false;
    private KeyPadController _keyPadController;

    private void Start()
    {
        _keyPadController = FindObjectOfType<KeyPadController>();
    }

    #region 상호작용 인터페이스 구현
    public override string GetInteractText()
    {
        if (IsCompleted) return "";
        else return LocalizationHelper.GetLocalizedInteractText("Interact/Inspect", "E");
    }

    public override void Interact()
    {
        if (IsRequiredItemSelected()) _keyPadController.ActivatePuzzle();
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        if (item == null) return true;

        return interactableItemNames.Contains(item.ItemName);
    }
    #endregion
}
