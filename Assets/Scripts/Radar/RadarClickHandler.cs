using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 레이더 화면 클릭했을 때의 핸들러(레이더 UI에서 레이더 화면에 추가)
/// </summary>
public class RadarClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RadarDisplay _radarDisplay;
    private RectTransform _radarArea;

    private void Start()
    {
        _radarArea = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 클릭 발생 시
    /// </summary>
    /// <param name="eventData">클릭 이벤트 데이터</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (FocusManager.Instance.CurrentPuzzleController is not RadarController)
            return;

        // 현재 영역 안에서 클릭 발생한 로컬 위치 가져오기
        Vector2 currentUIPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_radarArea, eventData.position, eventData.enterEventCamera, out currentUIPos);

        // 레이더 컨트롤러에서 현재 선택 위치를 갱신하기 위해 클릭 대상과 로컬 위치 넘겨주기
        _radarDisplay.UpdateCurrentSelectUIPos(currentUIPos);
        Debug.Log(currentUIPos);
    }
}
