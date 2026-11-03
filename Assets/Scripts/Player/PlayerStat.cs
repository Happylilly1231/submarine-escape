using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 플레이어 스탯 관리
/// <para> - 스탯: 체력(hp), 스태미나(stamina), 체온(temperature) </para> 
/// <para> - 스탯 계산 함수 </para> 
/// <para> - 스탯 UI 관리 </para>
/// </summary>
public class PlayerStat : MonoBehaviour
{
    private PlayerStatus _playerStatus;

    // 체력
    private float _hp; // 현재 체력
    public float Hp => _hp;
    private float _maxHp = 100f; // 최대 체력
    private Color hpTextOriginalColor;

    // 스태미나(회피용, 1칸씩 사용)
    private float _stamina; // 현재 스태미나
    public float Stamina => _stamina;
    private float _maxStamina = 3f; // 최대 스태미나
    private Coroutine _currentRechargeCoroutine; // 현재 진행 중인 스태미나 회복 코루틴

    // 체온
    private float _temperature; // 체온
    public float Temperature => _temperature;
    private float _maxTemperature = 36.5f; // 최대 체온

    // 정신력
    private float _sanity; // 정신력
    public float Sanity => _sanity;
    private float _maxSanity = 100f;
    public bool IsSanityUnstable { get; set; } = true; // 정신 불안정 상태 여부
    private Coroutine _unstableCoroutine;

    // 스탯 UI
    [SerializeField] private Slider hpSlider; // 체력 바
    [SerializeField] private TextMeshProUGUI hpText; // 체력 텍스트(ex) 100/100)
    [SerializeField] private Slider staminaSlider; // 스태미나 바
    [SerializeField] private Slider staminaGlowSlider; // 스태미나 각 칸이 완전히 채워졌을 때 표시되는 발광 부분
    [SerializeField] private GameObject sanityUI; // 정신력 UI

    [SerializeField] private Slider sanitySlider; // 정신력 바

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip damageSound;

    public event Action OnPlayerDamaged;

    void Awake()
    {
        _playerStatus = GetComponent<PlayerStatus>();

        _hp = _maxHp;
        _stamina = _maxStamina;
        _temperature = _maxTemperature;
        _sanity = _maxSanity;

        if (hpText != null)
            hpTextOriginalColor = hpText.color;
    }

    /// <summary>
    /// 스탯을 최대 수치로 초기화, 스탯 UI 업데이트
    /// </summary>
    void Start()
    {
        UpdateHpSlider();
        UpdateStaminaSlider();
        UpdateSanitySlider();
    }

    #region 공통 & 기타

    /// <summary>
    /// 괴물로 변신했을 때 스탯 UI 보여줄지 설정 함수
    /// </summary>
    /// <param name="isShow">보여주기 여부(괴물 여부)</param>
    public void SetMonsterStatUI(bool isShow)
    {
        // 괴물일 때는 스태미나 바 X
        staminaSlider.gameObject.SetActive(!isShow);
        staminaGlowSlider.gameObject.SetActive(!isShow);

        // 괴물일 때는 정신력 UI O
        sanityUI.gameObject.SetActive(isShow);

        if (isShow) UpdateSanitySlider();
        else UpdateStaminaSlider();
    }

    public void Die(EEndingType cause)
    {
        Debug.Log($"플레이어 사망 원인: {cause}");
        GameManager.instance.GameOver(cause);
    }

    public void ApplyLoadedStats(float hp, float stamina)
    {
        _hp = hp;
        _stamina = stamina;

        UpdateHpSlider();
        UpdateStaminaSlider();
    }

    #endregion

