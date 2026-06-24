using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField] private GameObject inGameMenuUI; // 전체 탭 메뉴 UI
    [SerializeField] private GameObject tabPanelsRoot; // 탭 패널 루트
    [SerializeField] private GameObject tabButtonsRoot;

    private InputAction _toggleInGameMenuAction;

    private GameObject[] _tabPanels; // 탭 패널 배열 (0: 지도, 1: 목표, 2: 메모)
    private GameObject[] _tabButtons;

    public bool IsOpen { get; private set; } = false;
    private int _currentIndex = 0; // 기본값은 이전에 열었던 탭 (초기값은 지도)

    private Color _originalColor = new Color(1f, 1f, 1f, 180f / 255f);
    private Color _highLightColor = new Color(1f, 210f / 255f, 80f / 255f, 180f / 255f);

    private MapViewController _mapViewController;

    private void Awake()
    {
        _tabButtons = new GameObject[tabButtonsRoot.transform.childCount];
        for (int i = 0; i < tabButtonsRoot.transform.childCount; i++)
        {
            _tabButtons[i] = tabButtonsRoot.transform.GetChild(i).gameObject;
            int idx = i;
            _tabButtons[i].GetComponent<Button>().onClick.AddListener(() => OpenTab(idx));
        }

        _tabPanels = new GameObject[tabPanelsRoot.transform.childCount];
        for (int i = 0; i < tabPanelsRoot.transform.childCount; i++)
        {
            _tabPanels[i] = tabPanelsRoot.transform.GetChild(i).gameObject;
        }

        _mapViewController = GetComponent<MapViewController>();
    }

    private void Start()
    {
        _toggleInGameMenuAction = SubmarineInGameManager.instance.playerInput.actions["ToggleInGameMenu"];

        _toggleInGameMenuAction.performed += OnToggleInGameMenu;

        inGameMenuUI.SetActive(false);

        foreach (var tabPanel in _tabPanels)
        {
            tabPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        _toggleInGameMenuAction.performed -= OnToggleInGameMenu;
    }

    /// <summary>
    /// Tab키 입력으로 인게임 메뉴 켜기/끄기
    /// </summary>
    /// <param name="context">입력</param>
    public void OnToggleInGameMenu(InputAction.CallbackContext context)
    {
        IsOpen = !IsOpen;
        inGameMenuUI.SetActive(IsOpen);

        if (IsOpen)
        {
            FocusManager.Instance.PushFocusState(GameFocusState.InGameMenu);

            // 이전에 열려있던 탭을 활성화
            OpenTab(_currentIndex);
        }
        else
        {
            FocusManager.Instance.PopFocusState();
        }
    }

    /// <summary>
    /// 인게임 메뉴 닫기
    /// </summary>
    public void CloseInGameMenu()
    {
        if (IsOpen)
        {
            inGameMenuUI.SetActive(false);
            IsOpen = false;
        }
    }

    /// <summary>
    /// 탭 열기
    /// </summary>
    /// <param name="index">탭 인덱스</param>
    public void OpenTab(int index)
    {
        _tabPanels[_currentIndex].SetActive(false);
        _tabButtons[_currentIndex].transform.GetChild(1).GetComponent<Image>().color = _originalColor;
        _tabButtons[_currentIndex].transform.GetChild(2).GetComponent<Image>().color = _originalColor;

        _tabPanels[index].SetActive(true);
        _currentIndex = index;
        _tabButtons[_currentIndex].transform.GetChild(1).GetComponent<Image>().color = _highLightColor;
        _tabButtons[_currentIndex].transform.GetChild(2).GetComponent<Image>().color = _highLightColor;

        switch (index)
        {
            case 0: // 맵
                _mapViewController.UpdateMapUI(); // 현재 위치로 맵 UI 갱신
                break;
        }
    }
}
