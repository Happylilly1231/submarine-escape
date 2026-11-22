using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterRope : MonoBehaviour, IInteractable
{
    private HelicopterRescueCutscene _helicopterRescueCutscene;

    private void Start()
    {
        _helicopterRescueCutscene = FindObjectOfType<HelicopterRescueCutscene>();
    }

    public string GetInteractText()
    {
        return LocalizationHelper.GetLocalizedInteractText("Interact/HelicopterRope", "E");
    }

    public void Interact()
    {
        Debug.Log("헬리콥터 로프와 상호작용");
        _helicopterRescueCutscene.StartCutscene();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
}
