using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorpedoAutoLoadSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] private RadarController radarController;

    public bool IsSwitchOn { get; private set; } = true; // 스위치 켜져있는지 여부

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 전력 필요
            return "Power Restoration Required";

        if (IsSwitchOn) // 스위치 켜져 있으면 -> 현재 어뢰 탑재 모드 자동임을 알려주기
            return "Torpedo Load Mode: AUTO";
        else // 스위치 꺼지면 -> 현재 어뢰 탑재 모드 수동임을 알려주기
            return "Torpedo Load Mode: MANUAL (Torpedo Room)"; // 어뢰 탑재 모드: 수동 (어뢰실)
    }

    public void Interact()
    {
        return;
    }

    /// <summary>
    /// 스위치 끄기
    /// </summary>
    public void SwitchOff()
    {
        IsSwitchOn = false;
        radarController.SetCurrentTorpedoState(TorpedoState.Unloaded); // 현재 어뢰 상태를 탑재되지 않음으로 설정
    }
}