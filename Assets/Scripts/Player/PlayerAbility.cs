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
    [Header("참조")]
    public AbilityUI abilityUI; // 능력 UI
    [SerializeField] private Volume monsterAbilityVolume; // 괴물일 때 활성화할 볼륨
    public ColorAdjustments monsterVolumeColorAdjustments;

    [Header("변신")]
    [SerializeField] private GameObject playerGeo; // 플레이어 외형
    public GameObject monsterGeo; // 괴물 외형
    [SerializeField] private GameObject monsterModel; // 괴물 모델
    [SerializeField] private Transform playerCameraPos; // 플레이어 카메라 위치
    [SerializeField] private Transform monsterCameraPos; // 괴물 카메라 위치
    [SerializeField] private Transform monsterAttackCameraPos; // 괴물 공격 카메라 위치
    [SerializeField] private Animator playerAnimator; // 플레이어 애니메이터
    [SerializeField] private Animator monsterAnimator; // 괴물 애니메이터

    [Header("차징")]
    [SerializeField] private ParticleSystem explosionParticle; // 폭발 파티클
    public ParticleSystem ExplosionParticle => explosionParticle;
    [SerializeField] private Transform monsterTransform; // 괴물 트랜스폼
    [SerializeField] private ShockwaveTrigger shockwaveTrigger; // 충격파 (심해 전용)

    [Header("오디오")]
    [SerializeField] private AudioClip destroySound;

    public bool IsTransformed { get; private set; } = false; // 현재 괴물로 변신했는지 여부
    // public bool CanTransform { get; set; } = false; // 현재 변신 가능 여부
    private int _transformLockCount = 0;
    public bool CanTransform => _transformLockCount == 0;

    private InputAction _transformationAction;
    private InputAction _attackAction;

    private PlayerMoveBase _playerMoveBase;
    private CharacterController _playerCC; // 플레이어 캐틱터 컨트롤러 (잠수함씬 전용)

    private GameObject currentDestroyObj = null; // 현재 파괴할 오브젝트
    private string currentDestroyObjName = ""; // 현재 파괴할 오브젝트 이름
    private float chargingTime = 5f; // 풀차징하는데 걸리는 시간
    private bool _isCharging = false; // 차징 중 여부
    public bool IsUsingAttackAbility { get; private set; } = false; // 공격 능력 사용 중 여부
    private Coroutine currentChargingCoroutine = null; // 현재 차징 코루틴
    private float attackDistance = 2.4f; // 공격 거리
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
    public float RemainingAttackCooldown => Mathf.Max(0f, _nextCanAttackTime - Time.time); // 현재 쿨타임 남은 시간

    // 공격 가능 여부
    public bool IsAttackAvailable => IsTransformed // 변신 상태
                                    && !IsUsingAttackAbility // 공격 능력 사용 중 아님
                                    && Time.time >= _nextCanAttackTime; // 쿨타임 지났을 때

    // 변신 가능 여부
    public bool IsTransformAvailable => CanTransform // 변신 가능
                                        && FocusManager.Instance.CurrentFocusState == GameFocusState.None; // 일반 포커스 상태일 때

    #region 생명주기

    private void Awake()
    {
        _playerMoveBase = GetComponent<PlayerMoveBase>();
        _playerCC = GetComponent<CharacterController>();
        _innerMonsterController = monsterTransform.GetComponent<InnerMonsterController>();
        _deepSeaMonsterController = monsterTransform.GetComponent<DeepSeaMonsterController>();
    }

    private void Start()
    {
        monsterModel.SetActive(GameManager.instance.IsAbilityActive); // 괴물 모델 활성화 여부 설정

        // 현재 능력 사용 여부에 따라 이 컴포넌트 활성화 여부 결정
        enabled = GameManager.instance.IsAbilityActive; // 능력 사용하지 않으면 이 컴포넌트 비활성화
        if (!GameManager.instance.IsAbilityActive)
        {
            PlayerManager.Instance.playerStat.SetMonsterStatUI(false); // 기본 스탯 UI로 변경
            return; // 아래 로직 실행 x
        }

        monsterAbilityVolume.profile.TryGet(out monsterVolumeColorAdjustments);

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
        {
            PlayerManager.Instance.playerStat.StartSanityInstability(); // 정신 불안정 상태로 시작
        }
        else // 심해씬
        {
            PlayerManager.Instance.playerStat.StopSanityInstability(); // 정신 안정 상태로 시작 (최대 수치에서)

            // 공격 잘 맞도록 수치 변경
            chargingTime = 0.3f; // 심해에서는 손전등도 있고 심해 물에 닿아있다보니까 공격력 높아진 걸로 간주해서 차징 시간 줄임
            attackDistance = 10f; // 공격 거리 증가
        }
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

    #endregion

    #region 입력 이벤트

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
        if (!IsAttackAvailable) return;

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

    #region 변신

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

        abilityUI.ChangeTransformIconUI(true);

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

        abilityUI.ChangeTransformIconUI(false);

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
            CapsuleCollider modelCollider = PlayerManager.Instance.playerMoveBase.GetComponent<UnderwaterPlayerMove>().modelCollider;
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

    #endregion

    #region 차징

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

        IsUsingAttackAbility = true; // 공격 능력 사용 중으로 설정
        AddTransformLock();
        _isCharging = true;
        abilityUI.SetShowChargingUI(true);
        abilityUI.UpdateChargingGaugeUI(0f);

        PlayerManager.Instance.playerMoveBase.SetMoveable(false); // 플레이어 이동 정지
        PlayerManager.Instance.playerCameraControllerBase.SetInputEnabled(false); // 플레이어 카메라 인풋 금지 (= 회전 금지)

        if (PlayerManager.Instance.playerInteractor != null)
        {
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(false); // 상호작용 UI 비활성화
            PlayerManager.Instance.playerInteractor.ClearDetectionText(); // 상호작용 텍스트 초기화
        }

        if (currentDestroyObj == null)
            abilityUI.UpdateChargingActionText("공격 차징 중");
        else
            abilityUI.UpdateChargingActionText($"[ {currentDestroyObjName} ] 파괴 차징 중");

        // 기존 FOV 연출이 있다면 종료하고 초기화
        _fovTween?.Kill();
        _mainCamera.fieldOfView = _normalFOV;

        // 차징 시간(chargingTime) 동안 시야각을 최대치까지 서서히 넓힘
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
        abilityUI.UpdateChargingGaugeUI(0f);

        while (timer <= chargingTime)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / chargingTime);

            abilityUI.UpdateChargingGaugeUI(Mathf.Clamp01(timer / chargingTime));

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
        abilityUI.SetShowChargingUI(false);

        // 풀차징 완료 후 쾅! 할 때 시야각을 원상 복구 (아주 빠르게 쾅 당겨짐)
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

        currentDestroyObj = null;
        currentDestroyObjName = "";

        PlayerManager.Instance.playerMoveBase.SetMoveable(true); // 플레이어 이동 가능
        PlayerManager.Instance.playerCameraControllerBase.SetInputEnabled(true); // 플레이어 카메라 인풋 가능 (= 회전 가능)

        abilityUI.SetShowChargingUI(false);
        if (PlayerManager.Instance.playerInteractor != null)
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(true); // 상호작용 UI 활성화

        // 차징 중 취소/실패 시에도 시야각을 부드럽게 원래대로 복구 (툭 끊기지 않게)
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

    #endregion

    #region 공격

    /// <summary>
    /// 공격 시도
    /// </summary>
    private void TryAttack()
    {
        StartCoroutine(TryAttackCoroutine());
    }

    /// <summary>
    /// 공격 시도 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator TryAttackCoroutine()
    {
        // 카메라 위치를 공격 시 카메라 위치로 설정 (좀 더 앞)
        PlayerManager.Instance.playerCameraControllerBase.SetCameraPos(monsterAttackCameraPos);

        // 잠수함 내부 -> 카메라 아래 보도록 회전 (타격감을 위해)
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

        // 공격 애니메이션 실행
        _playerMoveBase.animator.SetLayerWeight(1, 1f);
        _playerMoveBase.animator.Play("Destroy", 1, 0.25f);

        // 일정 시간 대기
        yield return new WaitForSeconds(0.25f);

        // 카메라 위치 원래대로 되돌림
        PlayerManager.Instance.playerCameraControllerBase.SetCameraPos(monsterCameraPos);

        // 폭발 파티클 & 소리 재생
        explosionParticle.Play();
        AudioManager.Instance.PlayGlobalOneShot(destroySound);

        // 애니메이션 레이어 되돌리기
        _playerMoveBase.animator.SetLayerWeight(1, 0f);

        // 현재 파괴 오브젝트가 있으면 -> 파괴 / 없으면 -> 공격 시도
        if (currentDestroyObj != null)
        {
            DestroyTargetObj(); // 목표 오브젝트 파괴
        }
        else
        {
            // 공격 시도

            // 괴물과의 거리 계산
            float distanceToMonster = Vector3.Distance(_mainCamera.transform.position, monsterTransform.position);
            Debug.Log("distanceToMonster: " + distanceToMonster);

            // 괴물이 공격 거리 내에 있으면
            if (distanceToMonster <= attackDistance)
            {
                // 플레이어 정면 기준 시야각 계산
                Vector3 dirToMonster = (monsterTransform.position - _mainCamera.transform.position).normalized;
                float angleToMonster = Vector3.Angle(_mainCamera.transform.forward, dirToMonster);

                // 플레이어 정면 기준 좌우 60도 이내에 있으면 -> 공격
                if (angleToMonster <= 60f)
                    AttackMonster();
            }
            else // 공격 거리 내에 없으면 -> 심해라면 충격파 반사 시도
            {
                // 할당된 충격파가 있고, 해당 충격파가 활성화되어있는 경우
                if (shockwaveTrigger != null && shockwaveTrigger.gameObject.activeSelf)
                {
                    // 충격파 거리 계산
                    float ditanceToShockwave = Vector3.Distance(_mainCamera.transform.position, shockwaveTrigger.transform.position);
                    Debug.Log("ditanceToShockwave: " + ditanceToShockwave);

                    // 충격파가 공격 거리 내에 있으면
                    if (ditanceToShockwave <= attackDistance)
                    {
                        // 플레이어 정면 기준 시야각 계산
                        Vector3 dirToShockwave = (shockwaveTrigger.transform.position - _mainCamera.transform.position).normalized;
                        float angleToShockwave = Vector3.Angle(_mainCamera.transform.forward, dirToShockwave);

                        // 플레이어 정면 기준 좌우 60도 이내에 있으면 -> 충격파 반사
                        if (angleToShockwave <= 60f)
                        {
                            // 최종 패턴 중 -> 괴물 죽이기 / 일반 -> 괴물 약화
                            if (_deepSeaMonsterController.IsExecutingFinalPattern)
                                shockwaveTrigger.ReflectShockwave(monsterTransform, 1f, _deepSeaMonsterController.Die);
                            else
                                shockwaveTrigger.ReflectShockwave(monsterTransform, 1f, _deepSeaMonsterController.Weaken);
                        }

                    }
                }
            }
        }

        // 고개 들도록 카메라 회전 
        yield return new WaitForSeconds(1f);
        if (playerCameraController != null) playerCameraController.RotateToYAngle(originalPlayerYAngle, 1f, originalXRotation, Ease.InQuad);
        yield return new WaitForSeconds(1f);

        // 초기화
        IsUsingAttackAbility = false; // 공격 능력 사용 중 아님으로 설정
        RemoveTransformLock(); // 변신 금지 해제
        PlayerManager.Instance.playerMoveBase.SetMoveable(true); // 플레이어 이동 가능
        PlayerManager.Instance.playerCameraControllerBase.SetInputEnabled(true); // 플레이어 카메라 조작 가능
        if (PlayerManager.Instance.playerInteractor != null)
            PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(true); // 상호작용 UI 활성화

        // 다음 공격 가능 시간 업데이트
        _nextCanAttackTime = Time.time + _attackCoolDown;
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
        {
            if (_deepSeaMonsterController.IsExecutingFinalPattern)
                _deepSeaMonsterController.Die();
            else
                _deepSeaMonsterController.Weaken();
        }
    }

    #endregion

    #region 파괴

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
    /// 목표 오브젝트 파괴
    /// </summary>
    private void DestroyTargetObj()
    {
        Debug.Log("[능력] " + currentDestroyObj + " 파괴!");
        currentDestroyObj.SetActive(false);
        currentDestroyObj = null;
        currentDestroyObjName = "";
    }

    #endregion
}
