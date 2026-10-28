using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 플레이어 히든 능력
/// </summary>
public class PlayerAbility : MonoBehaviour
{
    [SerializeField] private Volume monsterAbilityVolume; // 괴물일 때 활성화할 볼륨
    public ColorAdjustments monsterVolumeColorAdjustments;

    public bool IsTransformed { get; private set; } = false; // 현재 괴물로 변신했는지 여부
    // public bool CanTransform { get; set; } = false; // 현재 변신 가능 여부
    private int _transformLockCount = 0;
    public bool CanTransform => _transformLockCount == 0;

    private InputAction _transformationAction;
    private InputAction _attackAction;

    private PlayerMoveBase _playerMoveBase;
    private CharacterController _playerCC; // 플레이어 캐틱터 컨트롤러 (잠수함씬 전용)

    [Header("변신")]
    [SerializeField] private GameObject playerGeo; // 플레이어 외형
    public GameObject monsterGeo; // 괴물 외형
    [SerializeField] private GameObject monsterModel; // 괴물 모델
    [SerializeField] private Transform playerCameraPos; // 플레이어 카메라 위치
    [SerializeField] private Transform monsterCameraPos; // 괴물 카메라 위치
    [SerializeField] private Transform monsterAttackCameraPos; // 괴물 공격 카메라 위치
    [SerializeField] private Animator playerAnimator; // 플레이어 애니메이터
    [SerializeField] private Animator monsterAnimator; // 괴물 애니메이터

    [Header("참조")]
    public GameObject abilityUI; // 능력 UI
    [SerializeField] private GameObject chargingUI; // 차징 UI
    [SerializeField] private Image chargingGaugeImage; // 차징 게이지 이미지
    [SerializeField] private TextMeshProUGUI chargingActionText; // 차징 액션 텍스트
    [SerializeField] private ParticleSystem explosionParticle; // 폭발 파티클
    public ParticleSystem ExplosionParticle => explosionParticle;
    [SerializeField] private Transform monsterTransform; // 괴물 트랜스폼

    [Header("오디오")]
    [SerializeField] private AudioClip destroySound;

    [Header("우하단 UI")]
    [SerializeField] private TextMeshProUGUI attackCoolDownText;
    [SerializeField] private GameObject attackIconImage;
    [SerializeField] private GameObject transformLockUI;
    [SerializeField] private GameObject transformUI;
    [SerializeField] private GameObject monsterImage;
    [SerializeField] private GameObject humanImage;
    [SerializeField] private GameObject attackUnavailableImage;
    [SerializeField] private GameObject transformUnavailableImage;


    private GameObject currentDestroyObj = null; // 현재 파괴할 오브젝트
    private string currentDestroyObjName = ""; // 현재 파괴할 오브젝트 이름
    private float chargingTime = 5f; // 풀차징하는데 걸리는 시간
    private bool _isCharging = false; // 차징 중 여부
    public bool IsUsingAttackAbility { get; private set; } = false; // 공격 능력 사용 중 여부
    private Coroutine currentChargingCoroutine = null; // 현재 차징 코루틴
    private float attackDistance = 2f; // 공격 거리
    private InnerMonsterController _innerMonsterController;
    private DeepSeaMonsterController _deepSeaMonsterController;

    private Camera _mainCamera; // 혹은 Cinemachine 가상 카메라 컴포넌트
    private float _normalFOV = 60f; // 기본 시야각
    private float _maxChargingFOV = 80f; // 차징 완료 시 도달할 최대 시야각
    private float _restoreDuration = 0.15f; // 쾅 했을 때 돌아오는 속도 (매우 빠르게)
    private Tween _fovTween; // 진행 중인 FOV 트윈 관리용

    // 쿨타임
    private float _attackCoolDown = 5f; // 쿨타임 시간
    private float _nextCanAttackTime = 0f; // 다음 공격이 가능한 시간

    private FinalPatternManager finalPatternManager;

    private void Awake()
    {
        _playerMoveBase = GetComponent<PlayerMoveBase>();
        _playerCC = GetComponent<CharacterController>();
        _innerMonsterController = monsterTransform.GetComponent<InnerMonsterController>();
        _deepSeaMonsterController = monsterTransform.GetComponent<DeepSeaMonsterController>();
        finalPatternManager = FindAnyObjectByType<FinalPatternManager>();
    }

