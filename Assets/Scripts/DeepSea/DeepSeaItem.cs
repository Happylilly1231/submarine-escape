using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepSeaItem : MonoBehaviour
{
    private UnderwaterPlayerStat _underwaterPlayerStat;
    private EnergyGunController _energyGunController;

    private void Awake()
    {
        _underwaterPlayerStat = FindObjectOfType<UnderwaterPlayerStat>();
        _energyGunController = FindAnyObjectByType<EnergyGunController>();
    }

    public bool Use(Item item)
    {
        switch (item.ItemName)
        {
            case "Energy Gun":
                if (!_energyGunController.IsAming)
                    _energyGunController.CheckCurrentGauge();
                Debug.Log("에너지 건 - 게이지 확인");
                return true;
            case "Thermal Protector":
                DeepSeaInventoryManager.Instance.IsThermalProtectorActive = true;
                Debug.Log("열 보호 장치 - 심해에서 90초 동안 체온 저하 무효화");
                return true;
            case "Oxygen Capsule":
                _underwaterPlayerStat.RechargeOxygen(100f); // 산소 100% 충전
                Debug.Log("산소 캡슐 - 산소 100% 충전");
                return true;
        }
        return false;
    }
}
