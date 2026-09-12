using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepSeaMonsterAnimationEvents : MonoBehaviour
{
    private DeepSeaMonsterController _controller;

    private void Awake()
    {
        // 부모 오브젝트에 있는 컨트롤러 컴포넌트를 가져옴
        _controller = GetComponentInParent<DeepSeaMonsterController>();
    }

    public void OnAttackStart()
    {
        if (_controller != null && _controller.enabled)
        {
            _controller.OnAttackStart();
        }
    }

    public void OnAttackEnd()
    {
        if (_controller != null && _controller.enabled)
        {
            _controller.OnAttackEnd();
        }
    }

    // public void OnAttack()
    // {
    //     if (_controller != null && _controller.enabled)
    //     {
    //         _controller.OnAttack();
    //     }
    // }
}