    #region HP
    /// <summary>
    /// 피해: value만큼 체력 감소
    /// <para> - 체력 0 이하 시 사망 </para> 
    /// </summary>
    public void Damage(float value, EEndingType cause = EEndingType.MonsterDeath)
    {
        AudioManager.Instance.PlayGlobalOneShot(damageSound);
        _hp -= value;
        Debug.Log($"Damage: -{value} | Cause: {cause}");
        if (_hp <= 0)
        {
            _hp = 0;
            UpdateHpSlider();
            Die(cause); // 사망 원인을 넘겨줌
            return;
        }
        UpdateHpSlider();

        // 심해일 때는 대미지 효과, 슬로우 효과 적용 X (심해에서는 대미지 볼륨 따로 있음)
        if (FocusManager.Instance.CurrentFocusState != GameFocusState.DeepSea)
        {
            StartCoroutine(DamageEffect());
            StartCoroutine(_playerStatus.SlowEffect());
        }

        OnPlayerDamaged?.Invoke();
    }

    private IEnumerator DamageEffect()
    {
        FXManager.instance.VignetteOn(Color.red);

        hpText.color = Color.red;
        yield return new WaitForSeconds(2f);
        FXManager.instance.VignetteOff();
        hpText.color = hpTextOriginalColor;
    }

    /// <summary>
    /// 회복: value만큼 체력 증가
    /// </summary>
    public void Heal(float value)
    {
        _hp += value;
        // 체력이 최대 체력을 넘지 않도록 제한
        if (_hp > _maxHp)
        {
            _hp = _maxHp;
        }
        UpdateHpSlider();
        Debug.Log("Heal: +" + value);
    }

    /// <summary>
    /// 체력 바 UI 업데이트
    /// </summary>
    public void UpdateHpSlider()
    {
        hpSlider.value = _hp / _maxHp;
        hpText.text = _hp + "/" + _maxHp;
    }

    #endregion

    #region 스태미나

    /// <summary>
    /// cnt칸 만큼 스태미나 사용
    /// <para> - 사용하면 일정 시간 동안 최대로 회복됨 </para> 
    /// </summary>
    public bool UseStamina(int cnt = 1)
    {
        if (_stamina < cnt)
        {
            Debug.Log("현재 스태미나를 사용할 수 없습니다.");
            return false;
        }

        if (_currentRechargeCoroutine != null)
            StopCoroutine(_currentRechargeCoroutine);
        _stamina -= cnt;
        Debug.Log("Stamina: -" + cnt);
        UpdateStaminaSlider();
        _currentRechargeCoroutine = StartCoroutine(RechargeStamina());
        return true;
    }

    /// <summary>
    /// 스태미나 회복 코루틴
    /// <para> - 10초에 1칸 속도로 최대 스태미나까지 회복(0.01f만큼 0.1초마다 회복) </para> 
    /// </summary>
    IEnumerator RechargeStamina()
    {
        while (_stamina < _maxStamina)
        {
            _stamina += 0.01f;
            UpdateStaminaSlider();
            yield return new WaitForSeconds(0.1f);
        }
        _currentRechargeCoroutine = null;
    }

    /// <summary>
    /// 스태미나 바 UI 업데이트
    /// </summary>
    public void UpdateStaminaSlider()
    {
        staminaSlider.value = _stamina / _maxStamina;
        staminaGlowSlider.value = (int)_stamina / _maxStamina; // 각 칸이 회복되면 발광
    }

    #endregion

    #region 정신력

    /// <summary>
    /// 정신력 사용(소모)
    /// </summary>
    public void UseSanity(float value)
    {
        _sanity -= value;
        if (_sanity <= 0)
        {
            _sanity = 0;
            UpdateSanitySlider();
            Die(EEndingType.Mutation); // 사망 원인은 괴물화로 고정
            return;
        }
        UpdateSanitySlider();
    }

    /// <summary>
    /// 정신력 매우 빠르게 0으로 감소
    /// </summary>
    public void DecreaseSanityRapidly()
    {
        StartCoroutine(DecreaseSanityRapidlyCoroutine());
    }

