using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HoverInteractable : MonoBehaviour
{
    protected Outline outline; // 아웃라인
    // private PuzzleController _puzzleController = null; // 퍼즐 컨트롤러

    public virtual void Awake()
    {
        outline = GetComponentInChildren<Outline>(); // 아웃라인이 자식에 있으면 그 아웃라인 가져오도록 함
    }

    /// <summary>
    /// 호버 시작
    /// </summary>
    public virtual void OnHoverEnter()
    {
        SetShowOutline(true);
    }

    /// <summary>
    /// 호버 끝
    /// </summary>
    public virtual void OnHoverExit()
    {
        SetShowOutline(false);
    }

    /// <summary>
    /// 아웃라인 보이기 여부 설정
    /// </summary>
    /// <param name="isShow">보이기 여부</param>
    public virtual void SetShowOutline(bool isShow)
    {
        if (outline != null)
        {
            if (isShow)
                outline.enabled = true;
            else
                outline.enabled = false;
        }
    }
}
