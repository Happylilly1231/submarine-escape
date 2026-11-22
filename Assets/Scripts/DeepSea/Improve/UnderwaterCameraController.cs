using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UnderwaterCameraController : PlayerCameraControllerBase
{
    [SerializeField] private UnderwaterInput underwaterInput;
    [SerializeField] private UnderwaterPlayerMove underwaterPlayerMove;

    [SerializeField] private float minPitch = -90f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float minYaw = 0f;
    [SerializeField] private float maxYaw = 0f;
    [SerializeField] private float rotationSpeed = 10f;

    private float _mouseX = 0f;
    private float _mouseY = 0f;
    private float _pitch = 0f; // X축 회전 (상하)
    private float _yaw = 0f; // Y축 회전 (좌우)

    private Quaternion _currentLocalRotation;

    private bool _isPlayingLookEffect = false;
    private Quaternion _effectLocalRotation = Quaternion.identity;
    private Tween _lookEffectTween;

    private void Start()
    {
        enabled = false;
        _currentLocalRotation = Quaternion.identity;
    }

    public override void SetInputEnabled(bool isEnabled)
    {
        // 입력을 다시 켤 때, 혹시 돌고 있던 시선 연출이 있다면 강제 종료
        if (isEnabled && _isPlayingLookEffect)
        {
            _lookEffectTween?.Kill();
            _isPlayingLookEffect = false;
        }

        // false 상태였다가 다시 true로 켜지는 순간에만 각도 동기화 실행
        if (!_canControlInput && isEnabled)
        {
            SyncRotationFromCurrentTransform();
        }

        base.SetInputEnabled(isEnabled);
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing) return;

        float sensitivity = GameManager.instance.MouseSensitivity;
        _mouseX = underwaterInput.LookInput.x * sensitivity;
        _mouseY = underwaterInput.LookInput.y * sensitivity;
    }

    private void LateUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        // 카메라 위치는 플레이어 머리(cameraPos) 위치 고정
        transform.position = cameraPos.position;

        if (_canControlInput)
        {
            // 회전 값 누적
            _yaw += _mouseX;
            _pitch -= _mouseY;

            _yaw = Mathf.Clamp(_yaw, minYaw, maxYaw);
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            Quaternion targetLocalRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _currentLocalRotation = Quaternion.Slerp(_currentLocalRotation, targetLocalRotation, rotationSpeed * Time.deltaTime);
            transform.rotation = cameraPos.rotation * _currentLocalRotation;
        }
        else if (_isPlayingLookEffect)
        {
            // 회피/대시 등 입력 잠금 연출 중일 때: 기준점(cameraPos)에 연출 회전을 곱해 유지
            transform.rotation = cameraPos.rotation * _effectLocalRotation;
        }

        // 괴물일 때는 45도 이상 위로 올려다보면 괴물 외형 비활성화 (카메라에 머리 보이지 않게 하기 위해서)
        if (GameManager.instance.IsAbilityActive)
            PlayerManager.Instance.playerAbility.monsterGeo.SetActive(transform.forward.y < 0.7f);
    }

    private void SyncRotationFromCurrentTransform()
    {
        // 1. 카메라의 현재 월드 회전에서 기준점(cameraPos)의 회전을 빼서 순수 로컬 회전만 추출
        Quaternion localRot = Quaternion.Inverse(cameraPos.rotation) * transform.rotation;

        // 2. 오일러 각으로 변환
        Vector3 euler = localRot.eulerAngles;

        // 3. 유니티의 0~360도 체계를 -180~180도 범위로 변환 (음수 각도 보정)
        _pitch = (euler.x > 180f) ? euler.x - 360f : euler.x;
        _yaw = (euler.y > 180f) ? euler.y - 360f : euler.y;

        // 4. 설정된 최소/최대 제한 각도 안으로 안전하게 클램프
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        _yaw = Mathf.Clamp(_yaw, minYaw, maxYaw);

        // 5. LateUpdate의 Slerp가 튀지 않도록 현재 로컬 회전값도 일치시킴 (Z축 롤 제거)
        _currentLocalRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void LookDown(float downAngle = 60f, float duration = 0.5f)
    {
        if (_canControlInput)
            SetInputEnabled(false);

        float targetPitch = Mathf.Clamp(downAngle, minPitch, maxPitch);

        Quaternion targetWorldRotation = cameraPos.rotation * Quaternion.Euler(targetPitch, 0f, 0f);

        transform.DORotateQuaternion(targetWorldRotation, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 회피/대시 시 지정한 각도(Pitch, Yaw)로 시선을 돌렸다가 정면으로 복귀
    /// </summary>
    public void PlayActionLook(float targetPitch, float targetYaw, float outDuration, float holdDuration, float returnDuration)
    {
        SetInputEnabled(false);
        _lookEffectTween?.Kill();

        _isPlayingLookEffect = true; // ★ 연출 모드 ON
        // 시작 시점을 현재 카메라 로컬 회전값으로 맞춤 (갑자기 튐 방지)
        _effectLocalRotation = Quaternion.Inverse(cameraPos.rotation) * transform.rotation;

        Vector3 targetEuler = new Vector3(targetPitch, targetYaw, 0f);

        Sequence lookSeq = DOTween.Sequence();
        // 1. 목표 각도(반대 방향 또는 아래)로 시선 이동
        lookSeq.Append(DOTween.To(() => _effectLocalRotation, x => _effectLocalRotation = x, targetEuler, outDuration).SetEase(Ease.OutCubic));
        // 2. 잠깐 유지
        lookSeq.AppendInterval(holdDuration);
        // 3. 정면(0, 0, 0)으로 시선 복귀
        lookSeq.Append(DOTween.To(() => _effectLocalRotation, x => _effectLocalRotation = x, Vector3.zero, returnDuration).SetEase(Ease.InOutQuad));
        // 4. 완료 후 마우스 입력 다시 활성화
        lookSeq.OnComplete(() =>
        {
            _effectLocalRotation = Quaternion.identity;
            _isPlayingLookEffect = false; // ★ 연출 모드 OFF
            SetInputEnabled(true);
        });

        _lookEffectTween = lookSeq;
    }
}
