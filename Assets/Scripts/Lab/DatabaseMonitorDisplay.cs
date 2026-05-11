using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.EditorTools;

public enum DatabaseMonitorState
{
    None, // 모니터 화면 비활성화
    Login, // 로그인 화면
    Main // 로그인 후 데이터베이스 화면
}

public class DatabaseMonitorDisplay : MonoBehaviour
{
    public static DatabaseMonitorDisplay instance;

    [Header("게임 UI")]
    [SerializeField] private GameObject statUI;
    [SerializeField] private GameObject crosshairUI;
    [SerializeField] private GameObject actionText;

    [Header("공통 UI")]
    [SerializeField] private GameObject monitorScreen; // 모니터 화면 오브젝트
    [SerializeField] private TextMeshProUGUI timeText; // 시간 텍스트

    [Header("로그인 화면")]
    [SerializeField] private GameObject loginPanel; // 로그인 패널
    public TMP_InputField idText; // 아이디 입력필드
    public TMP_InputField pwText; // 비밀번호 입력필드
    [SerializeField] private TextMeshProUGUI loginFailedText; // 로그인 실패 텍스트

    [Header("메인 화면")]
    /* 패널 */
    [SerializeField] private GameObject mainPanel; // 메인 패널
    [SerializeField] private GameObject creatureInfoPanel; // 생물 정보 패널
    [SerializeField] private GameObject experimentRecordPanel; // 실험 기록 패널
    /* 버튼 이미지 */
    [SerializeField] private Sprite defaultPanelImg; // 생물 버튼 기본 이미지
    [SerializeField] private Sprite selectedPanelImg; // 생물 버튼 선택 이미지
    [SerializeField] private Sprite sampleBtnDefaultImg; // 샘플 버튼 기본 이미지
    [SerializeField] private Sprite sampleBtnSelectedImg; // 샘플 버튼 선택 이미지
    /* 생물 종 선택 버튼과 아이콘 */
    [SerializeField] private Image[] creatureBtnsImg; // 생물 버튼 패널 이미지
    [SerializeField] private Image[] creatureIconsImg; // 생물 아이콘 이미지
    /* 생물 정보 */
    [SerializeField] private Image creatureImg; // 생물 이미지
    [SerializeField] private TextMeshProUGUI creatureNameText; // 생물 이름 텍스트
    [SerializeField] private TextMeshProUGUI biogicalInfoText; // 생체 정보 텍스트
    [SerializeField] private GameObject[] sampleBtns; // 샘플 버튼 그룹
    [SerializeField] private GameObject[] sampleQtys; // 샘플 수량 그룹
    [SerializeField] private GameObject[] sampleInfos; // 샘플 상세 정보 그룹
    [SerializeField] private GameObject storageLocationPanel; // 창고 위치 패널
    [SerializeField] private TextMeshProUGUI storageLocationText; // 창고 위치
    /* 실험 기록 */
    [SerializeField] private GameObject[] userRecordPanels; // 사용자별 실험 기록 패널
    [SerializeField] private TextMeshProUGUI reportTitleText; // 실험 기록 제목 텍스트
    [SerializeField] private TextMeshProUGUI reportContentText; // 실험 기록 데이터 텍스트

    [Header("초기화")]
    [SerializeField] private CreatureData defaultCreatureData; // 초기 생물 데이터 - 아비설 면역 문어

    private GameTime _gameTime;
    private DatabaseMonitorController _monitorController;
    private int _loggedInUserIndex = -1; // 현재 로그인한 사용자 인덱스

    private Color whiteColor = Color.white;
    private Color defaultCreatureIconColor = new Color32(78, 78, 78, 255);
    private Color selectedSampleTextColor = new Color32(0, 57, 20, 255);

    private void Awake()
    {
        // 싱글톤 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _gameTime = FindObjectOfType<GameTime>();
        _monitorController = GetComponent<DatabaseMonitorController>();

        monitorScreen.SetActive(false);
        loginPanel.SetActive(false);
        mainPanel.SetActive(false);
    }

    private void Update()
    {
        if (monitorScreen.activeSelf)
        {
            float timeStart = 36000f + _gameTime.TimeSinceStart; // 10:00:00부터 시작
            timeText.text = $"{FormatTime(timeStart)}";
        }
    }

