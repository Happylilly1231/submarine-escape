using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorInteractor : MonoBehaviour
{
    [SerializeField] private Door door;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (door.CompareTag("EscapeRoomDoor") && !door.isOpened) // 닫혀있는 탈출실 문을 열려는 경우
            {
                SubmarineInGameManager.instance.hasEverOpenedEscapeDoor = true;
                SubmarineInGameManager.instance.AlertOn(); // 경보 발생
            }
            // 문 닫거나 열기 - 간단 구현(현재는 시야에 문이 들어오지 않아도 문과 상호작용 가능)
            Debug.Log("문에 닿았습니다.");
            if (door.gameObject.activeSelf)
                door.ToggleDoor();
        }
    }
}
