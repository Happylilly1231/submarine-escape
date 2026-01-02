using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 폭주 추적 상태 
    /// <para>- 현재 경보 발생지를 향해 이동한다.(단, 탈출실 도착 시에는 가만히 있는다.)</para>
    /// <para>- 상태 시작 시 경로 상의 문 리스트를 얻는다.</para>
    /// <para>- 레이가 파괴 가능한 것에 닿으면 해당 오브젝트를 현재 파괴할 오브젝트로 타입과 함께 설정한다.</para>
    /// <para>- 이후 정확한 파괴 위치로 순간 이동 후 파괴 상태로 전환된다.</para>
    /// <para>+) 조건 만족 시 폭주 공격 상태로 전환 가능</para>
    /// </summary>
    public class RageChaseState : IState<InnerMonsterController>
    {
        private float _rageSpeed = 10f; // 폭주 속도
        private List<Door> _doorsOnPathList = new List<Door>(); // 경로 상의 문 리스트(해당 문만 파괴하도록 해야 하므로 필요)
        private float _detectDestroyObjDistance = 1f; // 파괴 오브젝트 감지 거리
        private bool _isChasingEscapeRoom; // 탈출실이 목적지인지 여부
        private bool _reachedEscapeRoom = false; // 탈출실에 도착했는지 여부

        public void Enter(InnerMonsterController owner)
        {
            owner.CanMove(true); // 이동
            owner.Nav.speed = _rageSpeed; // 폭주 속도로 변경
            owner.Nav.SetDestination(SubmarineInGameManager.instance.CurrentAlertPos.position); // 경보 발생지를 향해 이동
            owner.ColliderCenterChange(true); // 컨트롤러 중심 변경

            // 경로 상의 문 리스트 얻기
            owner.StartCoroutine(GetDoorsOnPathList(owner));

            // 탈출실이 목적지인지 여부는 탈출실 문이 한번이라도 열렸는지 여부와 같음
            _isChasingEscapeRoom = SubmarineInGameManager.instance.hasEverOpenedEscapeDoor;
        }

        public void Update(InnerMonsterController owner)
        {
            // 탈출실이 목적지일 때 - 애니메이션 & 회전 설정
            // 도착 -> 이동 정지 & 가만히 있는 애니메이션 & 플레이어 바라보도록 수동 회전
            // 멀음 -> 이동 & 추적 애니메이션 & nav 자동 회전
            if (_isChasingEscapeRoom) // 탈출실이 목적지일 때
            {
                if (_reachedEscapeRoom) // 도착했었는데
                {
                    if (owner.Nav.remainingDistance >= 0.5f) // 목적지에서 멀어졌을 때 -> 다시 추적 애니메이션으로 변경
                    {
                        owner.CanMove(true); // 이동
                        _reachedEscapeRoom = false; // 탈출실에 도착하지 않음으로 설정
                        owner.Animator.SetBool("isRageIdle", false); // 애니메이션을 RageChase로 변경
                        owner.Nav.updateRotation = true; // 회전 자동으로 변경
                    }

                    // 현재 플레이어 위치를 바라보도록 회전
                    owner.LookAtTarget(owner.PlayerTransform.position);
                }
                else // 도착 못했었는데
                {
                    // 도착했을 때 -> 가만히 있는 애니메이션으로 변경
                    if (owner.Nav.remainingDistance < 0.5f)
                    {
                        owner.CanMove(false); // 이동 정지
                        _reachedEscapeRoom = true; // 탈출실에 도착했음으로 설정
                        owner.Animator.SetBool("isRageIdle", true); // 애니메이션을 가만히 있는 걸로 변경
                        owner.Nav.updateRotation = false; // 회전 수동으로 변경
                    }
                }
            }

            // 폭주 공격 상태로 전환
            if (owner.CanAttack() && owner.DistToPlayer < owner.RageAttackDistance)
            {
                owner.ChangeState(new RageAttackState());
                return;
            }

            // 레이로 파괴할 것에 닿았는지 검사
            Vector3 eyePos = owner.transform.position + Vector3.up; // 눈높이 위치
            Debug.DrawRay(eyePos, owner.transform.forward * _detectDestroyObjDistance, Color.red);
            if (Physics.Raycast(eyePos, owner.transform.forward, out RaycastHit hit, _detectDestroyObjDistance))
            {
                // 현재 파괴해야 할 장비에 닿으면 -> 폭주 파괴 상태로 전환
                if (hit.collider.gameObject == SubmarineInGameManager.instance.CurrentDestroyEquipment)
                {
                    // 현재 파괴해야 할 오브젝트로 설정
                    owner.currentDestroyObj = hit.collider.gameObject;
                    owner.Nav.Warp(SubmarineInGameManager.instance.CurrentDestroyEquipment.transform.GetChild(0).transform.position); // 정확한 파괴 위치로 순간 이동
                    owner.currentDestroyObjType = EDestroyObjType.CurrentDestroyEquipment; // 현재 파괴해야 할 오브젝트 타입 -> 현재 파괴해야 할 장비로 설정
                    owner.ChangeState(new RageDestroyState()); // 폭주 파괴 상태로 전환
                    return;
                }

                // 경로 상의 문에 닿으면 -> 폭주 파괴 상태로 전환
                Door door = hit.collider.GetComponent<Door>();
                if (door != null && _doorsOnPathList.Contains(door)) // 경로 상의 문 리스트에 존재하면
                {
                    // 현재 파괴해야 할 오브젝트로 설정
                    owner.currentDestroyObj = hit.collider.gameObject;

                    // 가까운 정확한 파괴 위치로 순간 이동
                    Vector3 closestDestroyPos;
                    closestDestroyPos = door.frontPos;
                    float dist1 = Vector3.Distance(owner.transform.position, door.frontPos);
                    float dist2 = Vector3.Distance(owner.transform.position, door.backPos);
                    if (dist2 < dist1)
                    {
                        closestDestroyPos = door.backPos;
                    }
                    owner.Nav.Warp(closestDestroyPos); // 순간 이동

                    // 현재 파괴해야 할 오브젝트 타입 선택 & 폭주 파괴 상태로 전환
                    if (hit.collider.CompareTag("Door")) // 기본 문
                    {
                        owner.currentDestroyObjType = EDestroyObjType.Door; // 현재 파괴해야 할 오브젝트 타입 -> 기본 문으로 설정
                        owner.ChangeState(new RageDestroyState()); // 폭주 파괴 상태로 전환
                    }
                    else if (hit.collider.CompareTag("EscapeRoomDoor")) // 탈출실 문
                    {
                        owner.currentDestroyObjType = EDestroyObjType.EscapeRoomDoor; // 현재 파괴해야 할 오브젝트 타입 -> 탈출실 문으로 설정
                        owner.ChangeState(new RageDestroyState()); // 폭주 파괴 상태로 전환
                    }
                }
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Animator.SetBool("isRageChasing", false); // 폭주 추적 애니메이션 종료
        }

        /// <summary>
        /// 경로 상의 문 리스트 얻기 함수
        /// </summary>
        private IEnumerator GetDoorsOnPathList(InnerMonsterController monster)
        {
            // 경로 계산 완료 대기
            while (monster.Nav.pathPending)
                yield return null;

            // 경로의 코너 배열 가져오기
            Vector3[] corners = monster.Nav.path.corners;

            // 코너 ~ 다음 코너 구간마다 RayCastAll 함수로 경로 상의 문 검출 -> DoorsOnPathList에 추가
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 start = corners[i]; // 현재 코너
                Vector3 end = corners[i + 1]; // 다음 코너
                Vector3 dir = (end - start).normalized; // 현재 코너에서 다음 코너로의 정규화된 방향
                float dist = Vector3.Distance(start, end); // 현재 코너에서 다음 코너까지의 거리

                // RayCastAll로 현재 코너에서 다음 코너로의 방향으로 다음 코너까지의 거리만큼만 문 레이어에 대해서만 검사(최대한 레이캐스트 범위를 한정함)
                RaycastHit[] hits = Physics.RaycastAll(start, dir, dist, LayerMask.GetMask("Door"));
                foreach (var hit in hits)
                {
                    Door door = hit.collider.GetComponent<Door>();
                    if (door != null && !_doorsOnPathList.Contains(door)) // 현재 리스트에 저장되지 않은 문들만 -> 리스트에 추가
                    {
                        door.gameObject.GetComponent<Renderer>().material.color = Color.red; // 해당 문 빨간색으로 표시
                        _doorsOnPathList.Add(door); // 리스트에 추가
                    }
                }
            }
        }
    }
}