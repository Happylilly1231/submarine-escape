using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class SwallowWeakPoint
{
    public Transform point;
    public RectTransform circleUI;
    public bool isDestroyed;
}

public class EnergyGunController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private UnderwaterInput underwaterInput;
    [SerializeField] private UnderwaterPlayerMove underwaterPlayerMove;
    [SerializeField] private UnderwaterCameraController underwaterCameraController;
    [SerializeField] private UnderwaterPlayerStat underwaterPlayerStat;
    [SerializeField] private ItemEquipController itemEquipController;
    [SerializeField] private DeepSeaMonsterController deepSeaMonsterController;

    [Header("조준 UI")]
    [SerializeField] private GameObject aimUI; // 조준 UI
    [SerializeField] private RectTransform targetCircleUI; // 괴물 적중 범위 원
    [SerializeField] private RectTransform crosshairUI; // 조준점 UI
    [SerializeField] private RectTransform muzzleUIPoint; // 총구 위치
    [SerializeField] private RectTransform aimLineRect; // 총구와 조준점을 잇는 선 RectTransform
    [SerializeField] private Image chargeFireGuageImage; // 현재 충전 중인 발사 게이지 이미지

    [Header("발사 연출")]
    [SerializeField] private ParticleSystem gunFireExlposionParticle; // 총 발사로 일어나는 폭발 파티클
    [SerializeField] private AudioSource gunFireExlposionAudio;
    [SerializeField] private AudioClip explosionSound;

    [Header("괴물 삼키기 시 입 약화")]
    [SerializeField] private List<Transform> candidatePoints = new List<Transform>();
    [SerializeField] private SwallowWeakPoint[] swallowWeakPoints = new SwallowWeakPoint[2];
    private bool _isSwallowMode = false;
    private SwallowWeakPoint _currentSwallowTarget = null;

    private Camera _mainCam;

    private float _currentGauge = 0f; // 현재 충전 게이지 (칸)
    private float _currentFireGauge = 0f; // 현재 발사 게이지
    private int _maxCellCount = 10;
    private float _fullChargeTime = 30f;
    private bool _isEquipingGun = false;
    private bool isTargetVisible = false;
    private Image[] _gaugeCellImgs = null;

    private float _hitRadius = 100f; // UI 상에서 적중으로 판정할 반지름
    private float _chargeFireGaugeDuration = 1.5f; // 발사 게이지 완충 시간
    private float _decaySpeed = 1.5f; // 벗어났을 때 게이지 감소 속도
    private float _detectDistance = 50f; // UI가 활성화될 최대 거리
    private float _normalFov = 60f;
    private float _aimFov = 50f;
    private float _duration = 0.25f;

    public bool IsFullCharged => _currentGauge >= _maxCellCount;
    private bool CanFire => _currentFireGauge >= 1f;
    private GameObject HeldGunObj => itemEquipController.HeldItemObject;
    private Item HeldGunItemData => itemEquipController.HeldItemData;

    public bool IsAming { get; private set; } = false;

    private Sequence _checkGaugeSequence = null;
    private Tween _fovTween = null;


    private void OnEnable()
    {
        underwaterInput.holdGunEvent += OnHoldGun;
        underwaterInput.fireEvent += OnFire;
    }

    private void OnDisable()
    {
        underwaterInput.holdGunEvent -= OnHoldGun;
        underwaterInput.holdGunCancelEvent -= ExitAimMode;
        underwaterPlayerStat.OnPlayerDamaged -= ExitAimMode;
        underwaterInput.fireEvent -= OnFire;

        _fovTween?.Kill();
    }

    private void Start()
    {
        if (_mainCam == null) _mainCam = Camera.main;

        aimUI.gameObject.SetActive(false);
        chargeFireGuageImage.fillAmount = 0f;

        enabled = false;
    }

    private void Update()
    {
        if (GameManager.instance.IsPausing) return;

        // 충전 되지 않은 상태면 충전 (40초에 10칸)
        if (_currentGauge < _maxCellCount)
        {
            float chargePerSec = _maxCellCount / _fullChargeTime;
            _currentGauge = Mathf.Min(_currentGauge + chargePerSec * Time.deltaTime, _maxCellCount);
        }

        // 현재 총 아이템 들고 있는지 여부에 따른 로직
        if (_isEquipingGun) // 들고 있을 때
        {
            if (DeepSeaInventoryManager.Instance.CurrentSlotIndex == 0) // 들고 있을 때는 계속 -> 게이지 UI 갱신
            {
                UpdateGaugeUI(); // 게이지 UI 갱신
            }
            else // 내려놓았으면 -> 초기화
            {
                _isEquipingGun = false;
                _gaugeCellImgs = null;

                // 게이지 확인 코루틴이 실행 중이었다면 -> 즉시 종료
                if (_checkGaugeSequence != null)
                {
                    _checkGaugeSequence.Kill();
                    _checkGaugeSequence = null;
                }
            }
        }
        else // 들고 있지 않다가
        {
            if (DeepSeaInventoryManager.Instance.CurrentSlotIndex == 0) // 들었으면 -> 들기 시작
            {
                _isEquipingGun = true;
                Transform parentImage = HeldGunObj.GetComponentInChildren<Canvas>().transform.GetChild(0);
                _gaugeCellImgs = parentImage.GetComponentsInChildren<Image>(true)
                                            .Skip(1) // 자기 자신 제외
                                            .ToArray();
            }
        }

        // 조준 중일 때만 -> 조준 UI 업데이트 및 발사 가능 여부 판단
        if (IsAming)
        {
            if (!CheckCanAim())
            {
                ExitAimMode();
                return;
            }

            UpdateCrosshairPosition();
            UpdateAimLine();

            if (_isSwallowMode)
            {
                UpdateSwallowMode(); // 약점 2개 동시 갱신 + 해당 약점 충전
            }
            else
            {
                UpdateTargetCircle();     // 기존 단일 타겟 원 갱신
                HandleChargingAndFire(); // 기존 단일 타겟 충전
            }
        }
    }

    private bool CheckCanAim()
    {
        return !(deepSeaMonsterController.currentPattern is AmbushAttack || underwaterPlayerMove.IsDodging || underwaterPlayerMove.IsUpwardDashing || underwaterPlayerMove.IsKnockedBack || deepSeaMonsterController.IsGrabbing);
    }

    /// <summary>
    /// 게이지 UI 갱신
    /// </summary>
    private void UpdateGaugeUI()
    {
        if (_gaugeCellImgs == null)
        {
            Debug.Log("칸 이미지 배열을 찾을 수 없음");
            return;
        }

        int filledCount = Mathf.FloorToInt(_currentGauge);
        for (int i = 0; i < _gaugeCellImgs.Length; i++)
        {
            _gaugeCellImgs[i].enabled = i < filledCount;
            // 색상 변경을 원하면: gaugeCells[i].color = (i < filledCount) ? Color.cyan : Color.gray;
        }

        /* [방식 B] 각 칸 내부도 부드럽게 차오르게 하려면 (Image Type이 Filled일 때)
        for (int i = 0; i < gaugeCells.Length; i++)
        {
            gaugeCells[i].fillAmount = Mathf.Clamp01(_currentCharge - i);
        }
        */
    }

    // 조준점 UI가 마우스 커서 따라가도록 위치 갱신
    private void UpdateCrosshairPosition()
    {
        crosshairUI.position = Mouse.current.position.ReadValue();
    }

    // 캔버스 총구 UI에서 조준점 UI까지 선 잇기
    private void UpdateAimLine()
    {
        Vector2 startPos = muzzleUIPoint.position;
        Vector2 endPos = crosshairUI.position;
        Vector2 direction = endPos - startPos;

        // 시작점을 총구 UI 위치에 고정
        aimLineRect.position = startPos;

        // 두 점 사이의 거리만큼 가로 길이(Width) 조절 (해상도 스케일 보정 포함)
        float distance = direction.magnitude;
        float scaleFactor = aimLineRect.lossyScale.x > 0 ? aimLineRect.lossyScale.x : 1f;
        aimLineRect.sizeDelta = new Vector2(distance / scaleFactor, aimLineRect.sizeDelta.y);

        // 조준점 방향으로 각도 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        aimLineRect.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 괴물 거리 체크 및 괴물 위치에 원 배치
    private void UpdateTargetCircle()
    {
        float distance = Vector3.Distance(_mainCam.transform.position, deepSeaMonsterController.transform.position);
        Vector3 screenPos = _mainCam.WorldToScreenPoint(deepSeaMonsterController.transform.position);
        Debug.Log(distance + " / " + screenPos);

        bool isOnScreen = screenPos.z > 0 &&
                          screenPos.x >= 0 && screenPos.x <= Screen.width &&
                          screenPos.y >= 0 && screenPos.y <= Screen.height;

        // 일정 거리 이하이면서 카메라 앞쪽(z > 0), 화면 안에 있을 때만 표시
        if (distance <= _detectDistance && isOnScreen)
        {
            SetTargetUIActive(true);
            screenPos.z = 0f;
            targetCircleUI.position = screenPos;
        }
        else
        {
            SetTargetUIActive(false);
        }
    }

    private void SetTargetUIActive(bool active)
    {
        isTargetVisible = active;
        if (targetCircleUI.gameObject.activeSelf != active)
        {
            targetCircleUI.gameObject.SetActive(active);
        }
    }

    // 조준점 진입 판정 -> 게이지 충전/감소 -> 발사
    private void HandleChargingAndFire()
    {
        bool isInsideCircle = false;

        // 괴물 원이 떠 있을 때, 조준점과 괴물 원 중심 사이의 UI(픽셀) 거리 계산
        if (isTargetVisible && crosshairUI != null && targetCircleUI != null)
        {
            float uiDistance = Vector2.Distance(crosshairUI.position, targetCircleUI.position);
            isInsideCircle = uiDistance <= _hitRadius;
        }

        // 게이지 증감
        if (isInsideCircle)
        {
            _currentFireGauge += (1f / _chargeFireGaugeDuration) * Time.unscaledDeltaTime;
        }
        else
        {
            _currentFireGauge -= _decaySpeed * Time.unscaledDeltaTime;
        }

        _currentFireGauge = Mathf.Clamp01(_currentFireGauge);
        Debug.Log("_currentFireGauge: " + _currentFireGauge);

        // 게이지 UI 갱신
        if (chargeFireGuageImage != null)
        {
            chargeFireGuageImage.fillAmount = _currentFireGauge;
        }
    }

    /// <summary>
    /// 우클릭
    /// </summary>
    private void OnHoldGun()
    {
        DeepSeaInventoryManager.Instance.SelectSlot(0); // 1번째 아이템인 에너지 건 슬롯 선택 (EquipItem)

        if (IsFullCharged)
        {
            EnterAimMode(); // 조준 모드 활성화
        }
        else
        {
            CheckCurrentGauge(); // 게이지 확인
        }
    }

    /// <summary>
    /// 좌클릭 -> 발사
    /// </summary>
    private void OnFire()
    {
        if (!CanFire) return;

        Debug.Log("발사!");

        // 발사 게이지 초기화
        _currentFireGauge = 0f;
        if (chargeFireGuageImage != null)
            chargeFireGuageImage.fillAmount = 0f;

        if (_isSwallowMode)
        {
            // 현재 충전된 약점 1개 파괴 처리
            if (_currentSwallowTarget != null && !_currentSwallowTarget.isDestroyed)
            {
                _currentSwallowTarget.isDestroyed = true;

                if (_currentSwallowTarget.circleUI != null)
                    _currentSwallowTarget.circleUI.gameObject.SetActive(false);

                Vector3 hitPos = _currentSwallowTarget.point != null
                    ? _currentSwallowTarget.point.position
                    : deepSeaMonsterController.transform.position;

                gunFireExlposionParticle.transform.position = hitPos;
                gunFireExlposionParticle.Play();
                gunFireExlposionAudio.PlayOneShot(explosionSound);
            }

            _currentSwallowTarget = null;

            // 아직 남은 약점이 있다면 삼키기 상태 유지 (남은 1개 바로 이어서 공략)
            bool hasRemaining = swallowWeakPoints.Any(wp => wp != null && !wp.isDestroyed && wp.point != null);
            if (hasRemaining)
            {
                deepSeaMonsterController.OnSwallowWeakPointHit();
                return;
            }

            // 2개 모두 파괴 완료 -> 삼키기 모드 종료 후 일반 약화/종료 처리로 진행
            EndSwallowWeakPoints();
        }
        else
        {
            // 일반 패턴: 괴물 위치에서 폭발 파티클 & 소리 재생
            gunFireExlposionParticle.transform.position = deepSeaMonsterController.transform.position;
            gunFireExlposionParticle.Play();
            gunFireExlposionAudio.PlayOneShot(explosionSound);
        }

        // 일반 발사 또는 삼키기 약점 2개 모두 격파 시 공통 종료 처리
        _currentGauge = 0f;
        UpdateGaugeUI();

        ExitAimMode(); // 조준 모드 비활성화

        if (!deepSeaMonsterController.IsWeakenFully)
            deepSeaMonsterController.Weaken(); // 괴물 약화
    }

    /// <summary>
    /// 현재 게이지 확인
    /// </summary>
    public void CheckCurrentGauge()
    {
        if (_checkGaugeSequence != null) return; // 중복 실행 금지

        _checkGaugeSequence = DOTween.Sequence();
        Debug.Log("HeldGunObj: " + HeldGunObj + " / " + DeepSeaInventoryManager.Instance.CurrentSlotIndex);
        _checkGaugeSequence.Append(HeldGunObj.transform.DOLocalMove(new Vector3(0.5f, -0.35f, 0.5f), 0.3f));
        _checkGaugeSequence.Join(HeldGunObj.transform.DOLocalRotate(new Vector3(0f, 120f, 10f), 0.3f));
        _checkGaugeSequence.AppendInterval(2f);
        _checkGaugeSequence.Append(HeldGunObj.transform.DOLocalRotate(HeldGunItemData.GripRotationOffset, 0.3f));
        _checkGaugeSequence.Join(HeldGunObj.transform.DOLocalMove(HeldGunItemData.GripPositionOffset, 0.3f));
        _checkGaugeSequence.OnComplete(() =>
        {
            DeepSeaInventoryManager.Instance.UnselectSlot(); // 슬롯 선택 해제 (UnequipItem)
            _checkGaugeSequence = null;
        });

    }

    /// <summary>
    /// 조준 모드 활성화
    /// </summary>
    private void EnterAimMode()
    {
        if (!CheckCanAim())
            return;

        Debug.Log("조준 모드 활성화");

        if (_checkGaugeSequence != null)
        {
            _checkGaugeSequence.Kill();
            _checkGaugeSequence = null;
        }

        IsAming = true;
        FocusManager.Instance.PushFocusState(GameFocusState.DeepSeaControlAim);

        Time.timeScale = 0.3f; // 슬로우 시작
        SetAimZoom(true);

        underwaterInput.holdGunCancelEvent += ExitAimMode;
        underwaterPlayerStat.OnPlayerDamaged += ExitAimMode;
        GameManager.instance.OnBeforePause += ExitAimMode;

        aimUI.SetActive(true);

        HeldGunObj.transform.localPosition = new Vector3(0f, -0.6f, 0.4f);
        HeldGunObj.transform.localRotation = Quaternion.Euler(HeldGunItemData.GripRotationOffset);
    }

    /// <summary>
    /// 조준 모드 비활성화
    /// </summary>
    public void ExitAimMode()
    {
        if (!IsAming || FocusManager.Instance.CurrentFocusState != GameFocusState.DeepSeaControlAim) return;

        Debug.Log("조준 모드 비활성화");

        IsAming = false;

        FocusManager.Instance.PopFocusState();

        Time.timeScale = 1f; // 슬로우 끝
        SetAimZoom(false);

        underwaterInput.holdGunCancelEvent -= ExitAimMode;
        underwaterPlayerStat.OnPlayerDamaged -= ExitAimMode;
        GameManager.instance.OnBeforePause -= ExitAimMode;

        aimUI.SetActive(false);

        HeldGunObj.transform.localPosition = HeldGunItemData.GripPositionOffset;
        HeldGunObj.transform.localRotation = Quaternion.Euler(HeldGunItemData.GripRotationOffset);
    }

    public void SetAimZoom(bool isAiming)
    {
        float targetFov = isAiming ? _aimFov : _normalFov;
        _fovTween?.Kill(); // 중복 실행 방지
        _fovTween = _mainCam.DOFieldOfView(targetFov, _duration).SetEase(Ease.OutQuad); // 지정된 시간(duration) 동안 부드럽게 FOV 변경
    }

    #region 삼키기 입 약화

    // 삼키기 시작 시 호출 (후보 중 서로 다른 2개만 뽑아서 배치)
    public void BeginSwallowWeakPoints()
    {
        _isSwallowMode = true;
        _currentSwallowTarget = null;
        _currentFireGauge = 0f;
        if (chargeFireGuageImage != null) chargeFireGuageImage.fillAmount = 0f;

        List<Transform> picked = candidatePoints
            .Where(p => p != null)
            .OrderBy(_ => Random.value)
            .Take(2)
            .ToList();

        for (int i = 0; i < swallowWeakPoints.Length; i++)
        {
            swallowWeakPoints[i].point = i < picked.Count ? picked[i] : null;
            swallowWeakPoints[i].isDestroyed = swallowWeakPoints[i].point == null;
            if (swallowWeakPoints[i].circleUI != null)
                swallowWeakPoints[i].circleUI.gameObject.SetActive(false);
        }

        _fullChargeTime = 0.5f;
        _chargeFireGaugeDuration = 0.5f;
    }

    public void EndSwallowWeakPoints()
    {
        _isSwallowMode = false;
        _currentSwallowTarget = null;
        _currentFireGauge = 0f;
        if (chargeFireGuageImage != null) chargeFireGuageImage.fillAmount = 0f;

        foreach (var wp in swallowWeakPoints)
        {
            wp.isDestroyed = true;
            if (wp.circleUI != null)
                wp.circleUI.gameObject.SetActive(false);
        }

        if (IsAming)
            ExitAimMode(); // 조준 모드 비활성화

        deepSeaMonsterController.currentPattern.OnAttackMiss();
    }

    // 삼키기 모드일 때 살아있는 약점들 UI 위치 갱신 + 충전
    private void UpdateSwallowMode()
    {
        SwallowWeakPoint insideTarget = null;

        foreach (var wp in swallowWeakPoints)
        {
            if (wp.isDestroyed || wp.point == null || wp.circleUI == null)
            {
                if (wp.circleUI != null) wp.circleUI.gameObject.SetActive(false);
                continue;
            }

            float dist = Vector3.Distance(_mainCam.transform.position, wp.point.position);
            Vector3 screenPos = _mainCam.WorldToScreenPoint(wp.point.position);
            bool visible = dist <= _detectDistance &&
                           screenPos.z > 0f &&
                           screenPos.x >= 0f && screenPos.x <= Screen.width &&
                           screenPos.y >= 0f && screenPos.y <= Screen.height;

            wp.circleUI.gameObject.SetActive(visible);
            if (!visible) continue;

            screenPos.z = 0f;
            wp.circleUI.position = screenPos;

            if (Vector2.Distance(crosshairUI.position, wp.circleUI.position) <= _hitRadius)
                insideTarget = wp;
        }

        float dt = Time.unscaledDeltaTime;
        if (insideTarget != null)
        {
            if (_currentSwallowTarget != insideTarget)
            {
                _currentSwallowTarget = insideTarget;
                _currentFireGauge = 0f; // 다른 약점으로 넘어가면 충전 초기화
            }
            _currentFireGauge += (1f / _chargeFireGaugeDuration) * dt;
        }
        else
        {
            _currentFireGauge -= _decaySpeed * dt;
        }

        _currentFireGauge = Mathf.Clamp01(_currentFireGauge);
        if (chargeFireGuageImage != null)
            chargeFireGuageImage.fillAmount = _currentFireGauge;
    }

    #endregion
}
