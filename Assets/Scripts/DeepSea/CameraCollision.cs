using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    [Header("=== Target & Layer Settings ===")]
    [SerializeField] private Transform cameraPivot;       // activeCameraPos (플레이어 머리/눈 피벗)
    [SerializeField] private LayerMask obstacleLayer;     // 벽, 장애물 레이어 (Default, Ground, Wall 등)

    [Header("=== Collision Parameters ===")]
    [SerializeField] private float cameraRadius = 0.2f;    // SphereCast 반지름
    [SerializeField] private float minDistance = 0.5f;     // 최소 카메라 거리
    [SerializeField] private float smoothSpeed = 15f;      // 당겨짐/복귀 보정 속도

    private float _defaultDistance;                        // 인스펙터 초기 설정 거리
    private Vector3 _localDirection;                       // 피벗 기준 카메라의 원래 로컬 방향

    private void Start()
    {
        if (cameraPivot == null && transform.parent != null)
        {
            cameraPivot = transform.parent;
        }

        if (cameraPivot == null) return;

        // [중요] 카메라가 피벗 기준 '원래' 어느 방향, 어느 거리에 있어야 하는지 고정값으로 기록
        Vector3 defaultLocalPos = transform.position - cameraPivot.position;
        _defaultDistance = defaultLocalPos.magnitude;
        _localDirection = defaultLocalPos.normalized;
    }

    private void LateUpdate()
    {
        if (cameraPivot == null) return;

        // 1. 벽이 없을 때 카메라가 가야 하는 '진짜 원래 목표 위치'를 매 프레임 계산
        // 피벗의 현재 회전값에 초기 방향과 거리를 곱해 구합니다.
        Vector3 origin = cameraPivot.position;
        Vector3 desiredCameraPos = origin + (cameraPivot.rotation * _localDirection) * _defaultDistance;

        // 피벗에서 가상의 목표 위치로 향하는 방향과 최대 거리
        Vector3 dir = (desiredCameraPos - origin).normalized;
        float maxDistance = _defaultDistance;

        // 2. 가상의 목표 위치를 향해 레이저(SphereCast)를 쏨 (실제 카메라 위치 기준이 아님!)
        float targetDistance = maxDistance;

        if (Physics.SphereCast(origin, cameraRadius, dir, out RaycastHit hit, maxDistance, obstacleLayer))
        {
            // 벽에 부딪혔다면 최소 거리와 부딪힌 거리 사이로 제한
            targetDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, maxDistance);
        }

        // 3. 최종 계산된 안전한 위치로 카메라를 부드럽게 이동
        Vector3 finalPos = origin + dir * targetDistance;
        transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * smoothSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraPivot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, cameraRadius);
        }
    }
}