    /// <summary>
    /// 빠르게 정신력을 0으로 줄이며 심장 박동 연출을 하는 코루틴
    /// </summary>
    private IEnumerator DecreaseSanityRapidlyCoroutine()
    {
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence);

        float duration = 5f;
        float elapsed = 0f;
        float startSanity = _sanity;

        FXManager.instance.StartHeartbeatUI(duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            _sanity = Mathf.Lerp(startSanity, 0f, normalizedTime);
            UpdateSanitySlider();
            yield return null;
        }

        _sanity = 0f;
        UpdateSanitySlider();

        Sequence seq = DOTween.Sequence();
        seq.Append(FXManager.instance.fadeImage.DOFade(1f, 2f)); // 화면이 완전히 검게 변함
        seq.AppendCallback(() => AudioManager.Instance.FadeOutSFX(2f)); // 경보 소리 페이드아웃되면서 꺼짐
        seq.OnComplete(() =>
        {
            Die(EEndingType.Mutation); // 괴물화로 게임 오버
        });
    }

    /// <summary>
    /// 정신력 바 UI 업데이트
    /// </summary>
    public void UpdateSanitySlider()
    {
        sanitySlider.value = _sanity / _maxSanity;
    }

    /// <summary>
    /// 정신 불안정 상태 시작
    /// </summary>
    public void StartSanityInstability()
    {
        IsSanityUnstable = true;
        PlayerManager.Instance.playerAbility.AddTransformLock(); // 변신 불가로 설정

        if (_unstableCoroutine != null) StopCoroutine(_unstableCoroutine);
        _unstableCoroutine = StartCoroutine(SanityInstabilityCoroutine());
    }

    /// <summary>
    /// 정신 안정 상태로 복귀
    /// </summary>
    public void StopSanityInstability()
    {
        IsSanityUnstable = false; // 정신 안정 상태
        PlayerManager.Instance.playerAbility.RemoveTransformLock(); // 변신 가능으로 설정
        PlayerManager.Instance.playerAbility.UnlockTransformUI(); // 변신 UI 잠금 해제
        _sanity = _maxSanity; // 정신력 최대 수치로 완전 회복
        UpdateSanitySlider();
    }

    /// <summary>
    /// 정신력이 낮은 수치에서 랜덤하게 왔다갔다하는 루틴
    /// </summary>
    private IEnumerator SanityInstabilityCoroutine()
    {
        // 불안정 상태인 동안 계속 반복
        while (IsSanityUnstable)
        {
            // 1. 랜덤하게 도달할 목표 정신력과 걸리는 시간 설정
            float targetSanity = Random.Range(10f, 40f); // 왔다갔다할 낮은 수치 범위
            float changeDuration = Random.Range(0.5f, 1f); // 수치가 바뀌는 속도 (불규칙하게 흔들림)
            float elapsed = 0f;
            float startSanity = _sanity;

            // 2. 부드럽게 목표 수치로 이동 (Lerp 사용)
            while (elapsed < changeDuration && IsSanityUnstable)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / changeDuration;

                // 매 프레임 변하는 목표치와 현재 정신력의 차이를 계산하여 소모/회복
                _sanity = Mathf.Lerp(startSanity, targetSanity, normalizedTime);
                UpdateSanitySlider();

                yield return null;
            }
        }

        _unstableCoroutine = null;
    }

    /// <summary>
    /// 심해에서의 정신력 업데이트
    /// </summary>
    public void UpdateSanityInDeapSea(float survivalTime)
    {
        float lossPerSec = _maxSanity / survivalTime; // 정신력 감소율 계산

        _sanity -= lossPerSec * Time.deltaTime;
        UpdateSanitySlider(); // UI 업데이트

        if (_sanity <= 0f)
        {
            _sanity = 0f;
            Debug.Log("정신력 0 도달 - 괴물화(폭주)로 사망");
            Die(EEndingType.Mutation); // 사망 원인은 괴물화로 고정
            return;
        }
    }

    #endregion
}
