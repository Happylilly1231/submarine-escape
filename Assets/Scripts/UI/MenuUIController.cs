using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject tabButtonRoot;
    [SerializeField] private GameObject tabPanelRoot;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button exitButton;

    private int _currentIndex = -1;
    private GameObject[] tabButtons;
    private GameObject[] tabPanels;
    private Color _originalColor = new Color(227f / 255f, 231f / 255f, 232f / 255f, 1f);
    private Color _highLightColor = new Color(1f, 0f, 33f / 255f, 200f / 255f);

    public static MenuUIController instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

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
        exitButton.onClick.AddListener(GameManager.instance.ExitMenu);
    }

    /// <summary>
    /// 디버깅 UI 활성화 여부 설정
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveDebuggingUI(bool isActive)
    {
        tabButtons[tabButtons.Length - 1].SetActive(isActive);
    }

    public void OpenTab(int index)
    {
        if (_currentIndex == index) return;

        if (_currentIndex >= 0)
        {
            tabPanels[_currentIndex].SetActive(false);
            tabButtons[_currentIndex].transform.GetChild(0).GetComponent<Image>().color = _originalColor;
            tabButtons[_currentIndex].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = _originalColor;
        }

        tabPanels[index].SetActive(true);
        _currentIndex = index;
        tabButtons[_currentIndex].transform.GetChild(0).GetComponent<Image>().color = _highLightColor;
        tabButtons[_currentIndex].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = _highLightColor;
    }
}