    private void Start()
    {
        if (attackCoolDownText == null)
        {
            Debug.Log("우하단 UI 연결 안됨!!!");
            return;
        }
        attackUnavailableImage.SetActive(true);
        transformUnavailableImage.SetActive(true);

        monsterModel.SetActive(GameManager.instance.IsAbilityActive); // 괴물 모델 활성화 여부 설정

        monsterAbilityVolume.profile.TryGet(out monsterVolumeColorAdjustments);

        // 현재 능력 사용 여부에 따라 이 컴포넌트 활성화 여부 결정
        enabled = GameManager.instance.IsAbilityActive;
        if (!GameManager.instance.IsAbilityActive)
        {
            PlayerManager.Instance.playerStat.SetMonsterStatUI(false); // 기본 스탯 UI로 변경
            abilityUI.SetActive(false);
            return;
        }

        _mainCamera = Camera.main;

        // 입력
        _transformationAction = PlayerManager.Instance.playerInput.actions.FindAction("Transformation"); // 변신 키 가져오기
        _attackAction = PlayerManager.Instance.playerInput.actions.FindAction("Attack"); // 공격 키 가져오기

        // 이벤트 함수 연결
        _transformationAction.performed += OnTransformation;
        _attackAction.performed += OnAttackStarted;
        _attackAction.canceled += OnAttackCanceled;

        // 시작 시 -> 무조건 괴물 상태로 변신 (잠수함 내에서나 심해에서나)
        TransformToMonster(true);

        if (SubmarineInGameManager.instance != null) // 잠수함 내
            PlayerManager.Instance.playerStat.StartSanityInstability(); // 정신 불안정 상태로 시작
        else // 심해씬
        {
            PlayerManager.Instance.playerStat.StopSanityInstability(); // 정신 안정 상태로 시작 (최대 수치에서)
            UpdateChargingTime(true); // 심해에서는 손전등도 있고 심해 물에 닿아있다보니까 공격력 높아진 걸로 간주해서 차징 시간 줄임
        }
    }

    private void Update()
    {
        UpdateDownRightUI(); // 우하단 UI 업데이트
    }

    /// <summary>
    /// 우하단 UI 업데이트
    /// </summary>
    public void UpdateDownRightUI()
    {
        if (attackCoolDownText == null)
        {
            return;
        }

        // 공격 쿨타임 업데이트
        float remainingTime = _nextCanAttackTime - Time.time; // 남은 쿨타임 계산
        if (remainingTime > 0f)
        {
            if (!attackCoolDownText.gameObject.activeSelf)
            {
                attackCoolDownText.gameObject.SetActive(true);
                attackIconImage.SetActive(false);
            }
            attackCoolDownText.text = $"{remainingTime:F0}s";
        }
        else
        {
            if (attackCoolDownText.gameObject.activeSelf)
            {
                attackCoolDownText.gameObject.SetActive(false);
                attackIconImage.SetActive(true);
            }
        }

        // 공격 가능 여부에 따른 UI 업데이트
        if (!IsTransformed || IsUsingAttackAbility || (finalPatternManager != null && finalPatternManager.IsPatternRunning) || Time.time < _nextCanAttackTime) // 불가능
        {
            if (!attackUnavailableImage.activeSelf) attackUnavailableImage.SetActive(true);
        }
        else
        {
            if (attackUnavailableImage.activeSelf) attackUnavailableImage.SetActive(false);
        }

        // 변신 가능 여부에 따른 UI 업데이트
        if (!CanTransform || FocusManager.Instance.CurrentFocusState != GameFocusState.None) // 불가능
        {
            if (!transformUnavailableImage.activeSelf) transformUnavailableImage.SetActive(true);
        }
        else
        {
            if (transformUnavailableImage.activeSelf) transformUnavailableImage.SetActive(false);
        }
    }

    public void UnlockTransformUI()
    {
        transformLockUI.SetActive(false);
        transformUI.SetActive(true);
    }

