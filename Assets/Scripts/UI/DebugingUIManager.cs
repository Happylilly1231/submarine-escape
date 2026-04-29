using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugingUIManager : MonoBehaviour
{
    public static DebugingUIManager Instance { get; private set; }

    [SerializeField] private Door crewRoomDoor;
    [SerializeField] private Button unlockCrewRoomDoorButton;
    [SerializeField] private Button escapeBackroomButton;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 버튼 할당
        unlockCrewRoomDoorButton.onClick.AddListener(UnlockCrewRoomDoor);
        escapeBackroomButton.onClick.AddListener(EscapeBackroom);
    }

    /// <summary>
    /// 선원실 문 잠금 해제
    /// </summary>
    public void UnlockCrewRoomDoor()
    {
        crewRoomDoor.isLocked = false;
        Debug.Log("[Debug] ✅ 성공 - 선원실 문 잠금 해제 완료");
    }

    /// <summary>
    /// 백룸 탈출
    /// </summary>
    public void EscapeBackroom()
    {
        if (BackroomManager.Instance.isPlayingBackroom)
        {
            BackroomManager.Instance.EscapeBackroom(SubmarineInGameManager.instance.playerMove);
            Debug.Log("[Debug] ✅ 성공 - 백룸 탈출");
        }
        else
            Debug.Log("[Debug] ❌ 실패 - 백룸 플레이 중이 아닙니다!");
    }
}
