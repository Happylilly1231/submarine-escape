using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using DG.Tweening; // Input System 네임스페이스

public class PanicCrosshair : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject crosshairTotalPartUI;
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private Image targetZoneImage;
    [SerializeField] private GameObject crosshairOriginUI;
    [SerializeField] private GameObject crosshairInTargetZoneUI;

    [Header("색상 설정")]
    [SerializeField] private Color targetZoneOriginColor = new Color(197f / 255f, 35f / 255f, 0f, 130f / 255f);
    [SerializeField] private Color crosshairInTargetZoneColor = new Color(0f, 221f / 255f, 24f / 255f, 120f / 255f);

    [Header("조작 및 흔들림 수치")]
    private float noiseSpeed = 1.5f;
    private float noiseAmount = 100f;
    private float controlSpeed = 550f;
    private float targetRadius = 200f;
    private float maxRadius = 350f;

    [Header("Rig Target")]
    [SerializeField] private Transform playerHandTarget;
    [SerializeField] private Transform playerMonsterSpikeTarget;
    private Transform _currentRigTarget;
    private Vector3 _currentRigTargetOriginPos;

    // 인풋 관련
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _fireAction;

    // 오프셋 관련
    private Vector2 _currentOffset;
    private float _seedX;
    private float _seedY;
    private Vector2 _baseInputOffset; // 순수 WASD 입력으로만 누적되는 기준 위치
    private float _crossHairOffsetRadius = 350f;

    private bool _isActive = false;
    private bool _isFireSequencing = false;

    public bool IsInTargetZone { get; private set; }
    public bool CanFire { get; private set; } = false;

    // 이벤트
    public event Action<bool> OnFireTriggered;

    private void Awake()
    {
        // 인풋 가져오기
        _playerInput = FindAnyObjectByType<PlayerInput>();
        _moveAction = _playerInput.actions.FindAction("DeepSea_Move");
        _fireAction = _playerInput.actions.FindAction("DeepSea/Ascend");

        // 랜덤 시드
        _seedX = UnityEngine.Random.Range(0f, 100f);
        _seedY = UnityEngine.Random.Range(100f, 200f);

        crosshairTotalPartUI.SetActive(false);
        targetZoneImage.gameObject.SetActive(false);
        crosshairOriginUI.SetActive(true);
        crosshairInTargetZoneUI.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.instance.IsAbilityActive)
        {
            _currentRigTarget = playerMonsterSpikeTarget;
            _currentRigTargetOriginPos = new Vector3(0f, 1.7f, 1.5f);
        }
        else
        {
            _currentRigTarget = playerHandTarget;
            _currentRigTargetOriginPos = new Vector3(-0.7f, 1.3f, 3f);
        }
        _currentRigTarget.localPosition = _currentRigTargetOriginPos;
    }

    public void SetTargetZonePos(Vector3 offsetDir)
    {
        targetZoneImage.rectTransform.anchoredPosition = offsetDir * _crossHairOffsetRadius;
    }

    public void ToggleCrosshair(bool enable)
    {
        _isActive = enable;
        crosshairTotalPartUI.SetActive(_isActive);

        if (enable)
        {
            _baseInputOffset = Vector2.zero;
            _currentOffset = Vector2.zero;
            crosshairUI.anchoredPosition = _currentOffset;

            // Input Action 이벤트 등록 및 활성화
            _fireAction.performed += OnFirePerformed;
        }
        else
        {
            // 이벤트 해제 및 비활성화
            _fireAction.performed -= OnFirePerformed;
        }
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (!_isActive || !CanFire) return;

        // Fire 입력이 들어왔을 때, 현재 조준선이 중앙에 있는지 여부를 담아 이벤트 발송
        OnFireTriggered?.Invoke(IsInTargetZone);

        // 능력 사용 게임일 때 -> 가시를 들었다가 내려치는 연출 추가
        if (GameManager.instance.IsAbilityActive)
        {
            _isFireSequencing = true;
            Vector3 firePos = _currentRigTarget.localPosition;
            _currentRigTarget.localPosition = new Vector3(0f, 3f, 1f);
            Sequence seq = DOTween.Sequence();
            seq.Append(_currentRigTarget.DOLocalMove(firePos, 0.3f).SetEase(Ease.OutElastic));
            seq.OnComplete(() =>
            {
                _isFireSequencing = false;
            });
        }
    }

    private void Update()
    {
        if (!_isActive || GameManager.instance.IsPausing) return;

        // 1. WASD 입력만 따로 누적 및 범위 제한 (여기가 조준선의 진짜 기준 위치)
        Vector2 inputDir = _moveAction.ReadValue<Vector2>() * controlSpeed * Time.unscaledDeltaTime;
        _baseInputOffset += inputDir;
        _baseInputOffset = Vector2.ClampMagnitude(_baseInputOffset, maxRadius);

        // 2. 펄린 노이즈는 누적(+=)하지 않고, 현재 시간에 따른 '떨림 오프셋'으로만 독립적으로 계산
        float nX = (Mathf.PerlinNoise(Time.unscaledTime * noiseSpeed, _seedX) - 0.5f) * 2f;
        float nY = (Mathf.PerlinNoise(Time.unscaledTime * noiseSpeed, _seedY) - 0.5f) * 2f;
        Vector2 noiseOffset = new Vector2(nX, nY) * noiseAmount * Time.unscaledDeltaTime; // 떨림 강도 (누적하지 않음!)

        // 3. 최종 크로스헤어 위치 = 순수 입력 기준 위치 + 노이즈 떨림
        _currentOffset = _baseInputOffset + noiseOffset;
        _currentOffset = Vector2.ClampMagnitude(_currentOffset, maxRadius); // 최종 최대 범위 제한
        crosshairUI.anchoredPosition = _currentOffset;

        if (!_isFireSequencing)
        {
            float tX = _currentOffset.x / maxRadius;
            float tY = _currentOffset.y / maxRadius;
            float radiusX = GameManager.instance.IsAbilityActive ? 0.2f : 0.7f;
            float radiusY = GameManager.instance.IsAbilityActive ? 0.2f : 0.3f;

            // 1. 카메라의 우측(Right)과 상단(Up) 방향을 가져옴 (화면 기준 방향 보정)
            // 만약 메인 카메라가 아니라면 지정해둔 카메라 변수를 사용하세요.
            Transform camTransform = Camera.main.transform;
            Vector3 camRight = camTransform.right;
            Vector3 camUp = camTransform.up;

            // 2. 카메라 방향을 기준으로 월드 공간에서의 오프셋 계산
            Vector3 worldRigOffset = (camRight * (tX * radiusX)) + (camUp * (tY * radiusY));

            // 3. 부모 기준으로 저장해둔 원점(OriginPos)을 월드 좌표로 변환한 뒤 오프셋 적용
            Transform parentTransform = _currentRigTarget.parent;
            Vector3 worldOriginPos = parentTransform != null
                ? parentTransform.TransformPoint(_currentRigTargetOriginPos)
                : _currentRigTargetOriginPos;

            _currentRigTarget.position = worldOriginPos + worldRigOffset;

            // 4. 회전값 처리
            if (!GameManager.instance.IsAbilityActive)
            {
                _currentRigTarget.localRotation = Quaternion.Euler(new Vector3(tY * -20f, 0f, 10f));
            }
        }


        // // 1. 손떨림 펄린 노이즈
        // float nX = (Mathf.PerlinNoise(Time.unscaledTime * noiseSpeed, seedX) - 0.5f) * 2f;
        // float nY = (Mathf.PerlinNoise(Time.unscaledTime * noiseSpeed, seedY) - 0.5f) * 2f;
        // Vector2 noiseDir = new Vector2(nX, nY) * noiseAmount * Time.unscaledDeltaTime;

        // // 2. Input System에서 WASD Vector2 값 읽기
        // Vector2 inputDir = _moveAction.ReadValue<Vector2>() * controlSpeed * Time.unscaledDeltaTime;

        // // 3. 위치 계산 및 범위 제한
        // currentOffset += noiseDir + inputDir;
        // currentOffset = Vector2.ClampMagnitude(currentOffset, maxRadius);
        // crosshairUI.anchoredPosition = currentOffset;

        // if (!_isFireSequencing)
        // {
        //     // Rig도 같이 움직이기
        //     float tX = currentOffset.x / maxRadius;
        //     float tY = currentOffset.y / maxRadius;
        //     float radiusX = GameManager.instance.IsAbilityActive ? 0.2f : 0.8f;
        //     float radiusY = GameManager.instance.IsAbilityActive ? 0.2f : 0.3f;
        //     Vector3 rigOffset = new Vector3(tX * radiusX, tY * radiusY, 0f);
        //     _currentRigTarget.localPosition = _currentRigTargetOriginPos + rigOffset;
        //     if (!GameManager.instance.IsAbilityActive) _currentRigTarget.localRotation = Quaternion.Euler(new Vector3(tY * -20f, 0f, 10f));
        // }

        // 4. 중앙 영역 진입 체크
        if (CanFire)
        {
            IsInTargetZone = Vector2.Distance(targetZoneImage.rectTransform.position, crosshairUI.position) <= targetRadius;
            Debug.Log(IsInTargetZone);
            if (IsInTargetZone)
            {
                targetZoneImage.color = crosshairInTargetZoneColor;
                if (!crosshairInTargetZoneUI.activeSelf) crosshairInTargetZoneUI.SetActive(true);
                if (crosshairOriginUI.activeSelf) crosshairOriginUI.SetActive(false);
            }
            else
            {
                targetZoneImage.color = targetZoneOriginColor;
                if (crosshairInTargetZoneUI.activeSelf) crosshairInTargetZoneUI.SetActive(false);
                if (!crosshairOriginUI.activeSelf) crosshairOriginUI.SetActive(true);
            }
        }
        else
            IsInTargetZone = false;
    }

    /// <summary>
    /// 발사 가능 여부 설정
    /// </summary>
    /// <param name="isAvailable"></param>
    public void SetFireAvailable(bool isAvailable)
    {
        CanFire = isAvailable;
        targetZoneImage.gameObject.SetActive(isAvailable);

        if (!isAvailable)
        {
            crosshairInTargetZoneUI.SetActive(false);
            crosshairOriginUI.SetActive(true);
        }
    }
}