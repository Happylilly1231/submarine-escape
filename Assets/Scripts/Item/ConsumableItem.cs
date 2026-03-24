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

    /// <summary>
    /// 소비 아이템 사용 로직
    /// </summary>
    /// <returns>아이템 사용 성공 여부</returns>
    public bool Use(Item item)
    {
        Debug.Log($"Consumed item: {item.ItemName}");

        switch (item.ItemName)
        {
            case "Locker Key":
                Debug.Log("사물함 열쇠 사용 - 특정 사물함 잠금 해제");
                return false;
            case "Flashlight Battery":
                // 손전등 배터리 사용 로직
                Debug.Log("손전등 배터리 교체 - 전력 100% 충전");
                return true;
            case "EnergyBar":
                // 에너지바 사용 로직
                Debug.Log("에너지바 - 5초 동안 이동 속도 20% 상승");
                return true;
            case "MedicalKit":
                if (_playerStat.Hp < 100f)
                {
                    _playerStat.Heal(50f);
                    Debug.Log("의료키트 - HP 50 회복");
                    return true;
                }
                else
                {
                    Debug.Log("HP가 이미 최대로 사용할 수 없음");
                    return false;
                }
            case "Thermal Syringe":
                // 열 주사기 사용 로직
                Debug.Log("급속 체온 회복 주사기 - 심해에서 30초 동안 체온 저하 무효화");
                return true;
        }
        return false;
    }
}
