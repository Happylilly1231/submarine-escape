using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라가 플레이어 머리 위치를 추적해 이동하고 회전하게 함
/// </summary>
public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private PlayerMove mPlayerMove; // 플레이어 이동 스크립트(마우스 좌표 가져와야 함)
    [SerializeField] private Transform mPlayerHead; // 플레이어 머리 위치
    [SerializeField] private Vector3 mCameraOffset = new Vector3(0f, 0.08f, 0.05f); // 눈높이

    private float mXRotation = 0f; // 카메라 상하 회전값
    private float mYRotation = 0f; // 카메라 좌우 회전값

    /// <summary>
    /// 정지 중이 아닐 때 마우스 좌표에 따른 카메라 회전
    /// </summary>
    void Update()
    {
        if (!GameManager.instance.IsPausing) // 정지 중이 아닐 때
        {
            mXRotation -= mPlayerMove.MouseY; // 상하 회전값
            mYRotation += mPlayerMove.MouseX; // 좌우 회전값
            mXRotation = Mathf.Clamp(mXRotation, -90f, 50f); // 시야 상하 회전 범위 제한
            transform.localRotation = Quaternion.Euler(mXRotation, mYRotation, 0f);
        }
    }

    /// <summary>
    /// 정지 중이 아닐 때 플레이어 머리 위치에서 오프셋만큼 떨어진 위치로 이동
    /// </summary>
    void LateUpdate()
    {
        if (!GameManager.instance.IsPausing)
            transform.position = mPlayerHead.TransformPoint(mCameraOffset);
    }
}
