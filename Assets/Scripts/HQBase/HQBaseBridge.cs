using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HQArrivalChoice
{
    None = -1,
    DoNothing = 0,     // 아무것도 하지 않는다 (1번 버튼)
    WaitForTiming = 1  // 타이밍을 기다린다 (2번 버튼)
}

public class HQBaseBridge : MonoBehaviour
{
    public static HQBaseBridge Instance { get; private set; }
    public HQArrivalChoice ArrivalChoice { get; private set; } = HQArrivalChoice.None;

    /// <summary>
    /// '타이밍을 기다린다'를 선택해 본부 복도 이동 중 폭탄 QTE가 떠야 하는지 여부
    /// </summary>
    public bool ShouldTriggerBombQTE => ArrivalChoice == HQArrivalChoice.WaitForTiming;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 엔딩 선택지 설정
    /// </summary>
    /// <param name="choiceIndex"></param>
    public void SetArrivalChoice(int choiceIndex)
    {
        if (System.Enum.IsDefined(typeof(HQArrivalChoice), choiceIndex))
        {
            ArrivalChoice = (HQArrivalChoice)choiceIndex;
        }
        else
        {
            ArrivalChoice = HQArrivalChoice.None;
        }
    }
}
