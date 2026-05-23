using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorpedoAutoLoadSwitch : MonoBehaviour, IInteractable
{
    private RadarController _radarController;

    public bool IsSwitchOn { get; private set; } = true; // 스위치 켜져있는지 여부

    private Color _switchOffLightColor = new Color(166f / 255f, 35f / 255f, 35f / 255f);

    private void Awake()
    {
        _radarController = FindAnyObjectByType<RadarController>();
    }

    private void OnEnable()
    {
        LightingManager.instance.OnLightChanged += ChangeLightIntensity;
    }

    private void OnDisable()
    {
        LightingManager.instance.OnLightChanged -= ChangeLightIntensity;
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 전력 필요
            return "Power Restoration Required";

        if (IsSwitchOn) // 스위치 켜져 있으면 -> 현재 어뢰 탑재 모드 자동임을 알려주기
            return "Current Torpedo Load Mode: AUTO";
        else // 스위치 꺼지면 -> 현재 어뢰 탑재 모드 수동임을 알려주기
            return "Current Torpedo Load Mode: MANUAL (Torpedo Room)"; // 어뢰 탑재 모드: 수동 (어뢰실)
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
        _radarController.SetCurrentTorpedoState(TorpedoState.Unloaded); // 현재 어뢰 상태를 탑재되지 않음으로 설정
        GetComponentInChildren<Light>().color = _switchOffLightColor; // 조명 빨간색으로 변경
    }

    /// <summary>
    /// 조명 강도 조절(불 꺼져있을 때 강도가 높으면 너무 밝아서 조정)
    /// </summary>
    public void ChangeLightIntensity(bool isTurnOn)
    {
        if (isTurnOn)
            GetComponentInChildren<Light>().intensity = 3f; // 불 켜져있을 때는 빨간색도 잘 보이게 3으로 높게 설정
        else
            GetComponentInChildren<Light>().intensity = 1f; // 불 꺼져있을 때는 1이어도 밝은 노란색이나 빨간색이나 잘 보이므로 1로 설정
    }
}