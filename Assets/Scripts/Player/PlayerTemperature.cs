using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 심해에서 플레이어 체온 관리
/// <para> - 심해에 진입하면 300초 동안 체온 감소 시작 </para>
/// <para> - 열 보호 장치 장착 시 90초 동안 체온 감소 무효화 </para>
/// <para> - 체온이 0도에 도달하면 저체온증으로 사망 </para>
/// </summary>
public class PlayerTemperature : MonoBehaviour
{
    private bool isInDeepSea = false; // 심해에 있는지 여부
    private bool isThermalProtectorEquipped = false; // 열 보호 장치 장착 여부
    private bool isThermalProtectorActive = false; // 열 보호 장치 활성화 여부
    private float currentTemperature = 100f; // 현재 체온
    private float survialTime = 300f; // 생존 시간(초)
    private float thermalProtectorDuration = 90f; // 열 보호 장치 지속 시간(초)
    private float temperatureDecreaseRate; // 체온 감소율(초당)

    private void Start()
    {
        // 체온 감소율 계산
        temperatureDecreaseRate = currentTemperature / survialTime;
    }

    private void Update()
    {
        // 잠수함 내부 - 체온 감소 X
        if (!isInDeepSea) return;

        // 심해 진입 - 체온 감소 O
        // 열 보호 장치가 장착되어 있다면 90초동안 체온 감소 x
        if (isThermalProtectorEquipped && isThermalProtectorActive)
        {
            // 열 보호 장치 지속 시간 감소
            thermalProtectorDuration -= Time.deltaTime;
            if (thermalProtectorDuration <= 0f)
            {
                isThermalProtectorActive = false;
                Debug.Log("열 보호 장치 지속 시간 종료 - 체온 감소 시작");
            }
        }
        // 열 보호 장치가 장착되지 않았거나 지속 시간이 종료되었다면 체온 감소
        else
        {
            // 체온 감소
            currentTemperature -= temperatureDecreaseRate * Time.deltaTime;
            if (currentTemperature <= 0f)
            {
                currentTemperature = 0f;
                Debug.Log("체온 0도 도달 - 저체온증으로 사망");

                GameManager.instance.GameOver(EEndingType.Hypothermia); // 저체온증으로 사망
            }
        }

    }

    /// <summary>
    /// 열 보호 장치 장착
    /// <para> - 심해에 진입한 경우 열 보호 장치 활성화 </para>
    /// <para> - 열 보호 장치가 장착되지 않은 경우 비활성화 </para>
    /// </summary>
    public void EquipThermalProtector()
    {
        isThermalProtectorEquipped = true;
        if (isInDeepSea) isThermalProtectorActive = true;
        else isThermalProtectorActive = false;
    }

    /// <summary>
    /// 심해 진입
    /// </summary>
    public void EnterDeepSea()
    {
        isInDeepSea = true;
        if (isThermalProtectorEquipped) isThermalProtectorActive = true;
    }
}
