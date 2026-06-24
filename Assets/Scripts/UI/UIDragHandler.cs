using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private Vector2 dragOffset; // 마우스 클릭 지점과 UI 중심점 사이의 오차를 기억할 변수
    private Vector2 originAnchoredPosition; // 원래 위치

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // UI가 속한 최상위 캔버스를 찾아옵니다 (좌표 계산용)
        canvas = GetComponentInParent<Canvas>();

        // 처음 UI 위치를 원본 위치로 저장
        originAnchoredPosition = rectTransform.anchoredPosition;
    }

    // 드래그를 시작할 때 딱 한 번 실행 (클릭한 순간)
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 마우스 커서 위치와 UI의 피벗 위치 사이의 차이(Offset)를 계산해서
        // 마우스를 잡은 위치 그대로 부드럽게 끌고 가도록 만듭니다.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMousePos
        );

        dragOffset = localMousePos;
    }

    // 마우스를 누른 채로 움직이는 동안 매 프레임 실행
    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 마우스의 스크린 좌표를 캔버스 안의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 movePos
        );

        // 오차(Offset)를 보정하여 UI 위치를 마우스 커서에 맞춰 갱신
        rectTransform.anchoredPosition = movePos - dragOffset;
    }

    /// <summary>
    /// 원래 위치로 되돌리는 함수
    /// </summary>
    public void ResetToOriginPos()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originAnchoredPosition;
        }
    }
}