    private void OnEnable()
    {
        if (GameManager.instance.IsAbilityActive)
        {
            if (LightingManager.instance != null) LightingManager.instance.OnLightChanged += UpdateChargingTime;
            if (PlayerManager.Instance != null) PlayerManager.Instance.playerStat.OnPlayerDamaged += ChargingFail;
        }
    }

    private void OnDisable()
    {
        if (GameManager.instance.IsAbilityActive)
        {
            // 구독 해제
            _transformationAction.performed -= OnTransformation;
            _attackAction.performed -= OnAttackStarted;
            _attackAction.canceled -= OnAttackCanceled;
            if (LightingManager.instance != null) LightingManager.instance.OnLightChanged -= UpdateChargingTime;
            if (PlayerManager.Instance != null) PlayerManager.Instance.playerStat.OnPlayerDamaged -= ChargingFail;

            if (_isCharging) ChargingFail();
        }
    }

    #region 입력 이벤트 함수

    /// <summary>
    /// 변신 키 입력 시 -> 현재 형태 전환
    /// </summary>
    public void OnTransformation(InputAction.CallbackContext context)
    {
        if (!CanTransform) return; // 변신 불가 상태일 때는 변신 불가

        if (FocusManager.Instance.CurrentFocusState == GameFocusState.None) // 일반 포커스일 때만 가능 (심해씬에서도 변신 불가)
            ToggleCurrentForm();
    }

    /// <summary>
    /// R키 입력 시 -> 차징 시작
    /// </summary>
    /// <param name="context"></param>
    public void OnAttackStarted(InputAction.CallbackContext context)
    {
        if (!IsTransformed) return; // 변신해있지 않을 때 -> 불가능
        if (IsUsingAttackAbility) return; // 공격 능력 사용 중 -> 불가능
        if (finalPatternManager != null && finalPatternManager.IsPatternRunning) return; // 심해 최종 패턴 진행 중 -> 불가능

        if (Time.time < _nextCanAttackTime)
        {
            Debug.Log("쿨타임 중");
            return; // 다음 공격 가능한 시간이 아닐 때 (쿨타임 지나지 않았을 때) -> 불가능
        }

        ChargingStart(); // 차징 시작
    }

    /// <summary>
    /// R키 취소 -> 차징 실패
    /// </summary>
    /// <param name="context"></param>
    public void OnAttackCanceled(InputAction.CallbackContext context)
    {
        if (!IsTransformed) return; // 변신해있지 않을 때 -> 불가능

        ChargingFail(); // 차징 실패
    }

    #endregion

    /// <summary>
    /// 변신 금지 상태 추가 (회피 시작, 정신 불안정 시작 등)
    /// </summary>
    public void AddTransformLock()
    {
        _transformLockCount++;
    }

    /// <summary>
    /// 변신 금지 상태 해제 (회피 종료, 정신 불안정 종료 등)
    /// </summary>
    public void RemoveTransformLock()
    {
        _transformLockCount--;
        if (_transformLockCount < 0) _transformLockCount = 0;
    }

    /// <summary>
    /// 괴물로 변신
    /// </summary>
    public void TransformToMonster(bool isImmediately = false)
    {
        Debug.Log("[능력] 괴물로 변신");
        IsTransformed = true;

        if (humanImage != null)
        {
            humanImage.SetActive(true);
            monsterImage.SetActive(false);
        }

        // 괴물 능력 볼륨 활성화
        if (isImmediately)
            monsterAbilityVolume.weight = 1f;
        else
            DOTween.To(() => monsterAbilityVolume.weight, x => monsterAbilityVolume.weight = x, 1f, 0.5f);

        // 외형 활성화 설정
        playerGeo.SetActive(false);
        monsterGeo.SetActive(true);

        // 애니메이터 설정
        _playerMoveBase.SetAnimator(monsterAnimator);
        _playerMoveBase.animator = monsterAnimator;

        PlayerManager.Instance.playerCameraControllerBase.SetCameraPos(monsterCameraPos); // 카메라 위치 설정
        PlayerManager.Instance.playerStat.SetMonsterStatUI(true); // 괴물 스탯 UI로 변경

        UpdateColliderSize(); // 콜라이더 크기 변경

        if (SubmarineInGameManager.instance != null) // 본씬
        {
            // UI 변경
            SubmarineInGameManager.instance.InventoryManager.CloseInventory(); // 인벤토리 비활성화
        }
        else // 심해씬
        {
            // 카메라 위치 설정
        }
    }

