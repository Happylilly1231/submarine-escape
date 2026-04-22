using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Extra, Maze, Large, CornerLeft, CornerRight, CornerLeftWithDoor, CornerRightWithDoor, Blocked, LongCorridor, Puzzle2, Puzzle3 }

public class Portal : MonoBehaviour
{
    public RoomType roomType;
    public Portal targetPortal;
    public Transform spawnPoint;

    public static event Action<RoomType> OnRoomExit; // 해당 방에서 나가는 이벤트
    public static event Action<RoomType> OnRoomEnter; // 해당 방에 진입하는 이벤트

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMove playerMove))
        {
            OnRoomExit?.Invoke(roomType);

            // 현재 방에서 나갈 때 로직
            switch (roomType)
            {
                case RoomType.LongCorridor: // 만약 이 구역이 긴 복도라면 -> 백룸 매니저에서 긴 복도 목적지 포탈 변경 여부 false로 초기화
                    BackroomManager.Instance.isLongCorridorTargetPortalChanged = false;
                    BackroomManager.Instance.SetAppearLongCorridorDoor(false);
                    break;
            }

            // 만약 탈출 포탈에 진입하면 -> 탈출
            if (this == BackroomManager.Instance.EscapePortal)
            {
                BackroomManager.Instance.EscapeBackroom(playerMove); // 백룸 탈출
                return;
            }

            Teleport(playerMove); // 순간이동

            OnRoomEnter?.Invoke(targetPortal.roomType);
        }
    }

    // 순간이동 - 플레이어가 문 트리거 콜라이더에 닿았을 때 호출
    public void Teleport(PlayerMove playerMove)
    {
        if (targetPortal == null) return;

        Quaternion relativeRotation = Quaternion.Inverse(spawnPoint.rotation) * playerMove.transform.rotation; // 입구 포탈 기준 플레이어의 상대적 회전(오프셋) 계산
        Quaternion flipRotation = Quaternion.Euler(0, 180f, 0);
        playerMove.PlayerTeleport(targetPortal.spawnPoint.position, targetPortal.spawnPoint.rotation * relativeRotation * flipRotation); // 오프셋 곱해서 회전(들어갈 때의 방향 유지되도록)
    }

    // 외부(매니저)에서 목적지를 강제로 바꿀 때 사용
    public void SetDestination(Portal newTarget)
    {
        targetPortal = newTarget;
    }
}
