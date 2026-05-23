using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIItem : MonoBehaviour
{
    public void Use(Item item, bool isViewing)
    {
        Debug.Log($"Used item: {item.ItemName}");
        switch (item.ItemName)
        {
            case "Map":
                FindAnyObjectByType<MapViewController>().UnlockMap(); // 맵 잠금 해제
                Debug.Log("맵 잠금 해제 - Tab키로 열고 닫을 수 있음");
                break;
            case "KeyPad Manual":
                Debug.Log(isViewing);
                if (!isViewing) // 확대
                {
                    transform.localPosition = new Vector3(0, 0.03f, 0.3f);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else // 원래 위치로
                {
                    transform.localPosition = new Vector3(0, 0, 0.5f);
                    transform.localRotation = Quaternion.Euler(19, 0, 0);
                }
                Debug.Log("키패드 매뉴얼 - 확대");
                break;
            case "Radar System Manual":
                Debug.Log(isViewing);
                if (!isViewing) // 확대
                {
                    transform.localPosition = new Vector3(0, 0f, 0.3f);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else // 원래 위치로
                {
                    transform.localPosition = new Vector3(0, 0, 0.5f);
                    transform.localRotation = Quaternion.Euler(19, 0, 0);
                }
                Debug.Log("레이더 시스템 매뉴얼 - 확대");
                break;
        }
    }
}