    /// <summary>
    /// 변신 해제
    /// </summary>
    public void TransformOff()
    {
        Debug.Log("[능력] 사람으로 돌아옴");
        IsTransformed = false;

        if (monsterImage != null)
        {
            monsterImage.SetActive(true);
            humanImage.SetActive(false);
        }

        // 괴물 능력 볼륨 비활성화
        DOTween.To(() => monsterAbilityVolume.weight, x => monsterAbilityVolume.weight = x, 0f, 0.5f);

        // 외형 활성화 설정
        playerGeo.SetActive(true);
        monsterGeo.SetActive(false);

        // 애니메이터 설정
        _playerMoveBase.SetAnimator(playerAnimator);
        _playerMoveBase.animator = playerAnimator;
        if (SubmarineInGameManager.instance != null)
        {
            _playerMoveBase.animator.SetLayerWeight(1, 0f); // 인트로 레이어 끄기
            _playerMoveBase.animator.SetLayerWeight(2, 1f); // 플레이어 레이어 켜기
        }

        PlayerManager.Instance.playerCameraControllerBase.SetCameraPos(playerCameraPos); // 카메라 위치 설정
        PlayerManager.Instance.playerStat.SetMonsterStatUI(false); // 기본 스탯 UI로 변경

        UpdateColliderSize(); // 콜라이더 크기 변경

        if (SubmarineInGameManager.instance != null) // 본씬
        {
            // UI 변경
            SubmarineInGameManager.instance.InventoryManager.OpenInventory(); // 인벤토리 활성화
        }
        else // 심해씬
        {
            // 카메라 위치 설정
        }
    }

    /// <summary>
    /// 콜라이더 사이즈 변경
    /// </summary>
    /// <param name="isTransform"></param>
    public void UpdateColliderSize()
    {
        float yValue = IsTransformed ? 1.1f : 0.9f;
        float height = IsTransformed ? 2.4f : 1.85f;

        if (SubmarineInGameManager.instance != null) // 본씬
        {
            // 캐릭터 컨트롤러의 크기 변경
            Vector3 centerPos = _playerCC.center;
            centerPos.y = yValue;
            _playerCC.center = centerPos;
            _playerCC.height = height;
        }
        else // 심해씬
        {
            // 모델 콜라이더 크기 변경
            CapsuleCollider modelCollider = PlayerManager.Instance.playerMoveBase.GetComponent<DeepSeaPlayerMove>().modelCollider;
            Vector3 centerPos = modelCollider.center;
            centerPos.y = yValue;
            modelCollider.center = centerPos;
            modelCollider.height = height;
        }
    }

    /// <summary>
    /// 현재 형태 전환
    /// </summary>
    public void ToggleCurrentForm()
    {
        if (IsTransformed)
            TransformOff();
        else
            TransformToMonster();
    }

    /// <summary>
    /// 파괴 시전
    /// </summary>
    /// <param name="destroyObj">파괴할 오브젝트</param>
    public void TryDestroy(GameObject destroyObj, string destroyObjName)
    {
        currentDestroyObj = destroyObj;
        currentDestroyObjName = destroyObjName;
    }

