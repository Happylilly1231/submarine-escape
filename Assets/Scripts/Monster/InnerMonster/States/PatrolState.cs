using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 순찰 상태 
    /// <para>- 현재 웨이포인트를 향해 이동한다.</para>
    /// <para>- 현재 웨이포인트에 도착 시 현재 웨이포인트를 다음 웨이포인트로 갱신하고 Idle 상태로 전환된다.</para>
    /// <para>- 현재 웨이포인트로 갈 수 없으면 갈 수 있는 다음 웨이포인트를 찾아 그곳으로 이동한다.</para>
    /// <para>- 갈 수 있는 웨이포인트가 아예 없으면 무기한 Idle 상태로 전환된다.</para>
    /// <para>+) 조건 만족 시 추적, 공격 상태로 전환 가능</para>
    /// </summary>
    public class PatrolState : IState<InnerMonsterController>
    {
        private float _patrolSpeed = 3f;
        private float _stopDistance = 1f; // 멈춤 거리
        private InnerMonsterController monster; // 다른 함수에 인자로 넘겨줄 수 없는 구조라 전역변수로 사용함

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(true); // 이동
            owner.Nav.speed = _patrolSpeed;
            owner.Animator.SetBool("isPatrolling", true); // 애니메이션 순찰 중(->walk)으로 설정
            owner.ColliderCenterChange(false); // 컨트롤러 중심 기본으로 돌림

            monster = owner;

            // 목적지 갱신
            UpdateDestination();

            // 이벤트 구독
            Door.OnDoorOpenStateChanged += UpdateDestination;
        }

        public void Update(InnerMonsterController owner)
        {
            if (owner.Nav.pathPending)
            {
                Debug.Log("길 계산 중");
                return;
            }

            // 웨이포인트 도착 -> Idle 상태로 전환
            if (owner.Nav.remainingDistance < _stopDistance)
            {
                owner.currentIndex = (owner.currentIndex + 1) % owner.WayPoints.Length;
                owner.ChangeState(new IdleState());
                return;
            }

            // 추적 상태로 전환
            if (owner.CanDetect())
            {
                owner.ChangeState(new ChaseState());
                return;
            }

            // 공격 상태로 전환
            if (owner.CanAttack())
            {
                owner.ChangeState(new AttackState());
                return;
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Animator.SetBool("isPatrolling", false); // 애니메이션 순찰 중 아님으로 설정

            // 이벤트 구독 해제
            Door.OnDoorOpenStateChanged -= UpdateDestination;
        }

        // 다음으로 갈 수 있는 웨이포인트 찾기
        private void FindNextWaypoint()
        {
            int idx = (monster.currentIndex + 1) % monster.WayPoints.Length;
            while (idx != monster.currentIndex) // 현재 웨이포인트 제외하고 나머지 모든 웨이포인트 순회로 검사
            {
                if (IsPathValid(monster.WayPoints[idx].position)) // 찾음
                {
                    monster.currentIndex = idx; // 해당 웨이포인트를 현재 웨이포인트로 설정
                    monster.Nav.SetDestination(monster.WayPoints[idx].position); // 해당 웨이포인트를 목적지로 설정
                    // Debug.Log("다음으로 갈 수 있는 웨이포인트 발견: 현재 웨이포인트를 인덱스 " + monster.currentIndex + "(으)로 설정합니다!");
                    return; // 종료
                }
                // Debug.Log("웨이포인트(인덱스: " + idx + ")는 갈 수 없습니다.");
                idx = (idx + 1) % monster.WayPoints.Length;
            }

            // 찾지 못한 경우 -> 갈 수 있는 웨이포인트 없음 true로 설정, Idle 상태로 전환
            // Debug.Log("갈 수 있는 웨이포인트가 존재하지 않으므로 무기한 Idle 상태로 전환됩니다.");
            monster.IsNoWaypointCanGo = true;
            monster.ChangeState(new IdleState());
        }

        // 목적지 갱신
        private void UpdateDestination()
        {
            Vector3 currentWaypointPos = monster.WayPoints[monster.currentIndex].position;
            if (IsPathValid(currentWaypointPos)) // 현재 웨이포인트를 갈 수 있으면
            {
                // Debug.Log("현재 웨이포인트(인덱스: " + monster.currentIndex + ")로 갈 수 있습니다!");
                monster.Nav.SetDestination(currentWaypointPos); // 목적지로 설정
            }
            else // 갈 수 없으면(경로가 유효하지 않음)
            {
                // Debug.Log("현재 웨이포인트(인덱스: " + monster.currentIndex + ")로 갈 수 없습니다.");
                // 다음으로 갈 수 있는 웨이포인트 탐색
                FindNextWaypoint();
            }
        }

        /// <summary>
        /// 현재 웨이포인트로 향하는 경로가 유효한지 여부 반환
        /// </summary>
        private bool IsPathValid(Vector3 targetPos)
        {
            NavMeshPath path = new NavMeshPath();
            if (monster.Nav.CalculatePath(targetPos, path))
            {
                return path.status == NavMeshPathStatus.PathComplete; // 경로가 완전한지 여부 반환
            }
            // 경로 계산을 못하면
            return false; // 목적지가 navMesh 위에 있지 않음
        }
    }
}

