using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapViewController : MonoBehaviour
{
    [SerializeField] private GameObject mapUI; // 맵 UI
    [SerializeField] private Image mapImage; // 맵 이미지
    [SerializeField] private Sprite firstFloorMapImage; // 1층 맵 이미지
    [SerializeField] private Sprite secondFloorMapImage; // 2층 맵 이미지
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform mapRectTransform;
    private Vector3 mapCameraPos = new Vector3(-3, 50, 6);

    private float xMin = -18f;
    private float xMax = 12f;
    private float zMin = -27f;
    private float zMax = 39f;

    // 카메라 Size가 20일 때의 월드 크기
    private float mapWorldWidth;
    private float mapWorldHeight;

    private bool _isMapLocked = false; // 지도 잠금되어있는지(아직 지도 얻어서 상호작용하지 못한 상태) 여부
    private int _currentFloor = 1; // 현재 층수
    public float secondFloorHeight; // 2층 바닥 높이

    private void Start()
    {
        mapWorldWidth = xMax - xMin;
        mapWorldHeight = zMax - zMin;
    }

    /// <summary>
    /// 맵 잠금 해제
    /// </summary>
    public void UnlockMap()
    {
        _isMapLocked = true;
    }

    /// <summary>
    /// Tab키 입력으로 맵 켜기/끄기
    /// </summary>
    /// <param name="context">입력</param>
    public void OnToggleMap(InputAction.CallbackContext context)
    {
        if (!context.performed || !_isMapLocked) return;
        Debug.Log("Tap!");

        // 맵을 켜는 경우
        if (!mapUI.activeSelf)
        {
            // 현재 플레이어 위치에 따라 자동으로 보여줄 층수 설정됨
            if (SubmarineInGameManager.instance.player.transform.position.y < secondFloorHeight)
                SetFloor(1);
            else
                SetFloor(2);

            SubmarineInGameManager.instance.IsMapOpened = true;
            SubmarineInGameManager.instance.Pause();
            SubmarineInGameManager.instance.player.GetComponent<PlayerInput>().actions["ToggleMap"].Enable();
            ShowPlayerLocation();
        }
        else
        {
            SubmarineInGameManager.instance.IsMapOpened = false;
            SubmarineInGameManager.instance.Resume();
        }

        mapUI.SetActive(!mapUI.activeSelf);
    }

    /// <summary>
    /// 플레이어 위치 보여주기
    /// </summary>
    private void ShowPlayerLocation()
    {
        Transform playerTransform = SubmarineInGameManager.instance.player.transform;

        // 플레이어 위치를 0 ~ 1 사이로 정규화
        float normX = 1 - (playerTransform.position.x - xMin) / mapWorldWidth; // x는 밑으로 내려갈수록 증가이므로 1에서 빼주기
        float normZ = (playerTransform.position.z - zMin) / mapWorldHeight;
        Debug.Log("Player: " + playerTransform.position);

        // UI 좌표로 변환 (0~1 값을 UI 이미지 크기에 곱함)
        float uiX = 127 + normZ * 1663f; // z가 가로
        float uiY = 170 + normX * 739f; // x가 세로

        playerIcon.anchoredPosition = new Vector2(uiX, uiY);
    }

    /// <summary>
    /// 맵에서 볼 층수 설정(층수 버튼 함수)
    /// </summary>
    /// <param name="floor">층수</param>
    public void SetFloor(int floor)
    {
        _currentFloor = floor;
        if (floor == 1)
        {
            mapImage.sprite = firstFloorMapImage;
        }
        else
        {
            mapImage.sprite = secondFloorMapImage;
        }
    }
}
