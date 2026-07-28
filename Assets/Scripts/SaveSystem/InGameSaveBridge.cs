using UnityEngine;
using System.Collections;

public class InGameSaveBridge : MonoBehaviour
{
    [SerializeField] private Door mainSceneCrewRoomDoor; // 선원실 문

    [Header("플레이어")]
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private InventorySlot[] inventorySlots;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayerMutation playerMutation;

    [Header("Puzzle References")]
    [SerializeField] private KeyPadController crewKeyPadController;
    [SerializeField] private PowerSwitch powerSwitch;
    [SerializeField] private ObjectiveManager objectiveManager;

    public KeyPadController CrewKeyPadController => crewKeyPadController;
    public PowerSwitch PowerSwitch => powerSwitch;
    public ObjectiveManager ObjectiveManager => objectiveManager;

    private IEnumerator Start()
    {
        if (MenuUIController.instance != null)
        {
            DebuggingUIManager debugUI = MenuUIController.instance.GetComponent<DebuggingUIManager>();

            if (debugUI != null) debugUI.RegisterCrewRoomDoor(mainSceneCrewRoomDoor);
            debugUI.RegisterScript(powerSwitch, objectiveManager);
        }
        if (SavePointManager.Instance != null)
        {
            // 세이브 시스템에 현재 씬 오브젝트 주소들 먼저 컴포넌트 단위로 등록
            SavePointManager.Instance.RegisterInGameReferences(player, playerStat, inventorySlots, inventoryManager, playerMutation, objectiveManager);

            yield return null;
            // 세이브 로드를 통해 들어온 경우에만 세이브 환경을 적용
            if (SavePointManager.Instance.IsLoadGameMode)
            {
                // 씬이 시작될 때 매니저에게 직접 맵 환경을 복구하라고 명령을 내림
                SavePointManager.Instance.ApplySavedEnvironmentWithBridge(this);
            }
        }
    }
}
