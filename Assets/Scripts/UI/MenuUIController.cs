using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject menuUI;
    public GameObject MenuUI => menuUI;

    [SerializeField] private GameObject tabButtonRoot;
    [SerializeField] private GameObject tabPanelRoot;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_Dropdown languageDropdown; // 언어 드롭다운
    [SerializeField] private Button setDifficultyEasyButton;
    private TextMeshProUGUI setDifficultyEasyButtonText;
    [SerializeField] private Button setDifficultyHardButton;
    private TextMeshProUGUI setDifficultyHardButtonText;

    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Button quitButton;
    // [SerializeField] private Button exitButton;

    private int _currentIndex = -1;
    private GameObject[] tabButtons;
    private GameObject[] tabPanels;
    private Color _originalColor = new Color(227f / 255f, 231f / 255f, 232f / 255f, 1f);
    private Color _highLightColor = new Color(1f, 0f, 33f / 255f, 200f / 255f);
    // private Color _originalButtonColor = new Color(1f, 0f, 33f / 255f, 200f / 255f);
    // private Color _selectedButtonColor = new Color(1f, 0f, 33f / 255f, 200f / 255f);
    private DebuggingUIManager _debuggingUIManager;

    public static MenuUIController instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        _debuggingUIManager = GetComponent<DebuggingUIManager>();

        tabButtons = new GameObject[tabButtonRoot.transform.childCount];
        for (int i = 0; i < tabButtonRoot.transform.childCount; i++)
        {
            tabButtons[i] = tabButtonRoot.transform.GetChild(i).gameObject;
            int idx = i;
            tabButtons[i].GetComponent<Button>().onClick.AddListener(() => OpenTab(idx));
        }

        tabPanels = new GameObject[tabPanelRoot.transform.childCount];
        for (int i = 0; i < tabPanelRoot.transform.childCount; i++)
        {
            tabPanels[i] = tabPanelRoot.transform.GetChild(i).gameObject;
        }

        // 디버깅 UI 비활성화
        SetActiveDebuggingUI(false);
    }

    private void Start()
    {
        // 현재 설정되어있는 언어 인덱스로 드롭다운 UI 값 초기화
        LanguageManager.Instance.GetCurrentLanguageIndex((index) =>
        {
            languageDropdown.SetValueWithoutNotify(index);
        });
        // 언어 드롭다운 값 변경 시 실행될 이벤트 연결
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        // 난이도 버튼 텍스트 가져오기
        setDifficultyEasyButtonText = setDifficultyEasyButton.GetComponentInChildren<TextMeshProUGUI>();
        setDifficultyHardButtonText = setDifficultyHardButton.GetComponentInChildren<TextMeshProUGUI>();

        // 슬라이더 수치 변경 이벤트 함수 연결
        masterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        mouseSensitivitySlider.onValueChanged.AddListener(GameManager.instance.SetMouseSensitivity);

        // 슬라이더 수치대로 설정 초기화
        AudioManager.Instance.SetMasterVolume(masterVolumeSlider.value);
        AudioManager.Instance.SetBGMVolume(bgmSlider.value);
        AudioManager.Instance.SetSFXVolume(sfxSlider.value);
        GameManager.instance.SetMouseSensitivity(mouseSensitivitySlider.value);

        // 버튼 함수 연결
        returnToTitleButton.onClick.AddListener(GameManager.instance.ReturnToTitle);
        quitButton.onClick.AddListener(GameManager.instance.QuitGame);
        // exitButton.onClick.AddListener(GameManager.instance.ExitMenu);
        setDifficultyEasyButton.onClick.AddListener(() => OnClickSetDifficultyButton(setDifficultyEasyButtonText, Difficulty.Easy));
        setDifficultyHardButton.onClick.AddListener(() => OnClickSetDifficultyButton(setDifficultyHardButtonText, Difficulty.Hard));

        OnClickSetDifficultyButton(setDifficultyEasyButtonText, Difficulty.Easy);
    }

    /// <summary>
    /// Escape키 입력에 따라 메뉴 열기/열기 해제
    /// </summary>
    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        // 정지 버튼(ESC) 눌렀을 때
        if (context.performed)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        menuUI.SetActive(!menuUI.activeSelf);
        if (menuUI.activeSelf)
        {
            GameManager.instance.Pause(); // 정지
            OpenTab(0);
            if (SubmarineInGameManager.instance != null) // 인게임 중이면
            {
                PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(false); // 상호작용 UI 끄기
            }
        }
        else
        {
            GameManager.instance.Resume(); // 정지 해제
            if (SubmarineInGameManager.instance != null) // 인게임 중이면
            {
                PlayerManager.Instance.playerInteractor.SetActiveInteractorUI(true); // 상호작용 UI 켜기
            }
        }
    }

    /// <summary>
    /// 언어 변경 함수(0: 영어 / 1: 한국어)
    /// </summary>
    public void OnLanguageChanged(int index)
    {
        LanguageManager.Instance.ChangeLanguage(index);
    }

    private void OnClickSetDifficultyButton(TextMeshProUGUI buttonText, Difficulty difficulty)
    {
        GameManager.instance.SetDifficulty(difficulty);
        buttonText.color = _highLightColor;
        if (buttonText == setDifficultyEasyButtonText)
            setDifficultyHardButtonText.color = _originalColor;
        else
            setDifficultyEasyButtonText.color = _originalColor;
    }

    /// <summary>
    /// 디버깅 UI 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetActiveDebuggingUI(bool isActive)
    {
        tabButtons[tabButtons.Length - 1].SetActive(isActive);
        if (isActive)
        {
            // 마지막 탭 열기
            OpenTab(tabPanels.Length - 1);
        }
        else
        {
            // 첫 탭 열기 
            OpenTab(0);
        }

        _debuggingUIManager.SetActiveDebugging(isActive);
    }

    /// <summary>
    /// 디버깅 UI 토글(켜기/끄기)
    /// </summary>
    public void ToggleDebuggingUI()
    {
        bool isActive = !tabButtons[tabButtons.Length - 1].activeSelf;
        SetActiveDebuggingUI(isActive);
    }

    /// <summary>
    /// 탭 열기
    /// </summary>
    /// <param name="index">탭 인덱스</param>
    public void OpenTab(int index)
    {
        if (_currentIndex == index) return;

        if (_currentIndex >= 0)
        {
            tabPanels[_currentIndex].SetActive(false);
            tabButtons[_currentIndex].transform.GetChild(0).GetComponent<Image>().color = _originalColor;
            tabButtons[_currentIndex].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = _originalColor;
        }

        Debug.Log(index + " / " + tabPanels.Length);
        tabPanels[index].SetActive(true);
        _currentIndex = index;
        tabButtons[_currentIndex].transform.GetChild(0).GetComponent<Image>().color = _highLightColor;
        tabButtons[_currentIndex].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = _highLightColor;
    }
}
