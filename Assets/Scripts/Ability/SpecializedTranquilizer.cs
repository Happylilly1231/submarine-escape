using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecializedTranquilizer : InteractableBase, IAbilityInteractable
{
    public bool CanDestroy => false;

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public override string GetInteractText()
    {
        if (PlayerManager.Instance.CheckMonsterTransformation()) return "사용 [E]";
        return "";
    }

    public override void Interact()
    {
        if (PlayerManager.Instance.CheckMonsterTransformation())
        {
            InteractWithAbility();
            return;
        }
    }

    public void InteractWithAbility()
    {
        Use(); // 사용
    }

    private void Use()
    {
        PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(false);
        gameObject.SetActive(false);
        FXManager.instance.ShowInjectionResultEffect(true, () =>
        {
            Debug.Log("특수 정신 안정제 사용 완료");
            PlayerManager.Instance.playerStat.StopSanityInstability();
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(true);
        });
    }
}
