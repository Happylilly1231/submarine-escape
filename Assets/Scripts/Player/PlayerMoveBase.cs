using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerMoveBase : MonoBehaviour
{
    public Animator animator;
    public bool CanMove { get; private set; } = true;

    /// <summary>
    /// 애니메이터 설정
    /// </summary>
    /// <param name="animator"></param>
    public void SetAnimator(Animator animator)
    {
        this.animator.enabled = false;
        this.animator = animator;
        animator.enabled = true;
    }

    /// <summary>
    /// 이동 가능 여부 설정
    /// </summary>
    /// <param name="isMoveable">이동 가능 여부</param>
    public virtual void SetMoveable(bool isMoveable)
    {
        CanMove = isMoveable;
    }

    public virtual void SetMoveable(bool isMoveable, bool isApplingGravity = true)
    {
        SetMoveable(isMoveable);
    }
}
