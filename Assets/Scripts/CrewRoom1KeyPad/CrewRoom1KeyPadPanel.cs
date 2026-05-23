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
            return "관찰 [E]";
        else return "드라이버 필요";
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
