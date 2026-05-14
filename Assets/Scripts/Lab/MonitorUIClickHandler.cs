using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum MonitorUIElement
{
    LoginBtn, // 로그인 버튼
    LogoutBtn,
    Octopus,
    Dragonfish,
    Seahorse,
    Crab,
    Leech,
    Sample1,
    Sample2,
    Sample3,
    ExperimentRecordBtn // 실험 기록 버튼
}

public class MonitorUIClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MonitorUIElement uiType;
    [SerializeField] private CreatureData creatureData; // 생물 데이터 (샘플 정보 포함)

    /// <summary>
    /// 클릭 발생 시
    /// </summary>
    /// <param name="eventData">클릭 이벤트 데이터</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        DatabaseMonitorDisplay.instance.OnUISelected(uiType, creatureData);
    }
}
