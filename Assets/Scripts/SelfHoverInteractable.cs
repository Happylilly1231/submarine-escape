using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelfHoverInteractable : HoverInteractable, IPointerEnterHandler, IPointerExitHandler
{
    // 마우스가 오브젝트 위로 들어왔을 때 유니티가 자동 호출
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }

    // 마우스가 오브젝트 밖으로 나갔을 때 유니티가 자동 호출
    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }
}