    /// <summary>
    /// 초 단위 시간을 "HH:MM:SS" 형식으로 변환
    /// </summary>
    private string FormatTime(float sec)
    {
        int hours = Mathf.FloorToInt(sec / 3600f) % 24;
        int minutes = Mathf.FloorToInt((sec % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(sec % 60f);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }

    /// <summary>
    /// 모니터 화면과 UI 패널 상태 설정
    /// <para> - NONE: 모니터 화면 비활성화</para>
    /// <para> - Login: 로그인 패널 활성화</para>
    /// <para> - Main: 메인 패널 활성화</para>
    /// </summary>
    public void ShowPanel(DatabaseMonitorState state)
    {
        if (state == DatabaseMonitorState.None)
        {
            monitorScreen.SetActive(false);
            statUI.SetActive(true);
            crosshairUI.SetActive(true);
            actionText.SetActive(true);
        }
        else
        {
            monitorScreen.SetActive(true);
            statUI.SetActive(false);
            crosshairUI.SetActive(false);
            actionText.SetActive(false);
        }

        switch (state)
        {
            case DatabaseMonitorState.None:
                loginPanel.SetActive(false);
                mainPanel.SetActive(false);
                break;
            case DatabaseMonitorState.Login:
                loginPanel.SetActive(true);
                mainPanel.SetActive(false);
                break;
            case DatabaseMonitorState.Main:
                loginPanel.SetActive(false);
                mainPanel.SetActive(true);

                // 로그인 후 메인 진입 시 초기화 로직
                // 1. 패널 활성화 상태 초기화
                creatureInfoPanel.SetActive(true);
                experimentRecordPanel.SetActive(false);

                // 2. 생물 선택 UI 초기화
                UpdateCreatureSelectionUI(MonitorUIElement.Octopus);

                // 3. 생물 정보 UI 초기화
                UpdateCreatureInfoUI(defaultCreatureData);

                // 4. 샘플 선택 UI 초기화
                UpdateSampleSelectionUI(MonitorUIElement.Sample1);

                // 5. 실험 기록 UI 초기화
                reportTitleText.text = "";
                reportContentText.text = "";
                ResetAllReportButtons();
                break;
        }
    }

    /// <summary>
    /// 로그인 입력 필드 초기화
    /// </summary>
    public void ResetLoginFields()
    {
        idText.text = "";
        pwText.text = "";
    }

    /// <summary>
    /// 현재 로그인한 사용자 인덱스 설정
    /// </summary>
    /// <param name="index"></param>
    public void SetUserIndex(int index)
    {
        _loggedInUserIndex = index;
    }

    /// <summary>
    /// UI 요소 선택 시 처리
    /// <para> - 로그인 버튼: 로그인 처리</para>
    /// <para> - 로그아웃 버튼: 로그아웃 처리</para>
    /// <para> - 생물 버튼: 생물 선택 UI 업데이트, 생물 정보 UI 업데이트</para>
    /// <para> - 샘플 버튼: 샘플 선택 UI 업데이트</para>
    /// <para> - 실험 기록 버튼: 실험 기록 UI 업데이트</para>
    /// </summary>
    public void OnUISelected(MonitorUIElement uiType, CreatureData creatureData = null)
    {
        Debug.Log("Selected UI: " + uiType + (creatureData != null ? ", Creature: " + creatureData.name : ""));
        switch (uiType)
        {
            case MonitorUIElement.LoginBtn:
                // 로그인 버튼 클릭 시 처리
                // 로그인 성공 여부에 따라 로그인 실패 텍스트 활성화
                if (!_monitorController.TryLogin()) loginFailedText.gameObject.SetActive(true);
                else loginFailedText.gameObject.SetActive(false);
                break;
            case MonitorUIElement.LogoutBtn:
                _monitorController.Logout();
                break;
            case MonitorUIElement.Octopus:
            case MonitorUIElement.Dragonfish:
            case MonitorUIElement.Seahorse:
            case MonitorUIElement.Crab:
            case MonitorUIElement.Leech:
                // 생물 버튼 클릭 시 처리
                UpdateCreatureSelectionUI(uiType);
                if (creatureData != null) UpdateCreatureInfoUI(creatureData);
                break;
            case MonitorUIElement.Sample1:
            case MonitorUIElement.Sample2:
            case MonitorUIElement.Sample3:
                // 샘플 버튼 클릭 시 처리
                UpdateSampleSelectionUI(uiType);
                break;
            case MonitorUIElement.ExperimentRecordBtn:
                // 실험 기록 버튼 클릭 시 처리
                UpdateExperimentRecordUI();
                break;
        }
    }

    /// <summary>
    /// 생물 선택 UI 업데이트
    /// <para> - 생물 정보 패널 활성화 </para>
    /// <para> - 선택된 버튼은 selectedPanel로 변경, 아이콘 색상 변경</para>
    /// <para> - 선택되지 않은 버튼은 defaultPanel로 변경, 아이콘 색상 변경</para>
    /// </summary>
    /// <param name="selectedType"></param>
    private void UpdateCreatureSelectionUI(MonitorUIElement selectedType)
    {
        // 생물 정보 패널 활성화 및 실험 기록 패널 비활성화
        creatureInfoPanel.SetActive(true);
        experimentRecordPanel.SetActive(false);

        int selectedIndex = (int)selectedType - (int)MonitorUIElement.Octopus; // 생물 버튼은 Octopus부터 시작하므로 인덱스 계산

        for (int i = 0; i < creatureBtnsImg.Length; i++)
        {
            if (i == selectedIndex) // 선택된 버튼이면
            {
                creatureBtnsImg[i].sprite = selectedPanelImg;
                creatureIconsImg[i].color = whiteColor;
            }
            else // 선택되지 않은 버튼이면
            {
                creatureBtnsImg[i].sprite = defaultPanelImg;
                creatureIconsImg[i].color = defaultCreatureIconColor;
            }
        }
    }

    /// <summary>
    /// 생물 정보 UI 업데이트
    /// <para> - 생물 이미지, 이름, 생체 정보 업데이트</para>
    /// <para> - 샘플 정보 업데이트 (수량, 상세 정보, 창고 선반 위치)</para>
    /// </summary>
    /// <param name="creatureData"></param>
    private void UpdateCreatureInfoUI(CreatureData creatureData)
    {
        creatureImg.sprite = creatureData.CreatureImage;
        creatureNameText.text = creatureData.CreatureName;
        biogicalInfoText.text = creatureData.BiologicalInfo;

        if (creatureData.Samples != null)
        {
            for (int i = 0; i < sampleBtns.Length; i++)
            {
                TextMeshProUGUI nameText = sampleBtns[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                nameText.text = creatureData.Samples[i].sampleName;

                int currnetStock = WarehouseManager.instance.GetStockCount(creatureData.CreatureName, creatureData.Samples[i].sampleName);

                sampleQtys[i].GetComponentInChildren<TextMeshProUGUI>().text = currnetStock.ToString();
                sampleInfos[i].GetComponentInChildren<TextMeshProUGUI>().text = creatureData.Samples[i].sampleDetail;
            }

            // 창고 위치 UI 업데이트
            storageLocationText.text = $"STORAGE 01 SECTOR <b>{creatureData.ShelfLocation}</b>";

            for (int i = 0; i < storageLocationPanel.transform.childCount; i++)
            {
                storageLocationPanel.transform.GetChild(i).GetComponent<Image>().sprite = sampleBtnDefaultImg;
            }
            storageLocationPanel.transform.GetChild(creatureData.ShelfIndex).GetComponent<Image>().sprite = sampleBtnSelectedImg;
        }

        ResetSampleUI(); // 생물 정보 업데이트 시 샘플 UI 초기화
    }

    /// <summary>
    /// 샘플 선택 UI 업데이트
    /// <para> - 선택된 버튼은 sampleBtnSelected로 변경, 텍스트 색상 변경, 상세 정보 패널 활성화</para>
    /// <para> - 선택되지 않은 버튼은 sampleBtnDefault로 변경, 텍스트 색상 변경, 상세 정보 패널 비활성화</para>
    /// </summary>
    /// <param name="selectedType"></param>
    private void UpdateSampleSelectionUI(MonitorUIElement selectedType)
    {
        int selectedIndex = (int)selectedType - (int)MonitorUIElement.Sample1; // 샘플 버튼은 Sample1부터 시작하므로 인덱스 계산

        for (int i = 0; i < sampleBtns.Length; i++)
        {
            TextMeshProUGUI text1 = sampleBtns[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI text2 = sampleBtns[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI qtyText = sampleQtys[i].GetComponentInChildren<TextMeshProUGUI>();

            if (i == selectedIndex) // 선택된 버튼이면
            {
                // 샘플 버튼 및 수량 오브젝트 - 흰색 배경 + 초록 텍스트
                sampleBtns[i].GetComponent<Image>().sprite = sampleBtnSelectedImg;
                sampleQtys[i].GetComponent<Image>().sprite = sampleBtnSelectedImg;

                text1.color = selectedSampleTextColor;
                text2.color = selectedSampleTextColor;
                qtyText.color = selectedSampleTextColor;

                sampleInfos[i].SetActive(true); // 상세 정보 패널 활성화
            }
            else // 선택되지 않은 버튼이면
            {
                // 샘플 버튼 및 수량 오브젝트 - 테두리 배경 + 흰색 텍스트
                sampleBtns[i].GetComponent<Image>().sprite = sampleBtnDefaultImg;
                sampleQtys[i].GetComponent<Image>().sprite = sampleBtnDefaultImg;

                text1.color = whiteColor;
                text2.color = whiteColor;
                qtyText.color = whiteColor;

                sampleInfos[i].SetActive(false); // 상세 정보 패널 비활성화
            }
        }
    }

    /// <summary>
    /// 샘플 버튼 UI 초기화
    /// <para> - 모든 샘플 버튼과 수량 오브젝트는 기본 이미지로 변경, 텍스트 색상은 흰색으로 변경</para>
    /// <para> - 모든 샘플 상세 정보 패널 비활성화</para>
    /// </summary>
    private void ResetSampleUI()
    {
        for (int i = 0; i < sampleBtns.Length; i++)
        {
            sampleBtns[i].GetComponent<Image>().sprite = sampleBtnDefaultImg;
            sampleQtys[i].GetComponent<Image>().sprite = sampleBtnDefaultImg;

            TextMeshProUGUI text1 = sampleBtns[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI text2 = sampleBtns[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI qtyText = sampleQtys[i].GetComponentInChildren<TextMeshProUGUI>();

            text1.color = whiteColor;
            text2.color = whiteColor;
            qtyText.color = whiteColor;

            sampleInfos[i].SetActive(false);
        }
    }

    /// <summary>
    /// 실험 기록 UI 업데이트
    /// <para> - 로그인한 사용자에 해당하는 실험 기록 패널만 활성화</para>
    /// </summary>
    private void UpdateExperimentRecordUI()
    {
        Debug.Log("실험 기록 UI 업데이트 - 사용자 인덱스: " + _loggedInUserIndex);
        creatureInfoPanel.SetActive(false);
        experimentRecordPanel.SetActive(true);

        for (int i = 0; i < userRecordPanels.Length; i++)
        {
            userRecordPanels[i].SetActive(i == _loggedInUserIndex); // 로그인한 사용자에 해당하는 패널만 활성화
        }
    }

    /// <summary>
    /// 보고서 버튼 클릭 시 호출될 함수
    /// </summary>
    /// <param name="data">버튼에 할당된 ReportData 에셋</param>
    public void OnReportButtonClicked(ReportButton selectedBtn)
    {
        if (selectedBtn == null || selectedBtn.reportData == null) return;

        // 실험 기록 보고서 텍스트 업데이트
        reportTitleText.text = selectedBtn.reportData.title;
        reportContentText.text = selectedBtn.reportData.content;

        // 현재 활성화된 계정 패널 내의 모든 버튼 이미지 초기화
        GameObject currentPanel = userRecordPanels[_loggedInUserIndex];
        ReportButton[] allButtonsInPanel = currentPanel.GetComponentsInChildren<ReportButton>();

        foreach (ReportButton btn in allButtonsInPanel)
        {
            btn.SetHighlight(false, selectedPanelImg, defaultPanelImg);
        }

        // 클릭된 버튼만 선택 이미지로 변경
        selectedBtn.SetHighlight(true, selectedPanelImg, defaultPanelImg);
    }

    /// <summary>
    /// 모든 계정의 모든 보고서 버튼을 기본 이미지로 초기화
    /// </summary>
    private void ResetAllReportButtons()
    {
        foreach (GameObject panel in userRecordPanels)
        {
            ReportButton[] buttons = panel.GetComponentsInChildren<ReportButton>();
            foreach (ReportButton btn in buttons)
            {
                btn.SetHighlight(false, selectedPanelImg, defaultPanelImg);
            }
        }
    }
}
