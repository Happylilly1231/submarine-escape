using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 심해 탈출 씬 전용 카메라 컨트롤러 (DeepSeaPlayerMove 직접 참조)
/// </summary>
public class DeepSeaCameraController : MonoBehaviour
{
    [SerializeField] private DeepSeaPlayerMove playerMove;
    [SerializeField] private Transform cameraPos;
    [SerializeField] private float minPitch = -80f;
    public float MinPitch => minPitch;
    [SerializeField] private float maxPitch = 70f;
    public float MaxPitch => maxPitch;
    public Light cameraLight; // 카메라 앞을 비추는 손전등 빛

    private Camera mainCamera;

    private float _xRotation = 0f; // 카메라 상하 회전값 (Pitch)

    private bool _canControlInput = true;
    private Sequence _rotationSequence; // DOTween 시퀀스 저장용
    private Tween _shakeTween; // 카메라 셰이크 트윈 저장용
    private Tween _fovTween;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// 플레이어 조작 입력 가능 여부 설정
    /// </summary>
    public void SetInputEnabled(bool isEnabled)
    {
        _canControlInput = isEnabled;
    }

    private void LateUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        // 카메라 위치는 플레이어 머리(cameraPos) 위치 고정
        transform.position = cameraPos.position;

        if (_canControlInput)
        {
            // 3. 마우스 상하(Pitch) 입력 추가 반영
            _xRotation -= playerMove.MouseY;
            _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);
        }

        float targetYaw = cameraPos.eulerAngles.y;
        transform.rotation = Quaternion.Euler(_xRotation, targetYaw, 0f);
    }

    // /// <summary>
    // /// FOV 반동을 포함한 카메라 셰이크
    // /// </summary>
    // public void ShakeCameraWithFOV(float duration = 0.25f, float strength = 0.4f, float fovImpact = 6f)
    // {
    //     KillActiveTween();
    //     SetInputEnabled(false);

    //     if (mainCamera == null) mainCamera = Camera.main;
    //     float defaultFov = mainCamera.fieldOfView;

    //     // 1. 위치 셰이크
    //     _shakeTween = transform.DOShakePosition(duration, strength, vibrato: 20, randomness: 90f)
    //         .SetUpdate(true)
    //         .OnComplete(() => SetInputEnabled(true))
    //         .OnKill(() => SetInputEnabled(true));

    //     // 2. FOV 순간 팽창 후 복귀 (충격 시 순간 줌아웃 -> 복귀)
    //     _fovTween = mainCamera.DOFieldOfView(defaultFov + fovImpact, 0.05f)
    //         .SetUpdate(true)
    //         .OnComplete(() =>
    //         {
    //             mainCamera.DOFieldOfView(defaultFov, duration - 0.05f)
    //                 .SetEase(Ease.OutQuad)
    //                 .SetUpdate(true);
    //         });
    // }

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
    /// 2. DOTween을 이용해 점차 특정 각도로 회전합니다.
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

        if (_shakeTween != null && _shakeTween.IsActive())
        {
            _shakeTween.Kill();
        }

        if (_fovTween != null && _fovTween.IsActive()) _fovTween.Kill();
    }

    private void OnDisable()
    {
        KillActiveTween();
    }

    /// <summary>
    /// 특정 월드 위치(targetPosition)를 즉시 바라보도록 몸통(Y축)과 카메라(X축)를 회전시킵니다.
    /// </summary>
    public void LookAtPosition(Vector3 targetPosition)
    {
        // 1. 플레이어 몸통 회전 (Y축 전담)
        Vector3 bodyDir = targetPosition - playerMove.transform.position;
        bodyDir.y = 0f; // 수평 방향만 추출

        if (bodyDir != Vector3.zero)
        {
            playerMove.transform.rotation = Quaternion.LookRotation(bodyDir);
        }

        // 2. 카메라 상하 회전 (X축 전담)
        float horizontalDistance = bodyDir.magnitude;
        float heightDifference = targetPosition.y - transform.position.y; // 현재 카메라 높이 기준

        // Pitch 각도 계산 (-Atan2)
        float targetPitch = -Mathf.Atan2(heightDifference, horizontalDistance) * Mathf.Rad2Deg;

        // 내부 X축 회전 변수 동기화 및 제한 적용
        _xRotation = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        playerMove.XRotation = _xRotation;

        // 3. 카메라 회전 즉시 적용
        float targetYaw = playerMove.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(_xRotation, targetYaw, 0f);
    }

    /// <summary>
    /// 특정 위치를 부드럽게 바라보도록 회전시키는 코루틴
    /// </summary>
    public IEnumerator Routine_LookAtPosition(Vector3 targetPosition, float duration, bool isDefaultForward = false)
    {
        Quaternion startBodyRot = playerMove.transform.rotation;
        float startPitch = _xRotation;

        // 목표 몸통 Y축 회전 계산
        Vector3 bodyDir = targetPosition - playerMove.transform.position;
        bodyDir.y = 0f;

        Quaternion targetBodyRot;

        // 수평 거리의 제곱(sqrMagnitude)이 매우 작을 때는(중앙 직하단/직상단 등)
        // 시작 회전(startBodyRot)을 유지하거나 정면(Vector3.forward)을 바라보게 안전 처리
        if (bodyDir.sqrMagnitude < 0.0001f)
        {
            if (isDefaultForward)
                targetBodyRot = Quaternion.LookRotation(Vector3.forward);
            else
                targetBodyRot = startBodyRot;
        }
        else
        {
            targetBodyRot = Quaternion.LookRotation(bodyDir);
        }

        // 목표 카메라 X축 회전(Pitch) 계산
        float horizontalDistance = bodyDir.magnitude;
        float heightDifference = targetPosition.y - transform.position.y;
        float targetPitch = Mathf.Clamp(-Mathf.Atan2(heightDifference, horizontalDistance) * Mathf.Rad2Deg, minPitch, maxPitch);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 몸통 Y축 부드러운 회전
            playerMove.transform.rotation = Quaternion.Slerp(startBodyRot, targetBodyRot, t);

            // 카메라 X축(Pitch) 부드러운 회전 및 내부 변수 동기화
            _xRotation = Mathf.Lerp(startPitch, targetPitch, t);
            playerMove.XRotation = _xRotation;

            yield return null;
        }
    }
}