    /// <summary>
    /// 차징 시작
    /// </summary>
    private void ChargingStart()
    {
        Debug.Log("차징 시작");

        // 만약 플레이어 상호작용 스크립트에서 현재 감지한 가구가 있고, 그게 능력으로 파괴 가능한 가구라면
        if (PlayerManager.Instance.playerInteractor is PlayerInteractor playerInteractor && playerInteractor.CurrentFurniture is IAbilityInteractable abilityInteractable && abilityInteractable.CanDestroy)
        {
            abilityInteractable.InteractWithAbility();
        }

        IsUsingAttackAbility = true;
        AddTransformLock();
        _isCharging = true;
        // monsterGeo.SetActive(false); // 괴물 외형 비활성화
        chargingUI.SetActive(true);
        chargingGaugeImage.fillAmount = 0f;

        PlayerManager.Instance.playerMoveBase.SetMoveable(false); // 플레이어 이동 정지
        PlayerManager.Instance.playerCameraControllerBase.SetInputEnabled(false); // 플레이어 카메라 인풋 금지 (= 회전 금지)

        if (PlayerManager.Instance.playerInteractor != null)
        {
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(false); // 상호작용 UI 비활성화
            PlayerManager.Instance.playerInteractor.ClearDetectionText(); // 상호작용 텍스트 초기화
        }

        if (currentDestroyObj == null)
            chargingActionText.text = "공격 차징 중";
        else
            chargingActionText.text = $"[ {currentDestroyObjName} ] 파괴 차징 중";

        // 1. 기존 FOV 연출이 있다면 종료하고 초기화
        _fovTween?.Kill();
        _mainCamera.fieldOfView = _normalFOV;

        // 2. 차징 시간(chargingTime) 동안 시야각을 최대치까지 서서히 넓힘
        _fovTween = DOTween.To(() => _mainCamera.fieldOfView, x => _mainCamera.fieldOfView = x, _maxChargingFOV, chargingTime)
            .SetEase(Ease.InQuad); // 뒤로 갈수록 가속도가 붙어 넓어지는 느낌 제공

        currentChargingCoroutine = StartCoroutine(ChargingCoroutine());
    }

    /// <summary>
    /// 차징 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator ChargingCoroutine()
    {
        float timer = 0f;
        chargingGaugeImage.fillAmount = 0f;

        while (timer <= chargingTime)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / chargingTime);

            chargingGaugeImage.fillAmount = Mathf.Clamp01(timer / chargingTime);

            if (timer > 1f)
            {
                // 차징 시간이 지날수록 카메라 흔들림을 점점 강하게 부여 (제곱 곡선으로 후반에 확 격렬해짐)
                float currentShake = Mathf.Lerp(0.01f, 0.1f, progress * progress);
                PlayerManager.Instance.playerCameraControllerBase.SetShakeIntensity(currentShake);
            }

