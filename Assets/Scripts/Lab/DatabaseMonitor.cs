using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseMonitor : InteractableBase
{
    [SerializeField] private DatabaseMonitorController monitorController; // 모니터 컨트롤러

    private bool _isPowerOn = false; // 전력 켜져 있는지 여부

    private void OnEnable()
    {
        LightingManager.instance.OnLightChanged += SetPower;
    }

    void OnDisable()
    {
        LightingManager.instance.OnLightChanged -= SetPower;
    }

    /// <summary>
    /// 전력 켜거나 끄기
    /// </summary>
    private void SetPower(bool isPowerOn)
    {
        _isPowerOn = isPowerOn;
    }


    public override string GetInteractText()
    {
        if (!_isPowerOn) // 전력 없을 때 -> 전력 필요
            return LocalizationHelper.GetLocalizedInteractText("Interact/PowerRestorationRequired");
        return LocalizationHelper.GetLocalizedInteractText("Interact/UseDatabaseMonitor", "E");
    }

    public override void Interact()
    {
        if (!_isPowerOn) // 전력 없을 때 -> 상호작용 X
            return;
        // 모니터 컨트롤러의 퍼즐 활성화 메서드 호출
        monitorController.ActivatePuzzle();
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
}
