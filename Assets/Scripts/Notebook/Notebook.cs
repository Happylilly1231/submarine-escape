using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notebook : InteractableBase
{
    private NotebookController notebookController;
    private void Start()
    {
        notebookController = GetComponent<NotebookController>();
    }

    public override string GetInteractText()
    {
        return LocalizationHelper.GetLocalizedInteractText("Interact/Notebook", "E");
    }

    public override void Interact()
    {
        notebookController.ActivatePuzzle();
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
}
