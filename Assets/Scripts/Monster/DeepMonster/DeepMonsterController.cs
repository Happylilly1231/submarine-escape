using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeepMonsterStates;

/// <summary>
/// 심해 괴물 움직임 관리
/// </summary>
public class DeepMonsterController : MonoBehaviour
{
    private StateMachine<DeepMonsterController> _fsm; // 상태 머신

    private void Awake()
    {
        _fsm = new StateMachine<DeepMonsterController>(this);
    }

    private void Start()
    {
        _fsm.ChangeState(new PatrolState());
    }

    private void Update()
    {
        _fsm.Update();
    }
}
