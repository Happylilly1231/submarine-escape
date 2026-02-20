using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HoverInteractable : MonoBehaviour
{
    Outline outline;

    public virtual void Awake()
    {
        outline = GetComponentInChildren<Outline>();
    }

    public virtual void OnHoverEnter()
    {
        SetShowOutline(true);
    }
    public virtual void OnHoverExit()
    {
        SetShowOutline(false);
    }

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
