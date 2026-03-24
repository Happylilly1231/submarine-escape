using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    // 스태미나(회피용, 1칸씩 사용)
    private float _stamina; // 현재 스태미나
    public float Stamina => _stamina;
    private float _maxStamina = 3f; // 최대 스태미나
    private Coroutine _currentRechargeCoroutine; // 현재 진행 중인 스태미나 회복 코루틴

    // 체온
    private float _temperature; // 체온
    public float Temperature => _temperature;
    private float _maxTemperature = 36.5f; // 최대 체온

    // 스탯 UI
    [SerializeField] private Slider hpSlider; // 체력 바
    [SerializeField] private TextMeshProUGUI hpText; // 체력 텍스트(ex) 100/100)
    [SerializeField] private Slider staminaSlider; // 스태미나 바
    [SerializeField] private Slider staminaGlowSlider; // 스태미나 각 칸이 완전히 채워졌을 때 표시되는 발광 부분

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip damageSound;

    void Awake()
    {
        _playerStatus = GetComponent<PlayerStatus>();
    }

    /// <summary>
    /// 스탯을 최대 수치로 초기화, 스탯 UI 업데이트
    /// </summary>
    void Start()
    {
        _hp = _maxHp;
        _stamina = _maxStamina;
        _temperature = _maxTemperature;

        UpdateHpSlider();
        UpdateStaminaSlider();


    }

    /// <summary>
    /// 피해: value만큼 체력 감소
    /// <para> - 체력 0 이하 시 사망 </para> 
    /// </summary>
    public void Damage(float value, EEndingType cause = EEndingType.MonsterDeath)
    {
        AudioManager.Instance.PlaySFX(damageSound);
        _hp -= value;
        Debug.Log($"Damage: -{value} | Cause: {cause}");
        if (_hp <= 0)
        {
            _hp = 0;
            Die(cause); // 사망 원인을 넘겨줌
        }
        UpdateHpSlider();
        StartCoroutine(DamageEffect());
        StartCoroutine(_playerStatus.SlowEffect());
    }

    IEnumerator DamageEffect()
    {
        FXManager.instance.VignetteOn(Color.red);
        Color originalColor = hpText.color;
        hpText.color = Color.red;
        yield return new WaitForSeconds(2f);
        FXManager.instance.VignetteOff();
        hpText.color = originalColor;
    }

    public void Die(EEndingType cause)
    {
        Debug.Log($"플레이어 사망 원인: {cause}");
        GameManager.instance.GameOver(cause);
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
    /// 체력 바 UI 업데이트
    /// </summary>
    public void UpdateHpSlider()
    {
        hpSlider.value = _hp / _maxHp;
        hpText.text = _hp + "/" + _maxHp;
    }

    /// <summary>
    /// 스태미나 바 UI 업데이트
    /// </summary>
    public void UpdateStaminaSlider()
    {
        staminaSlider.value = _stamina / _maxStamina;
        staminaGlowSlider.value = (int)_stamina / _maxStamina; // 각 칸이 회복되면 발광
    }
}
