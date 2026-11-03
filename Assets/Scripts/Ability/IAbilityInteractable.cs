using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbilityInteractable : IInteractable
{
    bool CanDestroy { get; }
    void InteractWithAbility(); // 능력있을 때 상호작용
}