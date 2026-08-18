using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

[System.Serializable]
public class ObjectiveProgress
{
    public string objectiveName;
    public string localizationKey;    // Localization 테이블 키
    public bool isUnlocked = false;
    public bool isCompleted = false;
}

public class ObjectiveManager : MonoBehaviour
{
    [Header("Objective UI")]
    public List<GameObject> mainSlots = new List<GameObject>();
    public List<GameObject> subSlots = new List<GameObject>();

    [Header("Objective Data")]
    public List<ObjectiveProgress> mainObjectives = new List<ObjectiveProgress>();
    public List<ObjectiveProgress> subObjectives = new List<ObjectiveProgress>();

    public static ObjectiveManager Instance { get; private set; }

    private int currentStep = 0;       // 현재 진행 중인 대단계 (0: 선원실, 1: 전력, 2: 연구실, 3: 탈출)
    private int labSubClearCount = 0;  // 연구실 세부 목표 깨진 개수 카운트

    private Color completedColor = new Color32(152, 152, 152, 255); // 회색

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        InitObjectivesData();
        UpdateObjectiveUI();
    }

    /// <summary>
    /// 목표 데이터 초기화
    /// </summary>
    void InitObjectivesData()
    {
        mainObjectives.Clear();
        subObjectives.Clear();

        // [메인 목표] ----------------------------------------------------
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "EscapeCrewRoom", localizationKey = "Objective/Main/EscapeCrewRoom", isUnlocked = true });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "RestorePower", localizationKey = "Objective/Main/RestorePower" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "CheckDB", localizationKey = "Objective/Main/CheckDB" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "GetSample", localizationKey = "Objective/Main/GetSample" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "CraftCure", localizationKey = "Objective/Main/CraftCure" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "AdministerCure", localizationKey = "Objective/Main/AdministerCure" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "GoToEscapeRoom", localizationKey = "Objective/Main/GoToEscapeRoom" });

        // [서브 목표] ----------------------------------------------------
        subObjectives.Add(new ObjectiveProgress { objectiveName = "GetVictorDiary", localizationKey = "Objective/Sub/GetVictorDiary", isUnlocked = true });
        subObjectives.Add(new ObjectiveProgress { objectiveName = "UnlockCrewRoom", localizationKey = "Objective/Sub/UnlockCrewRoom" });
        subObjectives.Add(new ObjectiveProgress { objectiveName = "OperateRemoteAlarm", localizationKey = "Objective/Sub/OperateRemoteAlarm" });
        subObjectives.Add(new ObjectiveProgress { objectiveName = "FireTorpedo", localizationKey = "Objective/Sub/FireTorpedo" });
        subObjectives.Add(new ObjectiveProgress { objectiveName = "FixTorpedoRadar", localizationKey = "Objective/Sub/FixTorpedoRadar" });
        subObjectives.Add(new ObjectiveProgress { objectiveName = "LoadTorpedoTube", localizationKey = "Objective/Sub/LoadTorpedoTube" });
        subObjectives.Add(new ObjectiveProgress { objectiveName = "OperateHydraulicValve", localizationKey = "Objective/Sub/OperateHydraulicValve" });
    }

    /// <summary>
    /// 메인 목표와 서브 목표를 각각 해당 슬롯 영역에 업데이트
    /// </summary>
    public void UpdateObjectiveUI()
    {
        // 메인 목표 슬롯 전체 비활성화
        foreach (var slot in mainSlots)
        {
            slot.SetActive(false);
        }

        int mainSlotIndex = 0;
        foreach (var mainObj in mainObjectives)
        {
            if (!mainObj.isUnlocked) continue;

            SetupSlotVisual(mainSlots[mainSlotIndex], mainObj);
            mainSlotIndex++;
        }

        // 서브 목표 슬롯 전체 비활성화
        foreach (var slot in subSlots)
        {
            slot.SetActive(false);
        }

        int subSlotIndex = 0;
        foreach (var subObj in subObjectives)
        {
            if (!subObj.isUnlocked) continue;

            SetupSlotVisual(subSlots[subSlotIndex], subObj);
            subSlotIndex++;
        }
    }

    /// <summary>
    /// 해금된 목표 UI 업데이트
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="data"></param>
    private void SetupSlotVisual(GameObject slot, ObjectiveProgress data)
    {
        slot.SetActive(true);

        GameObject completeImageObj = slot.transform.GetChild(1).gameObject;
        TextMeshProUGUI textMesh = slot.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        LocalizeStringEvent localizeEvent = textMesh.GetComponent<LocalizeStringEvent>();
        localizeEvent.StringReference.SetReference("ST_UI", data.localizationKey);
        localizeEvent.RefreshString();

        if (data.isCompleted)
        {
            completeImageObj.SetActive(true);
            textMesh.color = completedColor;
            textMesh.fontStyle = FontStyles.Strikethrough; // 선 긋기
        }
        else
        {
            completeImageObj.SetActive(false);
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyles.Normal;
        }
    }

    /// <summary>
    /// 목표 클리어 호출
    /// </summary>
    /// <param name="name"></param>
    public void CompleteObjective(string name)
    {
        ObjectiveProgress target = mainObjectives.Find(x => x.objectiveName == name);

        if (target == null)
        {
            target = subObjectives.Find(x => x.objectiveName == name);
        }

        if (target == null || target.isCompleted) return;

        target.isCompleted = true;
        Debug.Log($"[목표] {target.objectiveName} 완료");

        // 목표 달성에 따른 다음 목표 해금 조건 검사
        CheckAndUnlockNextObjectives(name);
        UpdateObjectiveUI();
    }

    /// <summary>
    /// 클러이한 목표 다음에 활성화될 목표들 해금
    /// </summary>
    /// <param name="clearedObjectiveName"></param>
    private void CheckAndUnlockNextObjectives(string clearedObjectiveName)
    {
        // 1. 선원실 탈출 성공 시
        if (clearedObjectiveName == "EscapeCrewRoom")
        {
            // 메인
            UnlockObjective("RestorePower");          // 전력 복구하기 해금
            // 서브
            UnlockObjective("UnlockCrewRoom");        // 1-4 문 열기 해금
        }
        // 2. 전력 복구 성공 시
        else if (clearedObjectiveName == "RestorePower")
        {
            // 메인 - 연구실 3개 해금
            UnlockObjective("CheckDB");
            UnlockObjective("GetSample");
            UnlockObjective("CraftCure");

            // 서브
            UnlockObjective("OperateRemoteAlarm");    // 원격 경보 조작해보기 해금
            UnlockObjective("FireTorpedo");           // 어뢰 발사하기 해금
        }
        // 3. 연구실 3개 목표 성공 시
        else if (clearedObjectiveName == "CraftCure")
        {
            // 치료제 제조 시 투여 목표 해금
            UnlockObjective("AdministerCure");
        }
        else if (clearedObjectiveName == "AdministerCure")
        {
            // 올바른 치료제 투여 시 탈출실 해치 열기 해금
            UnlockObjective("GoToEscapeRoom");
        }
        // 4. 서브: 어뢰 발사하기 성공 시
        else if (clearedObjectiveName == "FireTorpedo")
        {
            UnlockObjective("FixTorpedoRadar");       // 서브: 어뢰 레이더 수리하기 해금
        }
    }

    /// <summary>
    /// 특정 목표 해금하기
    /// </summary>
    /// <param name="name"></param>
    public void UnlockObjective(string name)
    {
        ObjectiveProgress target = mainObjectives.Find(x => x.objectiveName == name);

        if (target == null)
        {
            target = subObjectives.Find(x => x.objectiveName == name);
        }

        if (target != null && !target.isUnlocked)
        {
            target.isUnlocked = true;
        }
    }

    // /// <summary>
    // /// [치트/디버그 전용] 특정 목표를 즉시 완료 처리하고 시스템의 단계를 해당 위치로 강제 워프시킵니다.
    // /// 이전 단계의 목표들은 화면에 보이지 않고 전부 완료 처리됩니다.
    // /// </summary>
    // /// <param name="name">완료 처리할 목표의 objectiveName</param>
    // public void ForceCompleteObjective(string name)
    // {
    //     int targetIndex = mainObjectives.FindIndex(x => x.objectiveName == name);

    //     if (targetIndex != -1)
    //     {
    //         // 1. [핵심] 전체 목표 상태를 리셋하거나 후속 목표들을 초기화
    //         for (int i = 0; i < mainObjectives.Count; i++)
    //         {
    //             if (i <= targetIndex)
    //             {
    //                 // 선택한 목표 포함 이전 목표들은 완료 및 해금
    //                 mainObjectives[i].isUnlocked = true;
    //                 mainObjectives[i].isCompleted = true;
    //             }
    //             else
    //             {
    //                 // 선택한 목표 '이후'의 목표들은 다시 잠금 및 미완료 상태로 리셋!
    //                 mainObjectives[i].isUnlocked = false;
    //                 mainObjectives[i].isCompleted = false;
    //             }
    //         }

    //         // 2. 선택한 목표에 연결된 '다음 목표들' 해금 (RestorePower 완료 시 CheckDB, GetSample 등 해금)
    //         CheckAndUnlockNextObjectives(name); // 다음 목표 해금 연동
    //     }
    //     else
    //     {
    //         // 서브 목표 처리
    //         ObjectiveProgress subTarget = subObjectives.Find(x => x.objectiveName == name);
    //         if (subTarget != null)
    //         {
    //             subTarget.isUnlocked = true;
    //             subTarget.isCompleted = true;
    //             CheckAndUnlockNextObjectives(name);
    //         }
    //     }

    //     // 목표 이름에 따라 currentStep, labSubClearCount 강제 세팅
    //     if (name == "EscapeCrewRoom") // 0번 완료 처리 -> 바로 1번으로 전환
    //     {
    //         currentStep = 0;
    //     }
    //     else if (name == "RestorePower") // 1번 완료 처리 -> 바로 2번(연구실 시작)으로 전환
    //     {
    //         currentStep = 1;
    //     }
    //     else if (name == "CheckDB") // 연구실 1번째 완료
    //     {
    //         currentStep = 2;
    //         labSubClearCount = 0;
    //     }
    //     else if (name == "GetSample") // 연구실 2번째 완료
    //     {
    //         currentStep = 2;
    //         labSubClearCount = 1;
    //     }
    //     else if (name == "CraftCure") // 연구실 3번째 완료
    //     {
    //         currentStep = 2;
    //         labSubClearCount = 2;
    //     }
    //     else if (name == "AdministerCure") // 연구실 4번째 완료
    //     {
    //         currentStep = 2;
    //         labSubClearCount = 3;
    //     }
    //     else if (name == "GoToEscapeRoom") // 탈출실 완료
    //     {
    //         currentStep = 3;
    //     }

    //     Debug.Log($"[치트 강제 완료] {name} 완료 처리 시도. 대단계(currentStep): {currentStep}, 연구실 카운트: {labSubClearCount}");

    //     UpdateObjectiveUI();
    // }

    /// <summary>
    /// 세이브 데이터로부터 목표 진행 상황 덮어쓰기
    /// </summary>
    public void LoadObjectiveData(List<ObjectiveProgress> savedMain, List<ObjectiveProgress> savedSub)
    {
        if (savedMain != null && savedMain.Count > 0)
        {
            mainObjectives.Clear();
            foreach (var obj in savedMain)
            {
                mainObjectives.Add(new ObjectiveProgress
                {
                    objectiveName = obj.objectiveName,
                    localizationKey = obj.localizationKey,
                    isUnlocked = obj.isUnlocked,
                    isCompleted = obj.isCompleted
                });
            }
        }

        if (savedSub != null && savedSub.Count > 0)
        {
            subObjectives.Clear();
            foreach (var obj in savedSub)
            {
                subObjectives.Add(new ObjectiveProgress
                {
                    objectiveName = obj.objectiveName,
                    localizationKey = obj.localizationKey,
                    isUnlocked = obj.isUnlocked,
                    isCompleted = obj.isCompleted
                });
            }
        }

        // 데이터 덮어씌운 후 UI 갱신
        UpdateObjectiveUI();
    }
}