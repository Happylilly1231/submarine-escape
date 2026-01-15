using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeepSeaMonsterStates;

/// <summary>
/// 심해 괴물 움직임 관리
/// </summary>
public class DeepSeaMonsterController : MonoBehaviour
{
    private StateMachine<DeepSeaMonsterController> _fsm; // 상태 머신

    private float _attackForce = 10f;

    private void Awake()
    {
        _fsm = new StateMachine<DeepSeaMonsterController>(this);
    }

    private void Start()
    {
        _fsm.ChangeState(new PatrolState());
    }

    private void Update()
    {
        _fsm.Update();
    }

    /// <summary>
    /// 어뢰 맞았을 시
    /// </summary>
    public void OnTorpedoHit()
    {
        // 공격력 3 감소
        _attackForce -= 3f;
        Debug.Log("공격력이 3 감소합니다. 현재 공격력: " + _attackForce);
    }
}
