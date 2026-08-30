using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 심해 탈출 씬 전용 카메라 컨트롤러 (DeepSeaPlayerMove 직접 참조)
/// </summary>
public class DeepSeaCameraController : MonoBehaviour
{
    [SerializeField] private DeepSeaPlayerMove playerMove;
    [SerializeField] private Transform cameraPos;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float cameraRotationSpeed = 12f; // 부드러운 회전 속도

    private float _xRotation = 0f; // 카메라 상하 회전값 (Pitch)
    private float _previousModelXAngle = 0f; // 이전 프레임의 모델 X축 각도 저장용

    private void LateUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        HandleCameraRotation();

        // 카메라 위치는 플레이어 머리(cameraPos) 위치 고정
        transform.position = cameraPos.position;
    }

    private void HandleCameraRotation()
    {
        // 1. 현재 모델의 Local X축 각도 가져오기 (-180 ~ 180 범위 보정)
        float currentModelXAngle = playerMove.ModelTransform.localEulerAngles.x;
        if (currentModelXAngle > 180f) currentModelXAngle -= 360f;

        // 2. 방향 전환 등으로 모델의 꺾임 각도가 변경되었는지 확인
        float angleDelta = currentModelXAngle - _previousModelXAngle;

        if (Mathf.Abs(angleDelta) > 0.01f)
        {
            // 모델이 꺾인 각도(angleDelta)만큼 _xRotation을 반대로 보정하여 시선 튀는 현상 방지
            _xRotation -= angleDelta;

            // 보정된 값이 상하 제한 범위를 벗어나면 가장 가까운 한계값(minPitch ~ maxPitch)으로 램프
            _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

            _previousModelXAngle = currentModelXAngle;
        }

        // 3. 마우스 상하(Pitch) 입력 추가 반영
        _xRotation -= playerMove.MouseY;
        _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

        // 4. 기존 쿼터니언 회전 결합 (머리 정면 추종 유지)
        Quaternion baseYaw = Quaternion.Euler(0f, playerMove.transform.eulerAngles.y, 0f);
        Quaternion modelPitch = Quaternion.Euler(currentModelXAngle, 0f, 0f);
        Quaternion mousePitch = Quaternion.Euler(_xRotation, 0f, 0f);

        Quaternion targetRotation = baseYaw * modelPitch * mousePitch;

        // 5. 회전 적용
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * cameraRotationSpeed);
    }
}