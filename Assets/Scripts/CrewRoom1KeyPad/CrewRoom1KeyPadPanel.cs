using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrewRoom1KeyPadPanel : InteractableBase
{
    [SerializeField] private Item screwdriverItem;
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
        else if (IsRequiredItemSelected())
            return LocalizationHelper.GetLocalizedInteractText("Interact/Inspect", "E");
        else return LocalizationHelper.GetLocalizedTextWithParameter("Interact/ItemRequired", screwdriverItem.LocalizedDisplayName);
    }

    public override void Interact()
    {
        if (IsRequiredItemSelected()) _keyPadController.ActivatePuzzle();
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return item == screwdriverItem;
    }
    #endregion
}
