using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsumableItem : MonoBehaviour
{
    private PlayerStat _playerStat;

    void Awake()
    {
        _playerStat = FindObjectOfType<PlayerStat>();
    }

    public void Use(Item item)
    {
        Debug.Log($"Consumed item: {item.ItemName}");

        switch (item.ItemName)
        {
            case "Flashlight Battery":
                // 손전등 배터리 사용 로직
                Debug.Log("손전등 배터리 교체 - 전력 100% 충전");
                break;
            case "EnergyBar":
                // 에너지바 사용 로직
                Debug.Log("에너지바 - 5초 동안 이동 속도 20% 상승");
                break;
            case "MedicalKit":
                _playerStat.Heal(50f);
                Debug.Log("의료키트 - HP 50 회복");
                break;
            case "Thermal Syringe":
                // 열 주사기 사용 로직
                Debug.Log("급속 체온 회복 주사기 - 심해에서 30초 동안 체온 저하 무효화");
                break;
        }
    }
}
