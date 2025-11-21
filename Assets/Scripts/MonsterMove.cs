using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 이동
/// </summary>
public class MonsterMove : MonoBehaviour
{
    [SerializeField] private Transform mTarget;
    [SerializeField] private float mStopDistance = 2f; // 멈출 거리

    private NavMeshAgent mNav;

    void Start()
    {
        mNav = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, mTarget.position);

        if (distance > mStopDistance)
        {
            // 목표가 멀리 있으면 이동
            mNav.isStopped = false;
            mNav.SetDestination(mTarget.position);
        }
        else
        {
            // 목표가 가까워지면 멈춤
            mNav.isStopped = true;
        }
    }
}
