using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 심해 탈출 씬 전용 카메라 컨트롤러 (DeepSeaPlayerMove 직접 참조)
/// </summary>
public class DeepSeaCameraController : MonoBehaviour
{
    [Header("=== Target References ===")]
    [Tooltip("심해 플레이어 이동 스크립트")]
    [SerializeField] private DeepSeaPlayerMove playerMove;

    [Tooltip("카메라가 위치할 플레이어의 카메라 피벗/머리 Transform")]
    [SerializeField] private Transform cameraPos;

    [Header("=== Clamp Settings ===")]
    [SerializeField] private float minPitch = -70f; // 바닥 완전히 볼 수 있게
    [SerializeField] private float maxPitch = 70f;  // 하늘 볼 수 있게

    private float _xRotation = 0f; // 카메라 상하 회전값 (Pitch)

    private void Awake()
    {
        if (playerMove == null)
            playerMove = GetComponentInParent<DeepSeaPlayerMove>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (playerMove == null || cameraPos == null) return;
        if (GameManager.instance != null && GameManager.instance.IsPausing) return;

        HandleCameraRotation();

        // 카메라 위치는 플레이어 머리(cameraPos) 위치 고정
        transform.position = cameraPos.position;
    }

    private void HandleCameraRotation()
    {
        // 1. 마우스 Y축 입력으로 카메라 Pitch(상하) 연산
        _xRotation -= playerMove.MouseY;
        _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

        // 2. Yaw(좌우)는 playerMove가 Rotate시킨 몸통 Y축 회전을 그대로 따름
        float targetYaw = playerMove.transform.eulerAngles.y;

        // 3. 최종 카메라 회전 적용 (X: 상하 시선, Y: 몸통 방향)
        transform.rotation = Quaternion.Euler(_xRotation, targetYaw, 0f);
    }
}