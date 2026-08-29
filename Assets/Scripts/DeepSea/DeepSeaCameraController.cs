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

    private float _previousVerticalInput = 0f;
    private Vector2 _previousMoveInput = Vector2.zero;

    [SerializeField] private float cameraRotationSpeed = 12f; // 부드러운 회전 속도 (10~15 추천)

    private void LateUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        HandleCameraRotation();

        // 카메라 위치는 플레이어 머리(cameraPos) 위치 고정
        transform.position = cameraPos.position;
    }

    private void HandleCameraRotation()
    {
        // 1. 방향 입력 변경 시 마우스 추가 상하 시선(_xRotation) 리셋
        bool isAnyInputChanged = !Mathf.Approximately(playerMove.VerticalInput, _previousVerticalInput) ||
                                 !Mathf.Approximately(playerMove.MoveInput.x, _previousMoveInput.x) ||
                                 !Mathf.Approximately(playerMove.MoveInput.y, _previousMoveInput.y);

        if (isAnyInputChanged)
        {
            if (_previousVerticalInput >= 0f && playerMove.VerticalInput < 0f) // 아래로 내려가기 시작했을 때만
                _xRotation = 0f; // 정면 리셋
            _previousVerticalInput = playerMove.VerticalInput;
            _previousMoveInput = playerMove.MoveInput;
        }

        // 2. 마우스 상하(Pitch) 입력 연산
        _xRotation -= playerMove.MouseY;
        _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

        // 3. 모델 X축 각도 가져오기 (Euler 0~360 범위 보정)
        float modelXAngle = playerMove.ModelTransform.localEulerAngles.x;
        if (modelXAngle > 180f) modelXAngle -= 360f;

        // 4. 각 회전을 독립적인 Quaternion으로 생성
        Quaternion baseYaw = Quaternion.Euler(0f, playerMove.transform.eulerAngles.y, 0f); // Y축 (좌우)
        Quaternion modelPitch = Quaternion.Euler(modelXAngle, 0f, 0f);                      // 모델 자체 X축 (90/120도)
        Quaternion mousePitch = Quaternion.Euler(_xRotation, 0f, 0f);                       // 마우스 X축 (시야 상하)

        // 5. [중요] 회전 순서 결합: Y축 회전 -> 모델 X축 꺾임 -> 마우스 상하 회전
        // 순서대로 곱해줘야 마우스를 올릴 때 다른 축으로 시야가 비틀리지 않습니다.
        Quaternion targetRotation = baseYaw * modelPitch * mousePitch;

        // 6. 부드러운 회전 적용
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * cameraRotationSpeed);
    }
}