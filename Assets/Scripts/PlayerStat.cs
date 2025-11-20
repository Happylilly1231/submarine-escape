using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStat : MonoBehaviour
{
    // 체력
    private float hp;
    private float maxHp = 100f;
    public float Hp { get => hp; set => hp = value; }

    // 스태미나(회피용)
    private float stamina;
    private float maxStamina = 3f;
    public float Stamina { get => stamina; set => stamina = value; }
    private Coroutine currentRechargeCoroutine;

    // 체온
    private float temperature;
    private float maxTemperature = 36.5f;
    public float Temperature { get => temperature; set => temperature = value; }

    // 스탯 UI
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider staminaGlowSlider;

    void Start()
    {
        Hp = maxHp;
        Stamina = maxStamina;
        Temperature = maxTemperature;

        UpdateHpSlider();
        UpdateStaminaSlider();
    }

    public void Damage(float value)
    {
        hp -= value;
        Debug.Log("Damage: -" + value);
        if (hp <= 0)
        {
            hp = 0;
            Debug.Log("Die");
        }
        UpdateHpSlider();
    }

    public void Heal(float value)
    {
        hp += value;
        UpdateHpSlider();
        Debug.Log("Heal: +" + value);
    }

    public bool UseStamina(int cnt = 1)
    {
        if (stamina < cnt)
        {
            Debug.Log("현재 스태미나를 사용할 수 없습니다.");
            return false;
        }

        if (currentRechargeCoroutine != null)
            StopCoroutine(currentRechargeCoroutine);
        stamina -= cnt;
        Debug.Log("Stamina: -" + cnt);
        UpdateStaminaSlider();
        currentRechargeCoroutine = StartCoroutine(RechargeStamina());
        return true;
    }

    public void UpdateHpSlider()
    {
        hpSlider.value = hp / maxHp;
        hpText.text = hp + "/" + maxHp;
    }

    public void UpdateStaminaSlider()
    {
        staminaSlider.value = stamina / maxStamina;
        staminaGlowSlider.value = (int)stamina / maxStamina;
    }

    IEnumerator RechargeStamina()
    {
        while (stamina < maxStamina)
        {
            stamina += 0.01f;
            UpdateStaminaSlider();
            yield return new WaitForSeconds(0.1f);
        }
        currentRechargeCoroutine = null;
    }
}
