using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmergencyGenerator : InteractableBase, IAbilityInteractable
{
    public bool CanDestroy => false;

    [SerializeField] private PowerSwitch powerSwitch;

    public bool IsActivated { get; private set; } = false;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public override string GetInteractText()
    {
        if (!IsActivated)
        {
            if (PlayerManager.Instance.CheckMonsterTransformation()) return "작동 [E]";
            else return "작동시킬 충분한 힘이 없습니다...";
        }
        return "작동 중";
    }

    public override void Interact()
    {
        if (!IsActivated && PlayerManager.Instance.CheckMonsterTransformation())
        {
            InteractWithAbility();
            return;
        }
    }

    public void InteractWithAbility()
    {
        Activate(); // 작동
    }

    private void Activate()
    {
        IsActivated = true;
        _audioSource.Play(); // 소리 재생
        powerSwitch.TogglePower(true);
    }
}
