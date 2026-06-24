using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapViewController : MonoBehaviour
{
    [SerializeField] private Image mapImage; // 맵 이미지
    [SerializeField] private Sprite firstFloorMapImage; // 1층 맵 이미지
    [SerializeField] private Sprite secondFloorMapImage; // 2층 맵 이미지
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform mapRectTransform;
    [SerializeField] private Button firstFloorButton; // 1층 버튼
    [SerializeField] private Button secondFloorButton; // 2층 버튼
    private Vector3 mapCameraPos = new Vector3(-3, 50, 6);

    private float xMin = -18f;
    private float xMax = 12f;
    private float zMin = -27f;
    private float zMax = 39f;

    // 카메라 Size가 20일 때의 월드 크기
    private float mapWorldWidth;
    private float mapWorldHeight;

    public int CurrentFloor { get; private set; } = 1; // 현재 층수
    private float _secondFloorHeight = 6f; // 2층 바닥 높이

    private void Start()
    {
        mapWorldWidth = xMax - xMin;
        mapWorldHeight = zMax - zMin;

        firstFloorButton.onClick.AddListener(() => { SetFloor(1); });
        secondFloorButton.onClick.AddListener(() => { SetFloor(2); });
    }

    /// <summary>
    /// 맵 UI 갱신
    /// </summary>
    public void UpdateMapUI()
    {
        // 현재 플레이어 위치에 따라 자동으로 보여줄 층수 설정됨
        if (SubmarineInGameManager.instance.player.transform.position.y < _secondFloorHeight)
            SetFloor(1);
        else
            SetFloor(2);

        ShowPlayerLocation();
    }

    /// <summary>
    /// 맵에서 볼 층수 설정(층수 버튼 함수)
    /// </summary>
    /// <param name="floor">층수</param>
    public void SetFloor(int floor)
    {
        if (CurrentFloor == floor)
            return;

        CurrentFloor = floor;
        if (floor == 1)
        {
            mapImage.sprite = firstFloorMapImage;
        }
        else
        {
            mapImage.sprite = secondFloorMapImage;
        }
    }

    /// <summary>
    /// 플레이어 위치 및 방향 보여주기
    /// </summary>
    private void ShowPlayerLocation()
    {
        Transform playerTransform = SubmarineInGameManager.instance.player.transform;

        // [기존 위치 계산 코드]
        float normX = 1 - (playerTransform.position.x - xMin) / mapWorldWidth;
        float normZ = (playerTransform.position.z - zMin) / mapWorldHeight;
        float uiX = 61f + normZ * 1295f;
        float uiY = 141f + normX * 600f;
        playerIcon.anchoredPosition = new Vector2(uiX, uiY);

        // ================= 새로운 벡터 기반 회전 방식 =================

        // 1. 플레이어가 실제 월드에서 바라보는 앞방향 Vector3 (X, Y, Z)를 가져옵니다.
        Vector3 playerForward = playerTransform.forward;

        // 2. 위치 공식에 맞게 회전 방향도 축을 매핑해줍니다.
        // 위치 계산할 때 [월드 Z -> UI X], [월드 X -> UI Y]로 하셨으니 방향 벡터도 똑같이 매핑합니다.
        // 단, normX 계산할 때 1에서 뺐으므로(반전), UI Y축 방향도 반전(-playerForward.x)시켜야 일치합니다.
        float mapDirectionX = playerForward.z;
        float mapDirectionY = -playerForward.x;

        // 3. 이 매핑된 방향을 라디안 각도로 바꾸고, 이를 다시 디그리(Degree) 각도로 변환합니다.
        float angle = Mathf.Atan2(mapDirectionY, mapDirectionX) * Mathf.Rad2Deg;

        float finalUiRotationZ = angle - 135f;

        // 5. 최종 회전값 적용
        playerIcon.localEulerAngles = new Vector3(0, 0, finalUiRotationZ);
    }
}
