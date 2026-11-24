using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UnderwaterCameraController : PlayerCameraControllerBase
{
    [Header("참조")]
    [SerializeField] private UnderwaterInput underwaterInput;
    [SerializeField] private UnderwaterPlayerMove underwaterPlayerMove;

    [Header("카메라 기본 설정")]
    [SerializeField] private float minPitch = -90f;
    [SerializeField] private float maxPitch = 80f;
    private float lookBackMaxPitch = 50f;
    [SerializeField] private float minYaw = 0f;
    [SerializeField] private float maxYaw = 0f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("뒤/정면 보기")]
    [SerializeField] private Vector3 headLookBackOffset = new Vector3(0.1f, 0.1f, -0.25f); // 뒤 볼 때 카메라 위치 오프셋
    private Vector3 headLookBackMutationOffset = new Vector3(0.1f, 0.1f, -0.7f); // 괴물화 시 뒤 볼 때 카메라 위치 오프셋
    [SerializeField] private Quaternion headTurnYaw = Quaternion.Euler(0f, 160f, 0f);
    [SerializeField] private Quaternion headTiltOffset = Quaternion.Euler(10f, 0f, 6f);

    [Header("조명")]
    public Light cameraLight; // 카메라 앞을 비추는 손전등 빛

    // 기본
    private float _mouseX = 0f;
    private float _mouseY = 0f;
    private float _pitch = 0f; // X축 회전 (상하)
    private float _yaw = 0f; // Y축 회전 (좌우)
    private Quaternion _currentLocalRotation;

    // 뒤/정면 보기
    public bool IsLookingBack { get; private set; } // 현재 뒤 보고 있는지 여부 
    public bool IsTurningHead { get; private set; } // 고개를 돌리고 있는지 여부 변수

    // 연출용
    private bool _isPlayingLookEffect = false;
    private Quaternion _effectLocalRotation = Quaternion.identity;
    private Vector3 _effectLocalPosition = Vector3.zero;
    private Tween _lookEffectTween;

    private void OnEnable()
    {
        SetInputEnabled(true);
    }

    private void OnDisable()
    {
        SetInputEnabled(false);
    }

    private void Start()
    {
        _currentLocalRotation = Quaternion.identity;

        if (GameManager.instance.IsAbilityActive)
        {
            headLookBackOffset = headLookBackMutationOffset;
        }

        enabled = false;
    }

    public override void SetInputEnabled(bool isEnabled)
    {
        if (isEnabled) // 조작 활성화
        {
            // 만약 연출을 보는 중이었다면 -> 연출 강제 종료
            if (_isPlayingLookEffect)
            {
                _lookEffectTween?.Kill();
                _isPlayingLookEffect = false;
                _effectLocalPosition = Vector3.zero;
            }

            // 조작 불가 상태였었다면 -> 현재 회전 각도 동기화
            if (!_canControlInput)
            {
                SyncRotationFromCurrentTransform();
            }

            underwaterInput.glanceBackEvent -= ToggleLookBack;
            underwaterInput.glanceBackEvent += ToggleLookBack;
        }
        else // 조작 비활성화
        {
            underwaterInput.glanceBackEvent -= ToggleLookBack;
        }

        base.SetInputEnabled(isEnabled);
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing) return;

        // 감도 적용
        float sensitivity = GameManager.instance.MouseSensitivity;
        _mouseX = underwaterInput.LookInput.x * sensitivity;
        _mouseY = underwaterInput.LookInput.y * sensitivity;
    }

    private void LateUpdate()
    {
        if (GameManager.instance.IsPausing) return;

        // 카메라 위치 이동
        Vector3 cameraTargetPos = cameraPos.position; // 기본은 카메라 위치
        if (IsLookingBack) // 뒤를 보고 있을 때
        {
            cameraTargetPos = cameraPos.TransformPoint(headLookBackOffset); // 목이 보이지 않게 연출에서 조정해주는 오프셋을 더한 위치를 카메라의 목표 위치로 설정
        }
        transform.position = cameraTargetPos; // 현재 타겟 위치로 카메라 옮김

        // 카메라 회전 적용
        if (_canControlInput) // 조작 활성화 시
        {
            // 회전 값 누적
            _yaw += _mouseX;
            _pitch -= _mouseY;

            // 범위에 맞게 조정
            _yaw = Mathf.Clamp(_yaw, minYaw, maxYaw);
            float currentMaxPitch = IsLookingBack ? lookBackMaxPitch : maxPitch;
            _pitch = Mathf.Clamp(_pitch, minPitch, currentMaxPitch);

            // 부드러운 회전
            Quaternion targetLocalRotation = Quaternion.Euler(_pitch, _yaw, 0f); // 목표 로컬 회전 값
            _currentLocalRotation = Quaternion.Slerp(_currentLocalRotation, targetLocalRotation, rotationSpeed * Time.deltaTime); // 부드럽게 목표 값으로 감
            if (IsLookingBack) // 뒤 보고 있을 때는 -> 뒤 기준 회전
                transform.rotation = cameraPos.rotation * headTurnYaw * _currentLocalRotation * headTiltOffset;
            else // 정면 -> 정면 기준 회전
                transform.rotation = cameraPos.rotation * _currentLocalRotation;
        }
        else // 조작 비활성화 시
        {
            if (_isPlayingLookEffect) // 연출 보는 중일 때
            {
                // cameraPos 회전 값을 기준으로 연출 로컬 회전 값 적용
                transform.rotation = cameraPos.rotation * _effectLocalRotation;
            }
        }

        // 괴물일 때는 45도 이상 위로 올려다보면 괴물 외형 비활성화 (카메라에 머리 보이지 않게 하기 위해서)
        if (GameManager.instance.IsAbilityActive)
            PlayerManager.Instance.playerAbility.monsterGeo.SetActive(transform.forward.y < 0.7f);

        DeepSeaInventoryManager.Instance.UpdatePlayerHandTarget();
    }

    #region 뒤/정면 보기 (Q키)

    /// <summary>
    /// 뒤 보기 토글
    /// </summary>
    public void ToggleLookBack()
    {
        // 이미 고개를 돌리는 중이거나, 플레이어의 액션이 락된 상태일 때 -> 실행 X
        if (IsTurningHead || underwaterPlayerMove.IsActionLocked) return;

        IsTurningHead = true; // 고개 돌리는 중으로 설정
        SetInputEnabled(false); // 카메라 조작 불가
        Quaternion targetRotation = Quaternion.identity; // 목표 회전 값
        Vector3 targetPos = Vector3.zero; // 목표 위치
        _yaw = 0f; // _yaw는 초기화

        if (!IsLookingBack)
        {
            // 뒤 보기
            IsLookingBack = true;
            _pitch = Mathf.Clamp(_pitch, minPitch, lookBackMaxPitch);
            Quaternion mouseRot = Quaternion.Euler(_pitch, 0f, 0f);
            targetRotation = cameraPos.rotation * headTurnYaw * mouseRot * headTiltOffset; // 뒤 회전 값
            targetPos = cameraPos.TransformPoint(headLookBackOffset);
            DeepSeaUIManager.Instance.SetActiveBackViewUI(true);
        }
        else
        {
            // 정면 보기
            IsLookingBack = false;
            Quaternion mouseRot = Quaternion.Euler(_pitch, 0f, 0f);
            targetRotation = cameraPos.rotation * mouseRot; // 정면 회전 값
            targetPos = cameraPos.position;
            DeepSeaUIManager.Instance.SetActiveBackViewUI(false);
        }

        // 회전 시퀀스 실행
        Sequence seq = DOTween.Sequence();
        transform.position = targetPos;
        seq.Append(transform.DORotateQuaternion(targetRotation, 0.25f).SetEase(Ease.OutQuart).SetEase(Ease.OutQuart));
        // seq.Join(transform.DOMove(targetPos, 0.1f)); // 목표 위치로 같이 부드럽게 이동
        seq.OnComplete(() =>
        {
            IsTurningHead = false; // 고개 돌리는 중 아님으로 설정
            SetInputEnabled(true); // 카메라 조작 활성화
        });
    }

    #endregion

    #region 연출 관련

    /// <summary>
    /// 현재 카메라의 회전 값을 현재 카메라 컨트롤러 스크립트에 동기화 (카메라 조작 잠금하고 연출한 게 끝났을 때 그 회전을 그대로 유지하고 싶으면 사용(지금은 기본적으로 사용됨))
    /// </summary>
    private void SyncRotationFromCurrentTransform()
    {
        // 카메라 위치 기준 현재 카메라의 로컬 회전값 구하기
        Quaternion localRot = Quaternion.Inverse(cameraPos.rotation) * transform.rotation;
        if (IsLookingBack)
        {
            localRot = Quaternion.Inverse(headTurnYaw) * localRot * Quaternion.Inverse(headTiltOffset);
        }
        Vector3 euler = localRot.eulerAngles; // 오일러 각으로 변환

        // 유니티에 맞게 -180 ~ 180으로 변환
        _pitch = (euler.x > 180f) ? euler.x - 360f : euler.x;
        _yaw = (euler.y > 180f) ? euler.y - 360f : euler.y;

        // 범위에 맞게 조정
        float currentMaxPitch = IsLookingBack ? lookBackMaxPitch : maxPitch;
        _pitch = Mathf.Clamp(_pitch, minPitch, currentMaxPitch);
        _yaw = Mathf.Clamp(_yaw, minYaw, maxYaw);

        // LateUpdate의 Slerp가 튀지 않도록 현재 로컬 회전값도 일치시킴 (Z축 롤 제거)
        _currentLocalRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    /// <summary>
    /// 아래 바라보는 함수
    /// </summary>
    /// <param name="downAngle">아래 바라볼 각도</param>
    /// <param name="duration">시간</param>
    public void LookDown(float downAngle = 60f, float duration = 0.5f)
    {
        // 입력 잠금
        if (_canControlInput)
            SetInputEnabled(false);

        // 범위에 맞게 조정
        float targetPitch = Mathf.Clamp(downAngle, minPitch, maxPitch);

        // 월드 기준으로 변환
        Quaternion targetWorldRotation = cameraPos.rotation * Quaternion.Euler(targetPitch, 0f, 0f);

        // 회전
        transform.DORotateQuaternion(targetWorldRotation, duration).SetEase(Ease.OutQuad);
    }

    public void PlayActionLook(float targetPitch, float targetYaw, float outDuration, float holdDuration, float returnDuration)
    {
        IsTurningHead = false;
        transform.DOKill();

        _isPlayingLookEffect = true;
        SetInputEnabled(false);
        _lookEffectTween?.Kill();

        _effectLocalRotation = Quaternion.Inverse(cameraPos.rotation) * transform.rotation;
        _effectLocalPosition = Vector3.zero;

        // 현재 보기 상태에 맞는 '기준 회전값' 계산
        Quaternion baseLocalRot = IsLookingBack
            ? (headTurnYaw * headTiltOffset)
            : Quaternion.identity;

        // 기준 회전에 연출 각도(targetPitch, targetYaw)를 합성
        Quaternion actionTargetRot = baseLocalRot * Quaternion.Euler(targetPitch, targetYaw, 0f);

        Sequence lookSeq = DOTween.Sequence();
        // Quaternion 전용 보간으로 오일러 짐벌락 방지 및 정확한 복귀
        lookSeq.Append(DOTween.To(() => _effectLocalRotation, x => _effectLocalRotation = x, actionTargetRot.eulerAngles, outDuration).SetEase(Ease.OutCubic));
        lookSeq.AppendInterval(holdDuration);
        // 복귀할 때도 Vector3.zero가 아니라 baseLocalRot(뒤돌기 기본 자세 또는 정면)으로 복귀
        lookSeq.Append(DOTween.To(() => _effectLocalRotation, x => _effectLocalRotation = x, baseLocalRot.eulerAngles, returnDuration).SetEase(Ease.InOutQuad));

        lookSeq.OnComplete(() =>
        {
            _effectLocalRotation = baseLocalRot;
            _isPlayingLookEffect = false;
            SetInputEnabled(true); // 이제 IsLookingBack 상태와 실제 회전 각도가 100% 일치함!
        });

        _lookEffectTween = lookSeq;
    }

    #endregion
}
