using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class SaveSystemManager : MonoBehaviour
{
    public static SaveSystemManager Instance { get; private set; }

    private const string SAVE_KEY = "PlayerSaveData";
    private PlayerSaveContainer currentSaveContainer;
    private int _currentPlayingIdx = -1; // 현재 플레이어가 진행 중인 세이브포인트의 리스트 인덱스
    public int CurrentPlayingIdx => _currentPlayingIdx;
    public static bool IsLoadGameMode { get; set; } = false; // 세이브 데이터를 불러오는 중인지 체크

    private ESavePointType pendingLoadType;
    private bool hasPendingLoad = false; // 로드가 예약되어 있는지 여부

    private GameObject playerObj;
    private PlayerStat playerStat;
    private InventorySlot[] inventorySlots;

    [SerializeField] private InventoryManager inventoryManager;
    private PlayerMutation playerMutation;
    private ObjectiveManager objectiveManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadContainerOnBoot();
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// 게임 부팅 시 로컬에 저장된 최신 세이브 컨테이너를 복구
    /// </summary>
    private void LoadContainerOnBoot()
    {
        string jsonData = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(jsonData))
        {
            currentSaveContainer = JsonUtility.FromJson<PlayerSaveContainer>(jsonData);
            Debug.Log($"[부팅 로드] 직전 플레이어 [{currentSaveContainer.playerName}]의 데이터를 메모리에 로드했습니다.");
        }
        else
        {
            currentSaveContainer = null;
            Debug.Log("[부팅 로드] 기존 세이브 데이터가 없습니다.");
        }
    }

    /// <summary>
    /// 브릿지가 씬 시작 시 실시간 컴포넌트들의 주소를 등록함
    /// </summary>
    public void RegisterInGameReferences(GameObject player, PlayerStat stats, InventorySlot[] inventorySlots, InventoryManager inventoryManager, PlayerMutation playerMutation, ObjectiveManager objectiveManager)
    {
        this.playerObj = player;
        this.playerStat = stats;
        this.inventorySlots = inventorySlots;
        this.inventoryManager = inventoryManager;
        this.playerMutation = playerMutation;
        this.objectiveManager = objectiveManager;
        Debug.Log("[세이브 시스템] 로드/세이브를 위한 인게임 컴포넌트 등록 완료.");

        // 타이틀에서 예약된 로드가 있다면 업데이트
        if (hasPendingLoad)
        {
            hasPendingLoad = false; // 플래그 초기화
            LoadSavePoint(pendingLoadType);
        }
    }

    /// <summary>
    /// 로드할 타입의 데이터 예약
    /// </summary>
    /// <param name="type"></param>
    public void QueryLoadFromTitle(ESavePointType type)
    {
        pendingLoadType = type;
        hasPendingLoad = true;

        SetCurrentPlayingPoint(type);
    }

    /// <summary>
    /// 선택한 기점의 데이터를 씬의 오브젝트들에게 업데이트
    /// </summary>
    public void LoadSavePoint(ESavePointType type)
    {
        if (currentSaveContainer == null) return;

        // 해당 타입의 세이브 데이터 검색
        SavePointData data = currentSaveContainer.savePoints.FindIndex(x => x.savePointType == type) != -1
            ? currentSaveContainer.savePoints.Find(x => x.savePointType == type)
            : null;

        if (data == null)
        {
            Debug.LogError($"[{type}]에 해당하는 세이브 데이터가 존재하지 않습니다!");
            return;
        }

        // 현재 플레이 인덱스 타임라인 고정
        SetCurrentPlayingPoint(type);

        // [플레이어 스탯 업데이트]
        if (playerStat != null) playerStat.ApplyLoadedStats(data.playerStats.hp, data.playerStats.stamina);

        // [인벤토리 업데이트]
        if (inventorySlots != null && data.playerInventory != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] == null) continue;

                if (i < data.playerInventory.Count)
                {
                    var invData = data.playerInventory[i];

                    // 원래 하던 대로 invData.item 오브젝트를 그대로 사용합니다.
                    if (invData != null && invData.item != null)
                    {
                        inventorySlots[i].AddItem(invData.item, invData.itemcnt, invData.itemInstanceNum);
                    }
                }
            }
        }

        inventoryManager.HasRegisteredMap = data.hasRegisteredMap;
        if (inventoryManager.HasRegisteredMap) FindAnyObjectByType<MapViewController>().UnlockMap(); // 맵 잠금 해제

        // [괴물화 및 치료제 여부 업데이트]
        if (playerMutation != null)
        {
            playerMutation.CurrentStage = data.mutationStage;
            playerMutation.IsCured = data.isCureInjected;
        }

        if (data.worldItems != null)
        {
            List<WorldItemSaveData> savedItemsCopy = new List<WorldItemSaveData>(data.worldItems);

            // 현재 씬에 배치되어 있는 모든 ItemPickUp 오브젝트들을 가져옴
            ItemPickUp[] currentSceneItems = FindObjectsByType<ItemPickUp>(FindObjectsSortMode.None);

            foreach (var sceneItem in currentSceneItems)
            {
                string currentID = string.IsNullOrEmpty(sceneItem.uniqueID) ? sceneItem.gameObject.name : sceneItem.uniqueID;

                // 세이브 데이터 중에서 고유 ID가 일치하는 아이템이 있는지 찾기
                WorldItemSaveData matchData = savedItemsCopy.Find(x => x.uniqueID == currentID);

                if (matchData != null) // 세이브 데이터에 존재함
                {
                    // 위치, 회전, 스케일을 세이브 시점으로 강제 조정
                    sceneItem.transform.position = matchData.position.ToVector3();
                    sceneItem.transform.eulerAngles = matchData.rotation.ToVector3();
                    sceneItem.transform.localScale = matchData.scale.ToVector3();

                    // 매칭이 완료된 데이터는 리스트에서 제외
                    savedItemsCopy.Remove(matchData);
                }
                else // 세이브 데이터에 없음
                {
                    // 플레이어가 이미 주워갔거나 파괴된 아이템이므로 씬에서 삭제
                    Destroy(sceneItem.gameObject);
                }
            }

            Debug.Log("[로드 완료] 씬 아이템 상태 동기화 및 버려진 아이템 복구 완료.");
        }

        // [게임 매니저 시간 동기화]
        if (GameTime.Instance != null)
        {
            GameTime.Instance.SetTime(data.playTime);
        }

        Debug.Log($"[{type}] 데이터 로드 및 인게임 오브젝트 실시간 업데이트 완료!");
    }

    public void ApplySavedEnvironmentWithBridge(InGameSaveBridge saveBridge)
    {
        if (saveBridge == null || currentSaveContainer == null) return;

        bool isCrewKeyPadUnlocked = IsSavePointUnlocked(ESavePointType.CrewKeyPad);
        bool isPowerRestoration = IsSavePointUnlocked(ESavePointType.PowerRestoration);

        if (isPowerRestoration) objectiveManager.ForceCompleteObjective("RestorePower");
        else if (isCrewKeyPadUnlocked) objectiveManager.ForceCompleteObjective("EscapeCrewRoom");

        if (isCrewKeyPadUnlocked)
        {
            KeyPadController keyPad = saveBridge.CrewKeyPadController;
            if (keyPad != null)
            {
                keyPad.ForceOpen();
                Debug.Log("세이브 파일에서 선원실 탈출 확인 -> 문을 강제로 열었습니다.");
            }
        }
        if (isPowerRestoration && pendingLoadType != ESavePointType.CrewKeyPad)
        {
            PowerSwitch powerSwitch = saveBridge.PowerSwitch;
            if (powerSwitch != null)
            {
                powerSwitch.ForcePowerRestoration();
                Debug.Log("세이브 파일에서 전력 복구 확인 -> 잠수함 전력을 킵니다.");
            }
            //LightingManager.instance.LightToggle(true);
        }
    }

    /// <summary>
    /// 새 플레이어 이름으로 세이브 데이터를 초기화
    /// </summary>
    /// <param name="playerName"></param>
    public void InitSaveData(string playerName)
    {
        currentSaveContainer = new PlayerSaveContainer();
        currentSaveContainer.playerName = playerName;
        currentSaveContainer.savePoints = new List<SavePointData>();
        _currentPlayingIdx = -1;

        SaveGameData();
    }

    /// <summary>
    /// 기존 플레이어 이름과 컨테이너를 유지한 채 세이브 데이터만 초기화
    /// </summary>
    public void InitExistingPlayerSaveData(string playerName)
    {
        if (currentSaveContainer == null)
        {
            currentSaveContainer = new PlayerSaveContainer();
            currentSaveContainer.playerName = playerName;
            currentSaveContainer.savePoints = new List<SavePointData>();
            Debug.Log($"[세이브 초기화] 기존 플레이어 [{playerName}]의 데이터가 성공적으로 초기화되었습니다.");
        }
        else
        {
            Debug.Log($"[세이브 불러옴] 기존 플레이어 [{playerName}]의 데이터가 성공적으로 불러왔습니다.");
        }

        // 현재 플레이 중인 타임라인 인덱스 초기화
        _currentPlayingIdx = -1;

        SaveGameData();
    }

    /// <summary>
    /// 현재 플레이어가 진행 중인 세이브포인트의 리스트 인덱스 설정
    /// </summary>
    /// <param name="type"></param>
    public void SetCurrentPlayingPoint(ESavePointType type)
    {
        if (currentSaveContainer == null) return;

        int index = currentSaveContainer.savePoints.FindIndex(x => x.savePointType == type);
        if (index != -1) _currentPlayingIdx = index;
    }

    /// <summary>
    /// 플레이어 인벤토리 데이터 업데이트
    /// </summary>
    /// <param name="data"></param>
    private void SetPlayerInventory(SavePointData data)
    {
        if (data.playerInventory == null) data.playerInventory = new List<PlayerInventory>();
        data.playerInventory.Clear(); // 기존 데이터 삭제

        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slot = inventorySlots[i];

                if (slot != null)
                {
                    PlayerInventory inventorySlot = new PlayerInventory
                    {
                        item = slot.Item,
                        itemcnt = slot.ItemCount,
                        itemInstanceNum = slot.ItemInstanceNum
                    };
                    data.playerInventory.Add(inventorySlot);
                }
            }
        }
    }

    /// <summary>
    /// 맵에 배치된 아이템 데이터 업데이트
    /// </summary>
    /// <param name="data"></param>
    private void SetWorldItem(SavePointData data)
    {
        if (data.worldItems == null) data.worldItems = new List<WorldItemSaveData>();
        data.worldItems.Clear();

        ItemPickUp[] allItems = FindObjectsByType<ItemPickUp>(FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            WorldItemSaveData itemSaveData = new WorldItemSaveData
            {
                item = item.Item,
                uniqueID = item.uniqueID,
                isDropped = item.isDropped,
                position = new SerializableVector3(item.transform.position),
                rotation = new SerializableVector3(item.transform.eulerAngles),
                scale = new SerializableVector3(item.transform.localScale)
            };
            data.worldItems.Add(itemSaveData);
        }
        Debug.Log($"[세이브 완료] 필드 아이템 {data.worldItems.Count}개 저장됨.");
    }

    public void UpdateSavePoint(ESavePointType type, float playTime)
    {
        if (currentSaveContainer == null) return;

        string todayDate = System.DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");

        // 현재 플레이 기준점(currentPlayingIndex) 뒤에 미래 데이터가 있는지 확인
        if (_currentPlayingIdx != -1 && _currentPlayingIdx < currentSaveContainer.savePoints.Count - 1)
        {
            // 과거로 돌아가서 플레이하다가 '다음 기점'을 새로 갱신하려는 순간 현재 기준점 다음부터 있는 모든 세이브 데이터를 삭제
            int removeStartIdx = _currentPlayingIdx + 1;
            int countToRemove = currentSaveContainer.savePoints.Count - removeStartIdx;

            if (countToRemove > 0)
            {
                currentSaveContainer.savePoints.RemoveRange(removeStartIdx, countToRemove);
                Debug.Log($"[타임라인 개척] 새로운 기점 진행으로 인해, 기존 세이브 데이터 {countToRemove}개가 삭제");
            }
        }

        // 현재 리스트에 같은 유형의 기점이 이미 존재하는지 검사
        int existingIndex = currentSaveContainer.savePoints.FindIndex(x => x.savePointType == type);

        if (existingIndex != -1)
        {
            // 기존 기점 갱신 - 데이터 덮어쓰기
            SavePointData existingData = currentSaveContainer.savePoints[existingIndex];

            existingData.saveDate = todayDate;
            existingData.playTime = playTime;

            // 플레이어 데이터 매핑
            existingData.playerPosition = new SerializableVector3(playerObj.transform.position);
            existingData.playerStats.hp = playerStat.Hp;
            existingData.playerStats.stamina = playerStat.Stamina;
            SetPlayerInventory(existingData);
            existingData.hasRegisteredMap = inventoryManager.HasRegisteredMap;
            existingData.mutationStage = playerMutation.CurrentStage;
            existingData.isCureInjected = playerMutation.IsCured;

            // 괴물 데이터 매핑
            // existingData.insideMonsterPosition = new SerializableVector3(insideMonPos);
            // existingData.outsideMonsterPosition = new SerializableVector3(outsideMonPos);
            // existingData.isInsideMonsterBerserk = isBerserk;

            SetWorldItem(existingData);

            _currentPlayingIdx = existingIndex;
            Debug.Log($"<color=#32CD32><b>[세이브 기점 갱신 완료]</b></color> 📌 <b>{type}</b>\n" +
          $"──────────────────────────────────────────────────\n" +
          $"📅 <b>저장 일시:</b> {todayDate}  |  🕒 <b>플레이 타임:</b> {playTime:F1}초\n" +
          $"👤 <b>[플레이어 상태]</b>\n" +
          $"   • 📍 위치: {existingData.playerPosition.ToVector3()}\n" +
          $"   • ❤️ HP: {existingData.playerStats.hp}  |  ⚡ 스태미나: {existingData.playerStats.stamina}\n" +
          $"   • 🎒 인벤토리: {GetInventoryLogString(existingData.playerInventory)}\n" +
          $"   • 🎒 지도 아이템 등록 여부: {existingData.hasRegisteredMap}\n" +
          $"   • 🧬 괴물화 단계: {existingData.mutationStage}  |  💉 치료제 투여: {(existingData.isCureInjected ? "<color=red>YES</color>" : "NO")}\n" +
          //   $"👹 <b>[괴물 상태]</b>\n" +
          //   $"   • 🚪 내부 괴물 위치: {existingData.insideMonPos.ToVector3()}  |  💥 폭주 상태: {(existingData.isBerserk ? "<color=red>적대화</color>" : "일반")}\n" +
          //   $"   • 🌊 외부 괴물 위치: {existingData.outsideMonPos.ToVector3()}\n" +
          $"──────────────────────────────────────────────────");
        }
        else
        {
            // 새로운 기점 추가 - 인스턴스 새로 생성 후 추가
            SavePointData newData = new SavePointData
            {
                savePointType = type,
                isUnlocked = true,
                saveDate = todayDate,
                playTime = playTime,

                // 플레이어 데이터
                playerPosition = new SerializableVector3(playerObj.transform.position),
                playerStats = new PlayerStatsData
                {
                    hp = playerStat.Hp,
                    stamina = playerStat.Stamina
                },
                hasRegisteredMap = inventoryManager.HasRegisteredMap,
                mutationStage = playerMutation.CurrentStage,
                isCureInjected = playerMutation.IsCured,
            };

            SetPlayerInventory(newData);
            SetWorldItem(newData);

            currentSaveContainer.savePoints.Add(newData);
            _currentPlayingIdx = currentSaveContainer.savePoints.Count - 1; // 기준점을 리스트의 맨 끝으로 이동
            Debug.Log($"<color=#32CD32><b>[세이브 기점 생성 완료]</b></color> 📌 <b>{type}</b>\n" +
          $"──────────────────────────────────────────────────\n" +
          $"📅 <b>저장 일시:</b> {todayDate}  |  🕒 <b>플레이 타임:</b> {playTime:F1}초\n" +
          $"👤 <b>[플레이어 상태]</b>\n" +
          $"   • 📍 위치: {newData.playerPosition.ToVector3()}\n" +
          $"   • ❤️ HP: {newData.playerStats.hp}  |  ⚡ 스태미나: {newData.playerStats.stamina}\n" +
          $"   • 🎒 인벤토리: {GetInventoryLogString(newData.playerInventory)}\n" +
          $"   • 🎒 지도 아이템 등록 여부: {newData.hasRegisteredMap}\n" +
          $"   • 🧬 괴물화 단계: {newData.mutationStage}  |  💉 치료제 투여: {(newData.isCureInjected ? "<color=red>YES</color>" : "NO")}\n" +
          //   $"👹 <b>[괴물 상태]</b>\n" +
          //   $"   • 🚪 내부 괴물 위치: {existingData.insideMonPos.ToVector3()}  |  💥 폭주 상태: {(existingData.isBerserk ? "<color=red>적대화</color>" : "일반")}\n" +
          //   $"   • 🌊 외부 괴물 위치: {existingData.outsideMonPos.ToVector3()}\n" +
          $"──────────────────────────────────────────────────");
        }

        SaveGameData(); // 세이브 저장
    }

    /// <summary>
    /// 인벤토리 리스트를 에러 없이 안전하게 문자열로 가공해주는 헬퍼 함수
    /// </summary>
    private string GetInventoryLogString(List<PlayerInventory> inventoryList)
    {
        if (inventoryList == null || inventoryList.Count == 0) return "비어 있음";

        List<string> itemNames = new List<string>();
        for (int i = 0; i < inventoryList.Count; i++)
        {
            if (inventoryList[i].item != null)
            {
                // 아이템 이름 혹은 ToString() 정보 추출 (보유 개수 포함 가능)
                itemNames.Add($"{inventoryList[i].item.name}(x{inventoryList[i].itemcnt})");
            }
            else
            {
                itemNames.Add("EmptySlot");
            }
        }
        return string.Join(" | ", itemNames);
    }

    /// <summary>
    /// 세이브 데이터 저장
    /// </summary>
    public void SaveGameData()
    {
        if (currentSaveContainer == null) return;

        string jsonData = JsonUtility.ToJson(currentSaveContainer, true);
        PlayerPrefs.SetString(SAVE_KEY, jsonData);
        PlayerPrefs.Save();
    }

    public List<SavePointData> GetSavePoints() => currentSaveContainer?.savePoints;

    public SavePointData GetCurrentSavePointData()
    {
        if (currentSaveContainer == null || _currentPlayingIdx == -1 || _currentPlayingIdx >= currentSaveContainer.savePoints.Count)
            return null;

        return currentSaveContainer.savePoints[_currentPlayingIdx];
    }

    /// <summary>
    /// 특정 세이브포인트가 현재 세이브 데이터에 해금(기록)되어 있는지 확인하는 함수
    /// </summary>
    public bool IsSavePointUnlocked(ESavePointType type)
    {
        if (currentSaveContainer == null) return false;

        // 해당 타입의 세이브 데이터 검색
        SavePointData data = currentSaveContainer.savePoints.FindIndex(x => x.savePointType == type) != -1
            ? currentSaveContainer.savePoints.Find(x => x.savePointType == type)
            : null;

        if (data == null) return false;
        else return true;
    }
}

