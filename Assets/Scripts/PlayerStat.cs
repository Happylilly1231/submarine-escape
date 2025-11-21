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
    // 체력
    private float mHp; // 현재 체력
    public float Hp => mHp;
    private float mMaxHp = 100f; // 최대 체력

    // 스태미나(회피용, 1칸씩 사용)
    private float mStamina; // 현재 스태미나
    public float Stamina => mStamina;
    private float mMaxStamina = 3f; // 최대 스태미나
    private Coroutine mCurrentRechargeCoroutine; // 현재 진행 중인 스태미나 회복 코루틴

    // 체온
    private float mTemperature; // 체온
    public float Temperature => mTemperature;
    private float mMaxTemperature = 36.5f; // 최대 체온

    // 스탯 UI
    [SerializeField] private Slider mHpSlider; // 체력 바
    [SerializeField] private TextMeshProUGUI mHpText; // 체력 텍스트(ex) 100/100)
    [SerializeField] private Slider mStaminaSlider; // 스태미나 바
    [SerializeField] private Slider mStaminaGlowSlider; // 스태미나 각 칸이 완전히 채워졌을 때 표시되는 발광 부분

    /// <summary>
    /// 스탯을 최대 수치로 초기화, 스탯 UI 업데이트
    /// </summary>
    void Start()
    {
        mHp = mMaxHp;
        mStamina = mMaxStamina;
        mTemperature = mMaxTemperature;

        UpdateHpSlider();
        UpdateStaminaSlider();
    }

    /// <summary>
    /// 피해: value만큼 체력 감소
    /// <para> - 체력 0 이하 시 사망 </para> 
    /// </summary>
    public void Damage(float value)
    {
        mHp -= value;
        Debug.Log("Damage: -" + value);
        if (mHp <= 0)
        {
            mHp = 0;
            Debug.Log("Die");
        }
        UpdateHpSlider();
    }

    /// <summary>
    /// 회복: value만큼 체력 증가
    /// </summary>
    public void Heal(float value)
    {
        mHp += value;
        UpdateHpSlider();
        Debug.Log("Heal: +" + value);
    }

    /// <summary>
    /// cnt칸 만큼 스태미나 사용
    /// <para> - 사용하면 일정 시간 동안 최대로 회복됨 </para> 
    /// </summary>
    public bool UseStamina(int cnt = 1)
    {
        if (mStamina < cnt)
        {
            Debug.Log("현재 스태미나를 사용할 수 없습니다.");
            return false;
        }

        if (mCurrentRechargeCoroutine != null)
            StopCoroutine(mCurrentRechargeCoroutine);
        mStamina -= cnt;
        Debug.Log("Stamina: -" + cnt);
        UpdateStaminaSlider();
        mCurrentRechargeCoroutine = StartCoroutine(RechargeStamina());
        return true;
    }

    /// <summary>
    /// 스태미나 회복 코루틴
    /// <para> - 10초에 1칸 속도로 최대 스태미나까지 회복(0.01f만큼 0.1초마다 회복) </para> 
    /// </summary>
    IEnumerator RechargeStamina()
    {
        while (mStamina < mMaxStamina)
        {
            mStamina += 0.01f;
            UpdateStaminaSlider();
            yield return new WaitForSeconds(0.1f);
        }
        mCurrentRechargeCoroutine = null;
    }

    /// <summary>
    /// 체력 바 UI 업데이트
    /// </summary>
    public void UpdateHpSlider()
    {
        mHpSlider.value = mHp / mMaxHp;
        mHpText.text = mHp + "/" + mMaxHp;
    }

    /// <summary>
    /// 스태미나 바 UI 업데이트
    /// </summary>
    public void UpdateStaminaSlider()
    {
        mStaminaSlider.value = mStamina / mMaxStamina;
        mStaminaGlowSlider.value = (int)mStamina / mMaxStamina; // 각 칸이 회복되면 발광
    }
}
