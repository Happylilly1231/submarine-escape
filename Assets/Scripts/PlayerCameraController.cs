using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라가 플레이어 머리 위치를 추적해 이동하고 회전하게 함
/// </summary>
public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private PlayerMove playerMove; // 플레이어 이동 스크립트(마우스 좌표 가져와야 함)
    [SerializeField] private Transform playerHead; // 플레이어 머리 위치
    [SerializeField] private Transform cameraPos; // 카메라 위치
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0.08f, 0.05f); // 눈높이

    private float _xRotation = 0f; // 카메라 상하 회전값
    // private float _yRotation = 0f; // 카메라 좌우 회전값
    public bool IsIntroPlaying = false; // 인트로 시퀀스 재생 여부

    /// <summary>
    /// 정지 중이 아닐 때 - 마우스 좌표에 따른 카메라 회전 & 플레이어 머리 위치에서 오프셋만큼 떨어진 위치로 이동
    /// </summary>
    void LateUpdate()
    {
        if (!SubmarineInGameManager.instance.IsPausing) // 정지 중이 아닐 때
        {
            if (IsIntroPlaying)
            {
                transform.position = playerHead.TransformPoint(cameraOffset) + playerMove.transform.forward * 0.1f;
                transform.rotation = playerHead.rotation * Quaternion.Euler(5f, 0f, 0f);
                return;
            }
            _xRotation -= playerMove.MouseY; // 상하 회전값
            // _yRotation += playerMove.MouseX; // 좌우 회전값
            _xRotation = Mathf.Clamp(_xRotation, -90f, 50f); // 시야 상하 회전 범위 제한
            transform.localRotation = Quaternion.Euler(_xRotation, playerMove.transform.eulerAngles.y, 0f);

            if (playerMove.IsDodging)
                transform.position = playerHead.TransformPoint(cameraOffset);
            else
                transform.position = cameraPos.position;
        }
    }
}
