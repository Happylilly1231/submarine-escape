using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterPlayerStat : MonoBehaviour
{
    [Header("체온")]
    [SerializeField] private float thermalProtectorDuration = 90f; // 열 보호 장치 지속 시간(초)

    [Header("대미지")]
    [SerializeField] private Volume damagedVolume;
    [SerializeField] private AudioClip damageSound;

    public float MaxOxygen { get; private set; } = 100f; // 최대 산소량
    public float CurrentOxygen { get; private set; } = 100f; // 현재 산소량
    public float SurvivalTime = 300f; // 생존 시간(초)
    public float ThermalProtectorDuration => thermalProtectorDuration;
    public float MaxTemperature { get; private set; } = 100f; // 최대 체온
    public float CurrentTemperature { get; private set; } = 100f; // 현재 체온


    #region 산소

    /// <summary>
    /// 산소 소모
    /// </summary>
    /// <param name="amount">소모량</param>
    public void ConsumeOxygen(float amount)
    {
        CurrentOxygen = Mathf.Max(0f, CurrentOxygen - amount);
        if (CurrentOxygen == 0f)
        {
            Debug.Log("=== Game Over ===");
            GameManager.instance.GameOver(EEndingType.MonsterDeath); // 임시로 괴물한테 죽음 엔딩으로 해놓음
        }
    }

    /// <summary>
    /// 산소 충전
    /// </summary>
    /// <param name="amount">충전량</param>
    public void RechargeOxygen(float amount)
    {
        CurrentOxygen = Mathf.Min(MaxOxygen, CurrentOxygen + amount);
    }

    #endregion

    #region 대미지 받았을 때

    public void TakeDamage(float amount)
    {
        Debug.Log("대미지 입음!");

        if (GameManager.instance.IsAbilityActive) // 능력 사용 게임일 때
            PlayerManager.Instance.playerStat.Damage(amount); // HP 대미지
        else // 아니고 일반 게임일 때
        {
            AudioManager.Instance.PlayGlobalOneShot(damageSound);
            ConsumeOxygen(amount); // 산소 대미지
        }

        // 대미지 볼륨 효과
        DOTween.To(() => damagedVolume.weight, x => damagedVolume.weight = x, 1f, 0.5f)
            .OnComplete(() =>
            {
                DOTween.To(() => damagedVolume.weight, x => damagedVolume.weight = x, 0f, 0.5f);
            });
    }

    public void TriggerHitStop(float duration)
    {
        StartCoroutine(HitStopCoroutine(duration));
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(duration); // Realtime 사용 필수
        Time.timeScale = 1.0f;
    }

    #endregion

    #region 체온
    /// <summary>
    /// 체온 업데이트
    /// <para> - 열 보호 장치가 장착되어 있고, 사용 중이라면 90초동안 체온 감소 X </para>
    /// <para> - 열 보호 장치가 장착되지 않았거나, 사용 중이 아니라면 체온 감소 </para>
    /// </summary>
    public void UpdateTemperature()
    {
        float temperatureLossPerSec = MaxTemperature / SurvivalTime; // 체온 감소율 계산

        // 열 보호 장치가 장착되어 있고, 사용 중이라면 90초동안 체온 감소 X
        if (DeepSeaInventoryManager.Instance.HasThermalProtector && DeepSeaInventoryManager.Instance.IsThermalProtectorActive)
        {
            // 열 보호 장치 지속 시간 감소
            thermalProtectorDuration -= Time.deltaTime;
            if (thermalProtectorDuration <= 0f)
            {
                DeepSeaInventoryManager.Instance.IsThermalProtectorActive = false;
                Debug.Log("열 보호 장치 지속 시간 종료 - 체온 감소 시작");
            }
        }
        else // 열 보호 장치가 장착되지 않았거나, 사용 중이 아니라면 체온 감소
        {
            CurrentTemperature -= temperatureLossPerSec * Time.deltaTime;
            if (CurrentTemperature <= 0f)
            {
                CurrentTemperature = 0f;
                Debug.Log("체온 0도 도달 - 저체온증으로 사망");
                // GameManager.instance.GameOver(EEndingType.Hypothermia); // 저체온증으로 사망
            }
        }
    }
    #endregion
}
