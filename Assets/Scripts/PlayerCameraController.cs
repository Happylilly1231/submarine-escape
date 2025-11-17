using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] PlayerMove playerMove;
    [SerializeField] Transform playerHead;
    [SerializeField] Vector3 cameraOffset = new Vector3(0f, 0.08f, 0.05f); // 눈높이

    float xRotation = 0f; // 카메라 상하 회전값
    float yRotation = 0f; // 카메라 좌우 회전값

    void Update()
    {
        if (!playerMove.isPausing)
        {
            // 상하 회전
            xRotation -= playerMove.mouseY;
            yRotation += playerMove.mouseX;
            xRotation = Mathf.Clamp(xRotation, -90f, 50f); // 시야 상하 회전 범위 제한
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }

    void LateUpdate()
    {
        if (!playerMove.isPausing)
            transform.position = playerHead.TransformPoint(cameraOffset);
    }
}
