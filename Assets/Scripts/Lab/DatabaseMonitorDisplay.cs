using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private GameObject mainPanel; // 메인 패널
    [SerializeField] private Sprite defaultPanel; // 생물 버튼 기본 이미지
    [SerializeField] private Sprite selectedPanel; // 생물 버튼 선택 이미지
    [SerializeField] private Sprite sampleBtnDefault; // 샘플 버튼 기본 이미지
    [SerializeField] private Sprite sampleBtnSelected; // 샘플 버튼 선택 이미지
    [SerializeField] private Image[] creatureBtns; // 생물 버튼 패널 이미지
    [SerializeField] private Image[] creatureIcons; // 생물 아이콘 이미지
    [SerializeField] private Image creatureImage; // 생물 이미지
    [SerializeField] private TextMeshProUGUI creatureNameText; // 생물 이름 텍스트
    [SerializeField] private TextMeshProUGUI biogicalInfoText; // 생체 정보 텍스트
    [SerializeField] private GameObject[] sampleBtns; // 샘플 버튼 그룹
    [SerializeField] private GameObject[] sampleQtys; // 샘플 수량 그룹
    [SerializeField] private GameObject[] sampleInfos; // 샘플 상세 정보 그룹
    [SerializeField] private GameObject storageLocationPanel; // 창고 위치 패널
    [SerializeField] private TextMeshProUGUI storageLocationText; // 창고 위치

    private GameTime _gameTime;
    private DatabaseMonitorController _monitorController;

    private Color whiteColor = Color.white;
    private Color defaultCreatureIconColor = new Color32(78, 78, 78, 255);
    private Color selectedSampleTextColor = new Color32(0, 57, 20, 255);

    private void Awake()
    {
        // 싱글톤 구현
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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
                actionText.SetActive(false);
                break;
            case DatabaseMonitorState.Main:
                loginPanel.SetActive(false);
                mainPanel.SetActive(true);
                actionText.SetActive(true);
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
        loginFailedText.gameObject.SetActive(false);
    }

    public void OnUISelected(MonitorUIElement uiType, CreatureData creatureData = null)
    {
        Debug.Log("Selected UI: " + uiType + (creatureData != null ? ", Creature: " + creatureData.name : ""));

        switch (uiType)
        {
            case MonitorUIElement.LoginBtn:
                // 로그인 버튼 클릭 시 처리
                if (!_monitorController.TryLogin())
                {
                    loginFailedText.gameObject.SetActive(true);
                }
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
        }
    }

    /// <summary>
    /// 생물 선택 UI 업데이트
    /// <para> - 선택된 버튼은 selectedPanel로 변경, 아이콘 색상 변경</para>
    /// <para> - 선택되지 않은 버튼은 defaultPanel로 변경, 아이콘 색상 변경</para>
    /// </summary>
    /// <param name="selectedType"></param>
    private void UpdateCreatureSelectionUI(MonitorUIElement selectedType)
    {
        int selectedIndex = (int)selectedType - (int)MonitorUIElement.Octopus; // 생물 버튼은 Octopus부터 시작하므로 인덱스 계산

        for (int i = 0; i < creatureBtns.Length; i++)
        {
            if (i == selectedIndex) // 선택된 버튼이면
            {
                creatureBtns[i].sprite = selectedPanel;
                creatureIcons[i].color = whiteColor;
            }
            else // 선택되지 않은 버튼이면
            {
                creatureBtns[i].sprite = defaultPanel;
                creatureIcons[i].color = defaultCreatureIconColor;
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
        creatureImage.sprite = creatureData.CreatureImage;
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
                storageLocationPanel.transform.GetChild(i).GetComponent<Image>().sprite = sampleBtnDefault;
            }
            storageLocationPanel.transform.GetChild(creatureData.ShelfIndex).GetComponent<Image>().sprite = sampleBtnSelected;
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
                sampleBtns[i].GetComponent<Image>().sprite = sampleBtnSelected;
                sampleQtys[i].GetComponent<Image>().sprite = sampleBtnSelected;

                text1.color = selectedSampleTextColor;
                text2.color = selectedSampleTextColor;
                qtyText.color = selectedSampleTextColor;

                sampleInfos[i].SetActive(true); // 상세 정보 패널 활성화
            }
            else // 선택되지 않은 버튼이면
            {
                // 샘플 버튼 및 수량 오브젝트 - 테두리 배경 + 흰색 텍스트
                sampleBtns[i].GetComponent<Image>().sprite = sampleBtnDefault;
                sampleQtys[i].GetComponent<Image>().sprite = sampleBtnDefault;

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
            sampleBtns[i].GetComponent<Image>().sprite = sampleBtnDefault;
            sampleQtys[i].GetComponent<Image>().sprite = sampleBtnDefault;

            TextMeshProUGUI text1 = sampleBtns[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI text2 = sampleBtns[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI qtyText = sampleQtys[i].GetComponentInChildren<TextMeshProUGUI>();

            text1.color = whiteColor;
            text2.color = whiteColor;
            qtyText.color = whiteColor;

            sampleInfos[i].SetActive(false);
        }
    }
}
