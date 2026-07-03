using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCTVComputer : MonoBehaviour, IInteractable
{
    [SerializeField] private CCTVController cctvController;

    public string GetInteractText()
    {
        return "CCTV 보기 [E]";
    }

    public void Interact()
    {
        cctvController.ActivatePuzzle();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
}
