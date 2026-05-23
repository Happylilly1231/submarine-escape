using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EPanelType
{
    GameMenu,   // 게임 메뉴 패널
    NameSetting, // 이름 설정 패널
    NewNameInput, // 새 이름 입력 패널
    SavePoint // 세이브 포인트 패널
}

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private Button nameSettingButton;
    [SerializeField] private Button savePointButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button endingFrameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject buttonPanel; // 버튼 패널 (시작, 엔딩 갤러리, 종료 버튼이 포함된 패널)
    [SerializeField] private GameObject playerNameSettingsPanel; // 플레이어 이름 설정 패널
    [SerializeField] private GameObject playerNameInputPanel; // 플레이어 이름 입력 패널
    [SerializeField] private GameObject savePointPanel; // 세이브 포인트 패널
    [SerializeField] private Button defaultNameButton; // 기존 이름 버튼
    [SerializeField] private Button newNameButton; // 새 이름 버튼
    [SerializeField] private TextMeshProUGUI defaultText;
    [SerializeField] private TextMeshProUGUI defaultNameText; // 기존 이름 텍스트
    [SerializeField] private TMP_InputField playerNameInputField; // 플레이어 이름 입력 필드
    [SerializeField] private Button yesButton; // 확인 버튼
    [SerializeField] private Button noButton; // 취소 버튼
    [SerializeField] private Image nameSettingImg; // 이름 세팅 버튼 이미지
    [SerializeField] private Sprite noDefaultNameImg; // 기존 이름 없을 때 버튼 이미지
    [SerializeField] private Sprite yesDefaultNameImg; // 기존 이름 있을 때 버튼 이미지
    [SerializeField] private Button backButton; // 세이브 포인트 뒤로 버튼
    [SerializeField] private List<Button> saveBtns;

    // 생성된 C# 클래스 이름 (파일 이름과 동일)
    private PlayerInputActions playerInputActions;

    void Awake()
    {
        TogglePanel(GameManager.instance.CurrentPanelType);

        nameSettingButton.onClick.AddListener(() => TogglePanel(EPanelType.NameSetting));
        startButton.onClick.AddListener(() =>
        {
            SaveSystemManager.IsLoadGameMode = false;
            GameManager.instance.StartGame();
        });
        endingFrameButton.onClick.AddListener(GameManager.instance.EndingGallery);
        quitButton.onClick.AddListener(GameManager.instance.QuitGame);
        menuButton.onClick.AddListener(GameManager.instance.ToggleMenu);

        // 기존 이름 선택
        defaultNameButton.onClick.AddListener(() =>
        {
            string playerName = defaultNameText.text.Trim();
            if (string.IsNullOrEmpty(playerName)) return; // 기존 이름이 없으면 선택 불가
            SaveSystemManager.Instance.InitExistingPlayerSaveData(playerName);
            TogglePanel(EPanelType.GameMenu);
        });
        newNameButton.onClick.AddListener(() => TogglePanel(EPanelType.NewNameInput));

        // 새로운 이름 입력
        yesButton.onClick.AddListener(() =>
        {
            string inputName = playerNameInputField.text.Trim();
            if (string.IsNullOrEmpty(inputName))
            {
                Debug.LogWarning("플레이어 이름을 입력하지 않았습니다. 이름을 입력해주세요!");
                return;
            }

            GameManager.instance.SetPlayerName(inputName);
            SaveSystemManager.Instance.InitSaveData(inputName);
            TogglePanel(EPanelType.GameMenu);
        });
        noButton.onClick.AddListener(() => TogglePanel(EPanelType.NameSetting));
        savePointButton.onClick.AddListener(() => TogglePanel(EPanelType.SavePoint));
        backButton.onClick.AddListener(() => TogglePanel(EPanelType.GameMenu));

        playerInputActions = new PlayerInputActions(); // 메모리에 인풋 시스템 인스턴스 생성
    }

    void OnEnable()
    {
        // 타이틀에서 사용할 액션 맵 활성화
        playerInputActions.Player.Enable();
    }

    void OnDisable()
    {
        // 스크립트가 비활성화될 때 인풋도 비활성화
        playerInputActions.Player.Disable();
        playerInputActions.Dispose(); // 연결된 모든 리소스 해제(메모리 청소)
    }

    void Start()
    {
        // 타이틀 화면이 시작되면 커서를 보이게 설정
        if (GameManager.instance != null)
        {
            GameManager.instance.SetCursorVisible(true);
        }
    }

    void Update()
    {
        // 입력 감지 (예: 아무 키나 눌러서 시작 또는 특정 액션)
        if (playerInputActions.Player.ToggleMenu.WasPressedThisFrame())
        {
            GameManager.instance.ToggleMenu();
        }
    }

    /// <summary>
    /// 플레이어 이름 세팅 패널과 입력 패널, 버튼 패널을 토글하는 메서드
    /// </summary>
    /// <param name="panelType">토글할 패널의 유형</param>
    private void TogglePanel(EPanelType panelType)
    {
        GameManager.instance.CurrentPanelType = panelType;
        switch (panelType)
        {
            case EPanelType.GameMenu:
                buttonPanel.SetActive(true);
                playerNameSettingsPanel.SetActive(false);
                playerNameInputPanel.SetActive(false);
                savePointPanel.SetActive(false);
                break;
            case EPanelType.NameSetting:
                buttonPanel.SetActive(false);
                playerNameSettingsPanel.SetActive(true);
                playerNameInputPanel.SetActive(false);
                defaultNameText.text = GameManager.instance.PlayerName;
                if (defaultNameText.text == "")
                {
                    defaultText.text = "기존 이름 없음";
                    defaultText.color = new Color32(135, 133, 126, 255);
                    nameSettingImg.sprite = noDefaultNameImg;
                }
                else
                {
                    defaultText.text = "기존 이름 사용";
                    defaultText.color = new Color32(253, 245, 212, 255);
                    nameSettingImg.sprite = yesDefaultNameImg;
                }
                break;
            case EPanelType.NewNameInput:
                buttonPanel.SetActive(false);
                playerNameSettingsPanel.SetActive(false);
                playerNameInputPanel.SetActive(true);
                playerNameInputField.text = "";
                break;
            case EPanelType.SavePoint:
                buttonPanel.SetActive(false);
                playerNameSettingsPanel.SetActive(false);
                playerNameInputPanel.SetActive(false);
                savePointPanel.SetActive(true);
                UpdateSavePointUI();
                break;
        }
    }

    /// <summary>
    /// 세이브 데이터 UI 업데이트
    /// </summary>
    private void UpdateSavePointUI()
    {
        List<SavePointData> savePoints = SaveSystemManager.Instance.GetSavePoints();

        if (savePoints == null) savePoints = new List<SavePointData>();

        for (int i = 0; i < saveBtns.Count; i++)
        {
            if (i < savePoints.Count)
            {
                SavePointData data = savePoints[i];

                saveBtns[i].gameObject.SetActive(true);

                if (saveBtns[i].transform.childCount >= 2)
                {
                    Transform savePointName = saveBtns[i].transform.GetChild(0);
                    Transform playTime = saveBtns[i].transform.GetChild(1);
                    Transform saveDate = saveBtns[i].transform.GetChild(2);

                    TextMeshProUGUI nameText = savePointName.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI playTimeText = playTime.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI saveDateText = saveDate.GetComponent<TextMeshProUGUI>();

                    nameText.text = ToKoreanName(data.savePointType);
                    playTimeText.text = FormatTime(data.playTime);
                    saveDateText.text = data.saveDate;
                }

                // 버튼 클릭 이벤트 연결
                saveBtns[i].onClick.RemoveAllListeners();
                ESavePointType targetType = data.savePointType;

                // 버튼을 눌렀을 때 해당 기점 타입을 넘겨줌
                saveBtns[i].onClick.AddListener(() => OnSavePointButtonClicked(targetType));
            }
            else
            {
                saveBtns[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 세이브 기점 버튼 클릭 시 호출
    /// </summary>
    /// <param name="type"></param>
    private void OnSavePointButtonClicked(ESavePointType type)
    {
        Debug.Log($"{type} 기점으로 게임을 시작합니다.");

        SaveSystemManager.IsLoadGameMode = true;
        SaveSystemManager.Instance.QueryLoadFromTitle(type);
        GameManager.instance.StartGame();
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public string ToKoreanName(ESavePointType type)
    {
        return type switch
        {
            ESavePointType.CrewKeyPad => "선원실 탈출",
            ESavePointType.PowerRestoration => "전력 복구",
            ESavePointType.TorpedoLoaded => "어뢰관 장전",
            ESavePointType.TorpedoFirstLaunch => "어뢰 1차 발사 완료",
            ESavePointType.TorpedoSecondLaunch => "어뢰 2차 발사 완료",
            ESavePointType.TorpedoThirdLaunch => "어뢰 3차 발사 완료",
            ESavePointType.CureInjected => "치료제 투여",
            _ => "알 수 없는 기점"
        };
    }
}