            yield return null;
        }

        // 차징 완료 직후 흔들림 끄기
        PlayerManager.Instance.playerCameraControllerBase.SetShakeIntensity(0f);

        _isCharging = false;
        currentChargingCoroutine = null;
        chargingUI.SetActive(false);

        // 3. 풀차징 완료 후 쾅! 할 때 시야각을 원상 복구 (아주 빠르게 쾅 당겨짐)
        _fovTween?.Kill();
        _fovTween = DOTween.To(() => _mainCamera.fieldOfView, x => _mainCamera.fieldOfView = x, _normalFOV, _restoreDuration)
            .SetEase(Ease.OutBack); // 쿵 하는 충격량을 주며 복구되도록 OutBack 이즈 적용

        TryAttack(); // 풀차징 상태이므로 공격 시도
    }

    /// <summary>
    /// 차징 실패
    /// </summary>
    private void ChargingFail()
    {
        if (!_isCharging) return;

        Debug.Log("차징 실패");

        StopCoroutine(currentChargingCoroutine); // 현재 차징 코루틴 종료
        currentChargingCoroutine = null;
        _isCharging = false;
        IsUsingAttackAbility = false;
        RemoveTransformLock();
        // monsterGeo.SetActive(true); // 괴물 외형 활성화

        currentDestroyObj = null;
        currentDestroyObjName = "";

        PlayerManager.Instance.playerMoveBase.SetMoveable(true); // 플레이어 이동 가능
        PlayerManager.Instance.playerCameraControllerBase.SetInputEnabled(true); // 플레이어 카메라 인풋 가능 (= 회전 가능)

        chargingUI.SetActive(false);
        if (PlayerManager.Instance.playerInteractor != null)
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(true); // 상호작용 UI 활성화

        // 4. 차징 중 취소/실패 시에도 시야각을 부드럽게 원래대로 복구 (툭 끊기지 않게)
        _fovTween?.Kill();
        _fovTween = DOTween.To(() => _mainCamera.fieldOfView, x => _mainCamera.fieldOfView = x, _normalFOV, _restoreDuration)
            .SetEase(Ease.OutCubic);

        PlayerManager.Instance.playerCameraControllerBase.SetShakeIntensity(0f); // 흔들림 없애기
    }

    /// <summary>
    /// 빛 여부에 따라 차징 시간 업데이트
    /// </summary>
    /// <param name="isTurnOn">빛 여부</param>
    private void UpdateChargingTime(bool isTurnOn)
    {
        chargingTime = isTurnOn ? 1f : 2f;
    }

    /// <summary>
    /// 공격 시도
    /// </summary>
    private void TryAttack()
    {
        StartCoroutine(TryAttackCoroutine());
    }

    private IEnumerator TryAttackCoroutine()
    {
        // 카메라 위치 설정
        PlayerManager.Instance.playerCameraControllerBase.SetCameraPos(monsterAttackCameraPos);

        PlayerCameraController playerCameraController = PlayerManager.Instance.playerCameraController;

        float originalXRotation = 0f;
        float originalPlayerYAngle = 0f;
        if (playerCameraController != null)
        {
            originalXRotation = playerCameraController.xRotation;
            originalPlayerYAngle = playerCameraController.transform.eulerAngles.y;
            Vector3 lookDownPos = transform.position + transform.forward * 0.5f + Vector3.up * 2f;
            playerCameraController.RotateToTargetPos(lookDownPos, 1f, Ease.OutQuart);
        }

        _playerMoveBase.animator.SetLayerWeight(1, 1f);
        _playerMoveBase.animator.Play("Destroy", 1, 0.25f);

        yield return new WaitForSeconds(0.25f);

        // 카메라 위치 설정
        PlayerManager.Instance.playerCameraControllerBase.SetCameraPos(monsterCameraPos);

        explosionParticle.Play(); // 폭발 파티클 한번 재생
        AudioManager.Instance.PlayGlobalOneShot(destroySound);
        _playerMoveBase.animator.SetLayerWeight(1, 0f);

        if (currentDestroyObj != null)
        {
            DestroyTargetObj();
        }
        else
        {
            float distanceToMonster = Vector3.Distance(_playerMoveBase.transform.position, monsterTransform.position);
            if (distanceToMonster <= attackDistance)
            {
                // 플레이어 정면 기준 시야각 체크
                Vector3 dirToMonster = (monsterTransform.position - _playerMoveBase.transform.position).normalized;
                float angleToMonster = Vector3.Angle(_playerMoveBase.transform.forward, dirToMonster);

                // 플레이어 정면 기준 좌우 60도 이내에 있어야 공격 가능
                if (angleToMonster <= 60f)
                    AttackMonster();
            }
        }

        yield return new WaitForSeconds(1f);
        if (playerCameraController != null) playerCameraController.RotateToYAngle(originalPlayerYAngle, 1f, originalXRotation, Ease.InQuad);
        yield return new WaitForSeconds(1f);

        IsUsingAttackAbility = false;
        RemoveTransformLock();
        // monsterGeo.SetActive(true); // 괴물 외형 활성화

        PlayerManager.Instance.playerMoveBase.SetMoveable(true); // 플레이어 이동 가능
        PlayerManager.Instance.playerCameraControllerBase.SetInputEnabled(true); // 플레이어 카메라 인풋 가능 (= 회전 가능)

        if (PlayerManager.Instance.playerInteractor != null)
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(true); // 상호작용 UI 활성화

        _nextCanAttackTime = Time.time + _attackCoolDown; // 다음 공격 가능 시간 업데이트
    }

    private void DestroyTargetObj()
    {
        Debug.Log("[능력] " + currentDestroyObj + " 파괴!");
        currentDestroyObj.SetActive(false);
        currentDestroyObj = null;
        currentDestroyObjName = "";
    }

    /// <summary>
    /// 괴물 공격
    /// </summary>
    private void AttackMonster()
    {
        Debug.Log("괴물 공격!");
        if (_innerMonsterController != null)
            _innerMonsterController.TakeDamage(20f);
        else if (_deepSeaMonsterController != null)
            _deepSeaMonsterController.TakeDamage(20f);
    }
}
