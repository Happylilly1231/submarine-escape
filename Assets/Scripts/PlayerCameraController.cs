using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 카메라가 플레이어 머리 위치를 추적해 이동하고 회전하게 함
/// </summary>
public class PlayerCameraController : PlayerCameraControllerBase
{
    [SerializeField] private PlayerMove playerMove; // 플레이어 이동 스크립트(마우스 좌표 가져와야 함)
    [SerializeField] private Transform playerHead; // 플레이어 머리 위치
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0.08f, 0.05f); // 눈높이

    private float _xRotation = 0f; // 카메라 상하 회전값
    public float xRotation => _xRotation;
    // private float _yRotation = 0f; // 카메라 좌우 회전값
    public bool IsIntroPlaying = false; // 인트로 시퀀스 재생 여부

    // [추가] 외부 연출 제어용 변수
    private Sequence _rotationSequence; // DOTween 시퀀스 저장용

    /// <summary>
    /// 정지 중이 아닐 때 - 마우스 좌표에 따른 카메라 회전 & 플레이어 머리 위치에서 오프셋만큼 떨어진 위치로 이동
    /// </summary>
    void LateUpdate()
    {
        if (!GameManager.instance.IsPausing) // 정지 중이 아닐 때
        {
            if (IsIntroPlaying)
            {
                transform.position = playerHead.TransformPoint(cameraOffset) + playerMove.transform.forward * 0.1f;
                transform.rotation = playerHead.rotation * Quaternion.Euler(5f, 0f, 0f);
                return;
            }

            // [핵심] 조작이 허용된 경우에만 마우스 입력값으로 _xRotation 및 플레이어 Y회전을 갱신
            if (_canControlInput)
            {
                _xRotation -= playerMove.MouseY; // 상하 회전값
                _xRotation = Mathf.Clamp(_xRotation, -90f, 50f); // 시야 상하 회전 범위 제한
            }

            // 카메라 Rotation 적용 (x: 카메라 상하, y: 플레이어 좌우)
            transform.localRotation = Quaternion.Euler(_xRotation, playerMove.transform.eulerAngles.y, 0f);

            // 위치 기본값 결정
            Vector3 targetPos;
            if (playerMove.IsDodging)
                targetPos = playerHead.TransformPoint(cameraOffset);
            else
                targetPos = cameraPos.position;

            // 흔들림 강도가 있으면 퍼린 노이즈 더해서 흔들림 적용
            if (_shakeIntensity > 0f)
            {
                float x = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * _shakeIntensity;
                float y = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * _shakeIntensity;
                targetPos += new Vector3(x, y, 0f);
            }

            // 최종 위치 적용
            transform.position = targetPos;
        }
    }

    #region 타겟(Target) 바라보기 API
    /// <summary>
    /// 특정 World 위치(Vector3)를 부드럽게 바라봅니다.
    /// </summary>
    public void RotateToTargetPos(Vector3 targetPosition, float duration, Ease easeType = Ease.OutQuad)
    {
        // 타겟을 향하는 방향 계산
        Vector3 dir = targetPosition - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return; // 동일한 위치일 경우 예외 처리

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        Vector3 euler = targetRotation.eulerAngles;

        float targetYAngle = euler.y;

        // Unity의 EulerAngle (0~360)을 상하 회전용 (-180~180) 범위로 변환
        float targetXAngle = euler.x;
        if (targetXAngle > 180f) targetXAngle -= 360f;

        // 기존의 RotateToYAngle 메서드재활용!
        RotateToYAngle(targetYAngle, duration, targetXAngle, easeType);
    }

    /// <summary>
    /// 특정 World 위치(Vector3)를 즉시 바라봅니다.
    /// </summary>
    public void SetRotationToTargetPosInstant(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        Vector3 euler = targetRotation.eulerAngles;

        float targetYAngle = euler.y;
        float targetXAngle = euler.x;
        if (targetXAngle > 180f) targetXAngle -= 360f;

        SetRotationInstant(targetYAngle, targetXAngle);
    }

    #endregion

    #region DOTween 연출용 회전 제어 API

    /// <summary>
    /// 1. 즉시 특정 각도(Y축) 및 시선(X축)을 바라보게 합니다.
    /// </summary>
    public void SetRotationInstant(float targetYAngle, float targetXAngle = 0f)
    {
        KillActiveTween();

        _xRotation = Mathf.Clamp(targetXAngle, -90f, 50f);

        Vector3 playerEuler = playerMove.transform.eulerAngles;
        playerMove.transform.rotation = Quaternion.Euler(playerEuler.x, targetYAngle, playerEuler.z);
    }

    /// <summary>
    /// DOTween으로 점차 특정 각도로 회전
    /// </summary>
    /// <param name="targetYAngle">목표 월드 Y축 회전값</param>
    /// <param name="duration">회전 시간(초)</param>
    /// <param name="targetXAngle">목표 카메라 X축 회전값</param>
    /// <param name="easeType">이징 함수 (기본값: OutQuad)</param>
    public void RotateToYAngle(float targetYAngle, float duration, float targetXAngle = 0f, Ease easeType = Ease.OutQuad)
    {
        KillActiveTween();

        // 1. 회전 시작할 때 인풋 차단
        SetInputEnabled(false);

        float clampedTargetX = Mathf.Clamp(targetXAngle, -90f, 50f);

        // DOTween Sequence 생성
        _rotationSequence = DOTween.Sequence();

        // 1) 카메라 상하 회전 (DOVirtual.Float로 _xRotation 값을 감싸서 조작)
        _rotationSequence.Join(
            DOVirtual.Float(_xRotation, clampedTargetX, duration, x => _xRotation = x)
        );

        // 2) 플레이어 몸통 좌우 회전 (DORotate 사용 / RotateMode.FastBeyond360으로 자연스러운 회전)
        Vector3 targetPlayerEuler = new Vector3(
            playerMove.transform.eulerAngles.x,
            targetYAngle,
            playerMove.transform.eulerAngles.z
        );

        _rotationSequence.Join(
            playerMove.transform.DORotate(targetPlayerEuler, duration, RotateMode.FastBeyond360)
        );

        // 이징 및 완료 이벤트 설정
        _rotationSequence.SetEase(easeType)
            // 2. 회전 연출이 완벽히 끝난 순간 조작 다시 허용!
            .OnComplete(() => SetInputEnabled(true))
            .OnKill(() =>
            {
                SetInputEnabled(true);
                _rotationSequence = null;
            });
    }

    /// <summary>
    /// 진행 중인 Tween 안전하게 취소
    /// </summary>
    private void KillActiveTween()
    {
        if (_rotationSequence != null && _rotationSequence.IsActive())
        {
            _rotationSequence.Kill();
        }
    }

    private void OnDisable()
    {
        KillActiveTween();
    }

    #endregion
}
