using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleObject : MonoBehaviour, IAbilityInteractable
{
    public bool CanDestroy => true;

    [SerializeField] private string objName = "";

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (PlayerManager.Instance.CheckMonsterTransformation()) return $"파괴 가능: {objName}";
        return "";
    }

    public void Interact()
    {

    }

    public void InteractWithAbility()
    {
        PlayerManager.Instance.playerAbility.TryDestroy(gameObject, objName);
    }
}
