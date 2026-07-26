using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

public class Diary : MonoBehaviour
{
    [SerializeField] private GameObject statUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject interactorUI;
    [SerializeField] private GameObject diaryUI;
    [SerializeField] private GameObject heldItemObj;

    private InputAction _toggleMenuAction;
    private MenuUIController _menuUIController;
    private DiaryController _diaryController;

    private bool isOpen = false;

    private void Start()
    {
        _toggleMenuAction = PlayerManager.Instance.playerInput.actions["ToggleMenu"];

        _menuUIController = FindObjectOfType<MenuUIController>();
        _diaryController = GetComponent<DiaryController>();
    }

    /// <summary>
    /// 다이어리 열기
    /// </summary>
    public void ViewDiary()
    {
        if (isOpen) return;
        isOpen = true;

        statUI.SetActive(false);
        inventoryUI.SetActive(false);
        interactorUI.SetActive(false);
        diaryUI.SetActive(true);
        _diaryController.UpdatePageUI();

        // 손에 들고 있는 일기장 비활성화
        heldItemObj.SetActive(false);

        // ESC키 입력 액션 변경
        // ESC 메뉴 활성화 -> 다이어리 닫기
        _toggleMenuAction.performed += OnToggleDiaryInput;

        // 포커스 상태 변경 - Diary 상태
        FocusManager.Instance.PushFocusState(GameFocusState.Diary);
    }

    /// <summary>
    /// 다이어리 닫기
    /// </summary>
    public void CloseDiary()
    {
        if (!isOpen) return;
        isOpen = false;

        statUI.SetActive(true);
        inventoryUI.SetActive(true);
        interactorUI.SetActive(true);
        diaryUI.SetActive(false);

        heldItemObj.SetActive(true);

        // ESC키 입력 액션 복원
        // 다이어리 닫기 -> ESC 메뉴 활성화
        _toggleMenuAction.performed -= OnToggleDiaryInput;

        // 이전 포커스 상태로 복구
        FocusManager.Instance.PopFocusState();

        _toggleMenuAction.performed += _menuUIController.OnToggleMenu;
    }

    private void OnToggleDiaryInput(InputAction.CallbackContext context)
    {
        CloseDiary();
    }

    private void OnDisable()
    {
        _toggleMenuAction.performed -= OnToggleDiaryInput;
    }
}
