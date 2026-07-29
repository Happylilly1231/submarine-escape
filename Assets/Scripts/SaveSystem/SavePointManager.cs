using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SavePointManager : MonoBehaviour
{
    public static SavePointManager Instance { get; private set; }

    private static string saveFilePath => Path.Combine(Application.persistentDataPath, "SavePointDataCollection.json");

    private SavePointDataCollection dataCollection;
    private int _currentPlayingIdx = -1; // 현재 플레이 중인 세이브포인트 리스트 인덱스
    public int CurrentPlayingIdx => _currentPlayingIdx;
    public bool IsLoadGameMode { get; set; } = false; // 로드 모드 여부

    private ESavePointType pendingLoadType;
    private bool hasPendingLoad = false; // 로드 예약 여부

    // 인게임 실시간 참조 컴포넌트
    private GameObject playerObj;
    private PlayerStat playerStat;
    private InventorySlot[] inventorySlots;
    private InventoryManager inventoryManager;
    private PlayerMutation playerMutation;
    private ObjectiveManager objectiveManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllSavePoints();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 전체 세이브 포인트 데이터 불러오기 및 기본값 초기화
    /// </summary>
    public List<SavePointData> LoadAllSavePoints()
    {
        if (!File.Exists(saveFilePath)) // 파일이 없으면 초기화된 데이터 리스트 반환
        {
            dataCollection = CreateDefaultSavePointData();
            Debug.Log("세이브 포인트 데이터 생성됨");
            return dataCollection.savePointList;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            dataCollection = JsonUtility.FromJson<SavePointDataCollection>(json);

            ValidateMissingSavePoints(dataCollection);
            return dataCollection.savePointList;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystemManager] 데이터 불러오기 실패: {e.Message}");
            dataCollection = CreateDefaultSavePointData();
            return dataCollection.savePointList;
        }
    }

    /// <summary>
    /// 세이브 파일이 없을 경우 모든 세이브 포인트 초기화하여 생성
    /// </summary>
    private SavePointDataCollection CreateDefaultSavePointData()
    {
        SavePointDataCollection newData = new SavePointDataCollection();

        foreach (ESavePointType type in Enum.GetValues(typeof(ESavePointType)))
        {
            SavePointData defaultPoint = new SavePointData
            {
                savePointType = type,
                isUnlocked = false,
                saveDate = "",
                playTime = 0f,
                playerPosition = new SerializableVector3(new Vector3(9.2f, 0.12f, 4.5f)),
                playerStats = new PlayerStatsData(),
                mutationStage = 0,
                isCureInjected = false
            };

            newData.savePointList.Add(defaultPoint);
        }

        dataCollection = newData;
        SaveAllData();
        return newData;
    }

    /// <summary>
    /// 세이브 포인트 타입이 추가되었을 때 누락된 세이브 포인트 자동 보완
    /// </summary>
    private void ValidateMissingSavePoints(SavePointDataCollection collection)
    {
        bool isUpdated = false;
        foreach (ESavePointType type in Enum.GetValues(typeof(ESavePointType)))
        {
            if (!collection.savePointList.Exists(s => s.savePointType == type))
            {
                collection.savePointList.Add(new SavePointData
                {
                    savePointType = type,
                    isUnlocked = false
                });
                isUpdated = true;
            }
        }

        if (isUpdated) SaveAllData();
    }

    /// <summary>
    /// 인게임 씬 진입 시 브릿지가 컴포넌트들을 등록
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

        // 예약된 로드가 있다면 업데이트
        if (hasPendingLoad)
        {
            hasPendingLoad = false; // 플래그 초기화
            LoadSavePoint(pendingLoadType);
        }
    }

    /// <summary>
    /// 씬 전환 직전 로드 예약
    /// </summary>
    public void QueryLoadFromTitle(ESavePointType type)
    {
        pendingLoadType = type;
        hasPendingLoad = true;
        IsLoadGameMode = true;

        SetCurrentPlayingPoint(type);
    }

    /// <summary>
    /// 지정한 세이브 포인트 데이터를 인게임에 적용
    /// </summary>
    public void LoadSavePoint(ESavePointType type)
    {
        if (dataCollection == null) LoadAllSavePoints();

        SavePointData data = dataCollection.savePointList.Find(x => x.savePointType == type);

        if (data == null || !data.isUnlocked)
        {
            Debug.LogError($"[{type}] 해금되지 않았거나 존재하지 않는 세이브 데이터입니다");
            return;
        }

        // 현재 플레이 인덱스 타임라인 고정
        SetCurrentPlayingPoint(type);

        // [플레이어 업데이트]
        // 플레이어 위치
        if (playerObj != null)
        {
            playerObj.transform.position = data.playerPosition.ToVector3();
        }
        // 플레이어 스탯
        if (playerStat != null && data.playerStats != null)
        {
            playerStat.ApplyLoadedStats(data.playerStats.hp, data.playerStats.stamina);
        }

        // [인벤토리 업데이트]
        if (inventorySlots != null && data.playerInventory != null)
        {
            // Resources/Items 폴더 내 모든 Item 에셋을 불러옴
            Item[] allItems = Resources.LoadAll<Item>("Items");

            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] == null) continue;

                if (i < data.playerInventory.Count)
                {
                    var invData = data.playerInventory[i];
                    if (invData != null && !string.IsNullOrEmpty(invData.itemName))
                    {
                        // Item.itemName 필드와 일치하는 에셋 찾기
                        Item itemData = System.Array.Find(allItems, x => x.ItemName == invData.itemName);
                        if (itemData != null)
                        {
                            inventorySlots[i].AddItem(itemData, invData.itemcnt, invData.itemInstanceNum);
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveSystem] '{invData.itemName}' 이름을 가진 Item 에셋을 찾을 수 없습니다.");
                        }
                    }
                }
            }
        }

        // [괴물화 및 치료제 여부 업데이트]
        if (playerMutation != null)
        {
            playerMutation.CurrentStage = data.mutationStage;
            playerMutation.IsCured = data.isCureInjected;
        }

        // [씬 내부 아이템 복구]
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
        }

        // [게임 시간 동기화]
        if (GameTime.Instance != null)
        {
            GameTime.Instance.SetTime(data.playTime);
        }

        // [ 목표 복구 ]
        if (objectiveManager != null && data.mainObjectives != null && data.mainObjectives.Count > 0)
        {
            objectiveManager.LoadObjectiveData(data.mainObjectives, data.subObjectives);
        }

        Debug.Log($"[{type}] 세이브 데이터 로드 완료!");
    }

    /// <summary>
    /// 퍼즐 및 맵 환경 동기화
    /// </summary>
    public void ApplySavedEnvironmentWithBridge(InGameSaveBridge saveBridge)
    {
        if (saveBridge == null || dataCollection == null) return;

        // [세이브 데이터 가져오기]
        SavePointData targetData = GetSavePointData(pendingLoadType);

        if (targetData != null)
        {
            // [플레이어 노트 UI 데이터 복구]
            if (PlayerNoteManager.instance != null && targetData.playerNoteData != null)
            {
                PlayerNoteManager.instance.LoadPlayerNoteData(targetData.playerNoteData);
                Debug.Log($"[{pendingLoadType}] 플레이어 노트 UI 동기화 완료");
            }
        }

        // 선원실 탈출 해제와 전력 복구는 이후 추가될 목표를 고려한다고 해도 선형적이므로(어뢰 발사나 치료제나 다 이 2개가 우선적으로 되어야 함) >= 비교로 여부 판단
        bool isCrewKeyPadUnlocked = pendingLoadType >= ESavePointType.CrewKeyPad;
        bool isPowerRestoration = pendingLoadType >= ESavePointType.PowerRestoration;

        if (isCrewKeyPadUnlocked && saveBridge.CrewKeyPadController != null)
        {
            saveBridge.CrewKeyPadController.ForceOpen();
            Debug.Log("세이브 파일에서 선원실 탈출 확인 -> 문을 강제로 열었습니다.");
        }

        if (isPowerRestoration && saveBridge.PowerSwitch != null)
        {
            saveBridge.PowerSwitch.ForcePowerRestoration();
            Debug.Log("세이브 파일에서 전력 복구 확인 -> 잠수함 전력을 킵니다.");
        }
    }

    /// <summary>
    /// 세이브 포인트 지점에 도달 시 데이터 저장 및 갱신
    /// </summary>
    public void UpdateSavePoint(ESavePointType type, float playTime)
    {
        if (dataCollection == null) LoadAllSavePoints();

        string todayDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        int targetIndex = dataCollection.savePointList.FindIndex(x => x.savePointType == type);

        SavePointData targetData;

        if (targetIndex != -1 && dataCollection.savePointList[targetIndex].isUnlocked)
        {
            // [기존 해금 기점 재도달] 데이터만 현재 상태로 '갱신'
            targetData = dataCollection.savePointList[targetIndex];
            _currentPlayingIdx = targetIndex;
            Debug.Log($"<color=#32CD32><b>[세이브 기점 갱신]</b></color> 📌 {type}");
        }
        else
        {
            // [새로운 기점 진입] 현재 위치 이후의 타임라인 정리 후 새 데이터 기록
            if (_currentPlayingIdx != -1 && _currentPlayingIdx < dataCollection.savePointList.Count - 1)
            {
                for (int i = _currentPlayingIdx + 1; i < dataCollection.savePointList.Count; i++)
                {
                    dataCollection.savePointList[i].isUnlocked = false;
                }
            }

            if (targetIndex != -1)
            {
                targetData = dataCollection.savePointList[targetIndex];
                _currentPlayingIdx = targetIndex;
            }
            else
            {
                targetData = new SavePointData { savePointType = type };
                dataCollection.savePointList.Add(targetData);
                _currentPlayingIdx = dataCollection.savePointList.Count - 1;
            }

            Debug.Log($"<color=#32CD32><b>[신규 세이브 기점 해금]</b></color> 📌 {type}");
        }

        // 데이터 채우기
        targetData.isUnlocked = true;
        targetData.saveDate = todayDate;
        targetData.playTime = playTime;

        if (playerObj != null)
            targetData.playerPosition = new SerializableVector3(playerObj.transform.position);

        if (playerStat != null)
        {
            targetData.playerStats = new PlayerStatsData
            {
                hp = playerStat.Hp,
                stamina = playerStat.Stamina
            };
        }

        if (playerMutation != null)
        {
            targetData.mutationStage = playerMutation.CurrentStage;
            targetData.isCureInjected = playerMutation.IsCured;
        }

        SetPlayerInventory(targetData);
        SetWorldItem(targetData);
        SetObjectives(targetData); // 목표 데이터 채우기

        // [단서 데이터 저장]
        if (PlayerNoteManager.instance != null)
        {
            // 💡 그냥 넘기지 않고 .Clone()으로 독립된 복사본을 만들어 저장!
            var currentData = PlayerNoteManager.instance.GetCurrentPlayerNoteData();
            if (currentData != null)
            {
                targetData.playerNoteData = currentData.Clone();
            }
        }

        SaveAllData();
    }

    private void SetPlayerInventory(SavePointData data)
    {
        if (data.playerInventory == null) data.playerInventory = new List<PlayerInventorySlot>();
        data.playerInventory.Clear();

        if (inventorySlots != null)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot != null && slot.Item != null)
                {
                    data.playerInventory.Add(new PlayerInventorySlot
                    {
                        itemName = slot.Item.ItemName,
                        itemcnt = slot.ItemCount,
                        itemInstanceNum = slot.ItemInstanceNum
                    });
                }
            }
        }
    }

    private void SetWorldItem(SavePointData data)
    {
        if (data.worldItems == null) data.worldItems = new List<WorldItemSaveData>();
        data.worldItems.Clear();

        ItemPickUp[] allItems = FindObjectsByType<ItemPickUp>(FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            if (item == null || item.Item == null) continue;

            data.worldItems.Add(new WorldItemSaveData
            {
                itemName = item.Item.ItemName,
                uniqueID = string.IsNullOrEmpty(item.uniqueID) ? item.gameObject.name : item.uniqueID,
                isDropped = item.isDropped,
                position = new SerializableVector3(item.transform.position),
                rotation = new SerializableVector3(item.transform.eulerAngles),
                scale = new SerializableVector3(item.transform.localScale)
            });
        }
    }

    // 현재 ObjectiveManager의 목표들을 SavePointData로 복사
    private void SetObjectives(SavePointData data)
    {
        if (objectiveManager == null) return;

        if (data.mainObjectives == null) data.mainObjectives = new List<ObjectiveProgress>();
        data.mainObjectives.Clear();
        foreach (var obj in objectiveManager.mainObjectives)
        {
            data.mainObjectives.Add(new ObjectiveProgress
            {
                objectiveName = obj.objectiveName,
                localizationKey = obj.localizationKey,
                isUnlocked = obj.isUnlocked,
                isCompleted = obj.isCompleted
            });
        }

        if (data.subObjectives == null) data.subObjectives = new List<ObjectiveProgress>();
        data.subObjectives.Clear();
        foreach (var obj in objectiveManager.subObjectives)
        {
            data.subObjectives.Add(new ObjectiveProgress
            {
                objectiveName = obj.objectiveName,
                localizationKey = obj.localizationKey,
                isUnlocked = obj.isUnlocked,
                isCompleted = obj.isCompleted
            });
        }
    }

    public void SaveAllData()
    {
        if (dataCollection == null) return;

        try
        {
            string json = JsonUtility.ToJson(dataCollection, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystemManager] 파일 저장 실패: {e.Message}");
        }
    }

    public void SetCurrentPlayingPoint(ESavePointType type)
    {
        if (dataCollection == null) return;

        int index = dataCollection.savePointList.FindIndex(x => x.savePointType == type);
        if (index != -1) _currentPlayingIdx = index;
    }

    public List<SavePointData> GetSavePoints() => dataCollection?.savePointList;

    public SavePointData GetSavePointData(ESavePointType type)
    {
        if (dataCollection == null) LoadAllSavePoints();
        return dataCollection.savePointList.Find(s => s.savePointType == type);
    }

    public bool IsSavePointUnlocked(ESavePointType type)
    {
        if (dataCollection == null) LoadAllSavePoints();
        SavePointData data = dataCollection.savePointList.Find(x => x.savePointType == type);
        return data != null && data.isUnlocked;
    }

    public SavePointData GetCurrentSavePointData()
    {
        if (dataCollection == null || _currentPlayingIdx == -1 || _currentPlayingIdx >= dataCollection.savePointList.Count)
            return null;

        return dataCollection.savePointList[_currentPlayingIdx];
    }

    /// <summary>
    /// 모든 세이브 포인트 데이터를 완전 초기화 (파일 삭제 및 기본값 재생성)
    /// </summary>
    public void ResetAllSaveData()
    {
        try
        {
            // 1. 세이브 파일이 존재하면 삭제
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[SavePointManager] 세이브 파일 삭제 완료");
            }

            // 2. 초기 데이터 구조 새로 생성
            dataCollection = CreateDefaultSavePointData();

            // 3. 인덱스 및 상태 초기화
            _currentPlayingIdx = -1;
            IsLoadGameMode = false;
            hasPendingLoad = false;

            Debug.Log("<color=#FF0000><b>[SavePointManager] 모든 세이브 포인트 데이터가 성공적으로 리셋되었습니다.</b></color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SavePointManager] 데이터 리셋 실패: {e.Message}");
        }
    }
}
