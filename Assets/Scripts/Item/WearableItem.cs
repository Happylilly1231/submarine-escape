using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WearableItem : MonoBehaviour
{
    [SerializeField] private GameObject wetsuit; // 잠수복 오브젝트
    [SerializeField] private GameObject playerHead; // 플레이어 머리 오브젝트
    [SerializeField] private Camera mainCamera; // 메인 카메라
    [SerializeField] private Camera wetsuitCamera; // 잠수복 카메라

    private bool _isWetsuitEquipped = false; // 잠수복 착용 여부


    public void Use(Item item)
    {
        Debug.Log($"Consumed item: {item.ItemName}");

        switch (item.ItemName)
        {
            case "Wetsuit":
                if (_isWetsuitEquipped)
                {
                    playerHead.SetActive(true); // 플레이어 머리 오브젝트 활성화
                    wetsuit.SetActive(false); // 잠수복 오브젝트 비활성화

                    mainCamera.enabled = true; // 메인 카메라 활성화
                    wetsuitCamera.enabled = false; // 잠수복 카메라 비활성화
                    _isWetsuitEquipped = false;

                    Debug.Log("잠수복 벗음");
                }
                else
                {
                    playerHead.SetActive(false); // 플레이어 머리 오브젝트 비활성화
                    wetsuit.SetActive(true); // 잠수복 오브젝트 활성화

                    mainCamera.enabled = false; // 메인 카메라 비활성화
                    wetsuitCamera.enabled = true; // 잠수복 카메라 활성화
                    _isWetsuitEquipped = true;
                    Debug.Log("잠수복 착용");
                }
                break;
            case "Thermal Protector":
                FindAnyObjectByType<PlayerTemperature>()?.EquipThermalProtector();
                Debug.Log("열 보호 장치 - 심해에서 90초 동안 체온 저하 무효화");
                break;
        }
    }
}
