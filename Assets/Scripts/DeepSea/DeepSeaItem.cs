using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepSeaItem : MonoBehaviour
{
    private DeepSeaPlayerMove _player;

    private void Awake()
    {
        _player = FindObjectOfType<DeepSeaPlayerMove>();
    }

    public bool Use(Item item)
    {
        switch (item.ItemName)
        {
            case "Thermal Protector":
                DeepSeaInventoryManager.Instance.IsThermalProtectorActive = true;
                Debug.Log("열 보호 장치 - 심해에서 90초 동안 체온 저하 무효화");
                return true;
            case "Oxygen Capsule":
                _player.RechargeOxygen(100f); // 산소 100% 충전
                Debug.Log("산소 캡슐 - 산소 100% 충전");
                return true;
            case "Helicopter Locator":

                Debug.Log("헬리콥터 좌표 입력 손목시계 - 헬리콥터 위치 확인");
                return true;
        }
        return false;
    }
}
