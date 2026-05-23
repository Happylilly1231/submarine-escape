using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ObjectiveType
{
    Main,   // 메인 목표 (탈출에 필수. ex| 선원실 탈출, 전력 복구, 치료제 투여, 탈출실 가기)
    Optional   // 서브 목표 (탈출에 필수는 아님. ex| 어뢰 발사, 어뢰관 장전) / 서브 목표는 후에 구현
}

[System.Serializable]
public class ObjectiveProgress
{
    public string objectiveName;
    public string descriptionText;
    public bool isCompleted = false;
}

public class ObjectiveManager : MonoBehaviour
{
    [Header("Objective UI")]
    public List<GameObject> objectiveSlots = new List<GameObject>();

    [Header("Objective Data")]
    public List<ObjectiveProgress> mainObjectives = new List<ObjectiveProgress>();

    private int currentStep = 0;       // 현재 진행 중인 대단계 (0: 선원실, 1: 전력, 2: 연구실, 3: 탈출)
    private int labSubClearCount = 0;  // 연구실 세부 목표 깨진 개수 카운트

    private Color completedColor = new Color32(152, 152, 152, 255);
    private bool isProcessingLabQueue = false; // 코루틴 중복 실행 방지용 플래그

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
        // 선원실 탈출하기
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "EscapeCrewRoom", descriptionText = "선원실 탈출하기" });
        // 전력 복구하기
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "RestorePower", descriptionText = "엔진실에서 전력 복구하기" });
        // 연구실 동시 목표 3개
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "CheckDB", descriptionText = "연구실에서 심해생물 DB 확인하기" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "GetSample", descriptionText = "창고에서 샘플 가져오기" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "CraftCure", descriptionText = "연구실에서 치료제 제조하기" });
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "AdministerCure", descriptionText = "올바른 치료제 투여하기" });
        // 탈출실로 가기
        mainObjectives.Add(new ObjectiveProgress { objectiveName = "GoToEscapeRoom", descriptionText = "탈출실 해치 열기" });
    }

    /// <summary>
    /// 현재 단계에 맞게 UI 업데이트
    /// </summary>
    void UpdateObjectiveUI()
    {
        // 모든 슬롯 비활성화
        foreach (var slot in objectiveSlots)
        {
            slot.SetActive(false);
        }

        if (currentStep == 0) // [단계 0] 선원실 탈출하기
        {
            SetupSlot(0, mainObjectives[0]);
        }
        else if (currentStep == 1) // [단계 1] 전력 복구하기
        {
            SetupSlot(0, mainObjectives[1]);
        }
        else if (currentStep == 2) // [단계 2] 연구실 누적 단계
        {
            // 0번(DB 확인), 1번(샘플 확보), 2번(제조), 3번(투여)
            for (int i = 0; i <= labSubClearCount; i++)
            {
                if (i >= 4) break;
                // 화면에서는 오직 현재 순서(i < labSubClearCount)보다 낮을 때만 선을 긋는다.
                bool shouldShowCompleted = (i < labSubClearCount);
                SetupSlotVisual(i, mainObjectives[2 + i], shouldShowCompleted);
            }
        }
        else if (currentStep == 3) // [단계 3] 탈출실로 가기
        {
            SetupSlot(0, mainObjectives[6]);
        }
    }

    /// <summary>
    /// 자식 오브젝트들을 찾아 값을 세팅
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <param name="data"></param>
    void SetupSlot(int slotIndex, ObjectiveProgress data)
    {
        SetupSlotVisual(slotIndex, data, data.isCompleted);
    }

    /// <summary>
    /// ui 제어
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <param name="data"></param>
    /// <param name="forceComplete"></param>
    void SetupSlotVisual(int slotIndex, ObjectiveProgress data, bool forceComplete)
    {
        if (slotIndex >= objectiveSlots.Count) return;

        GameObject slot = objectiveSlots[slotIndex];
        slot.SetActive(true);

        GameObject completeImageObj = slot.transform.GetChild(1).gameObject;
        TextMeshProUGUI textMesh = slot.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        // 목표 텍스트 업데이트
        textMesh.text = data.descriptionText;

        if (forceComplete)  // 목표 완료
        {
            completeImageObj.SetActive(true);
            textMesh.color = completedColor; // 회색
            textMesh.fontStyle = FontStyles.Strikethrough; // 선 긋기
        }
        else  // 목표 완료 못함
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
        if (target != null) Debug.Log(target.objectiveName + " " + target.isCompleted);
        if (target == null || target.isCompleted) return;

        target.isCompleted = true;

        if (currentStep == 2)
        {
            // 연구실 단계일 때는 실시간으로 UI 연쇄 반응 큐
            if (!isProcessingLabQueue)
            {
                StartCoroutine(ProcessLabObjectivesSequence());
            }
        }
        else
        {
            UpdateObjectiveUI();
            StartCoroutine(ProcessNextStepAfterDelay(name));
        }
    }

    private IEnumerator ProcessNextStepAfterDelay(string name)
    {
        if (currentStep == 0 && name == "EscapeCrewRoom")
        {
            yield return new WaitForSeconds(2f);
            currentStep = 1;
            UpdateObjectiveUI();
        }
        else if (currentStep == 1 && name == "RestorePower")
        {
            yield return new WaitForSeconds(2f);
            currentStep = 2;
            UpdateObjectiveUI();
        }
    }

    /// <summary>
    /// 연구실 전용 실시간 연쇄 업데이트 코루틴
    /// </summary>
    private IEnumerator ProcessLabObjectivesSequence()
    {
        isProcessingLabQueue = true;

        while (labSubClearCount < 4)
        {
            ObjectiveProgress currentTargetData = mainObjectives[2 + labSubClearCount];

            if (currentTargetData.isCompleted)
            {
                labSubClearCount++;
                UpdateObjectiveUI();

                yield return new WaitForSeconds(2f);

                if (labSubClearCount == 4)
                {
                    currentStep = 3;
                    UpdateObjectiveUI();
                    break;
                }
            }
            else
            {
                break;
            }
        }

        isProcessingLabQueue = false;
    }

    /// <summary>
    /// [치트/디버그 전용] 특정 목표를 즉시 완료 처리하고 시스템의 단계를 해당 위치로 강제 워프시킵니다.
    /// 이전 단계의 목표들은 화면에 보이지 않고 전부 완료 처리됩니다.
    /// </summary>
    /// <param name="name">완료 처리할 목표의 objectiveName</param>
    public void ForceCompleteObjective(string name)
    {
        int targetIndex = mainObjectives.FindIndex(x => x.objectiveName == name);

        // 선택한 목표를 포함하여 '그 이전의 모든 목표'를 전부 완료(isCompleted = true) 처리
        for (int i = 0; i <= targetIndex; i++)
        {
            mainObjectives[i].isCompleted = true;
        }

        // 목표 이름에 따라 currentStep, labSubClearCount 강제 세팅
        if (name == "EscapeCrewRoom") // 0번 완료 처리 -> 바로 1번으로 전환
        {
            currentStep = 0;
        }
        else if (name == "RestorePower") // 1번 완료 처리 -> 바로 2번(연구실 시작)으로 전환
        {
            currentStep = 1;
        }
        else if (name == "CheckDB") // 연구실 1번째 완료
        {
            currentStep = 2;
            labSubClearCount = 0;
        }
        else if (name == "GetSample") // 연구실 2번째 완료
        {
            currentStep = 2;
            labSubClearCount = 1;
        }
        else if (name == "CraftCure") // 연구실 3번째 완료
        {
            currentStep = 2;
            labSubClearCount = 2;
        }
        else if (name == "AdministerCure") // 연구실 4번째 완료
        {
            currentStep = 2;
            labSubClearCount = 3;
        }
        else if (name == "GoToEscapeRoom") // 탈출실 완료
        {
            currentStep = 3;
        }

        Debug.Log($"[치트 강제 완료] {name} 완료 처리 시도. 대단계(currentStep): {currentStep}, 연구실 카운트: {labSubClearCount}");

        UpdateObjectiveUI();

        StartCoroutine(ProcessNextStepAfterDelay(name));
    }
}
