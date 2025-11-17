using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterMove : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float stopDistance = 2f; // 멈출 거리

    private NavMeshAgent nav;

    void Start()
    {
        nav = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > stopDistance)
        {
            // 목표가 멀리 있으면 이동
            nav.isStopped = false;
            nav.SetDestination(target.position);
        }
        else
        {
            // 목표가 가까워지면 멈춤
            nav.isStopped = true;
        }
    }
}
