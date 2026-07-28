using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class NotebookController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    [Header("게임 UI")]
    [SerializeField] private GameObject statUI;
    [SerializeField] private GameObject interactorUI;
    [SerializeField] private GameObject inventoryUI;

    [Header("노트북 ui")]
    [SerializeField] private GameObject notebookDisplay;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TextMeshProUGUI[] passwordDisplay;
    [SerializeField] private GameObject failText;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI[] contentText;

    private static string corretPw = "ABYSS";
    private Coroutine checkPasswordCoroutine;

    public override void Start()
    {
        base.Start();

        InitializeInput();

        loginPanel.SetActive(true);
        mainPanel.SetActive(false);
    }

    #region 퍼즐 시작/종료
    public override void ActivatePuzzle()
    {
        statUI.SetActive(false);
        interactorUI.SetActive(false);
        inventoryUI.SetActive(false);

        if (mainPanel.activeSelf) UpdateMainPanelContent();
        notebookDisplay.SetActive(true);

        inventoryManager.CloseInventory();
        itemEquipController.UnequipItem();

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        if (loginPanel.activeSelf)
        {
            // 이벤트 리스너 등록
            passwordInputField.onValueChanged.AddListener(OnInputChanged);
            passwordInputField.onEndEdit.AddListener(OnEndEdit); // 엔터키 입력 시 포커스 유지용

            // 바로 입력 가능하게 InputField 포커스
            passwordInputField.interactable = true;
            passwordInputField.ActivateInputField();
        }
    }

    public override void ExitPuzzle()
    {
        statUI.SetActive(true);
        interactorUI.SetActive(true);
        inventoryUI.SetActive(true);

        inventoryManager.OpenInventory();

        if (checkPasswordCoroutine != null)
        {
            StopCoroutine(checkPasswordCoroutine);
            checkPasswordCoroutine = null;
        }

        base.ExitPuzzle();

        notebookDisplay.SetActive(false);
        ResetInput();
    }

    protected override void UnsubscribeEvents()
    {
        base.UnsubscribeEvents();

        // 이벤트 리스너 해제
        passwordInputField.onValueChanged.RemoveAllListeners();
        passwordInputField.onEndEdit.RemoveAllListeners();
    }
    #endregion

    private void InitializeInput()
    {
        passwordInputField.characterLimit = 5; // 5글자 제한
        passwordInputField.contentType = TMP_InputField.ContentType.Alphanumeric;

        ResetDisplayTexts();
    }

    /// <summary>
    /// 입력 내용이 바뀔 때 호출 (입력 중 대문자 표시)
    /// </summary>
    private void OnInputChanged(string currentInput)
    {
        ResetDisplayTexts();

        for (int i = 0; i < currentInput.Length; i++)
        {
            if (i < passwordDisplay.Length)
            {
                passwordDisplay[i].text = currentInput[i].ToString().ToUpper();
            }
        }

        if (currentInput.Length == 5)
        {
            if (checkPasswordCoroutine != null)
            {
                StopCoroutine(checkPasswordCoroutine);
            }
            checkPasswordCoroutine = StartCoroutine(CheckPasswordWithDelay(currentInput));
        }
    }

    /// <summary>
    /// 엔터 입력 등으로 포커스가 해제되는 것을 방지
    /// </summary>
    private void OnEndEdit(string text)
    {
        if (notebookDisplay.activeSelf && passwordInputField.interactable)
        {
            ResetDisplayTexts();
            passwordInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// 약간의 텀(딜레이)을 주고 비밀번호 검사
    /// </summary>
    private IEnumerator CheckPasswordWithDelay(string input)
    {
        // 검사 대기 중 중복 입력 방지
        passwordInputField.interactable = false;

        yield return new WaitForSeconds(0.3f);

        if (input.ToUpper() == corretPw)
        {
            Debug.Log($"비밀번호 맞음: {input.ToUpper()}");
            UpdateMainPanelContent();

            failText.SetActive(false);
            loginPanel.SetActive(false);
            mainPanel.SetActive(true);
            PlayerNoteManager.instance.RegisterClue("Project_Abyss");
        }
        else
        {
            Debug.Log($"비밀번호 틀림: {input.ToUpper()}");
            passwordInputField.interactable = true;
            passwordInputField.text = "";
            ResetDisplayTexts();

            failText.SetActive(true);
            passwordInputField.ActivateInputField();
        }

        checkPasswordCoroutine = null;
    }

    /// <summary>
    /// 모든 표시 텍스트를 초기화
    /// </summary>
    private void ResetDisplayTexts()
    {
        foreach (var txt in passwordDisplay)
        {
            txt.text = "";
        }
        failText.SetActive(false);
    }

    /// <summary>
    /// 퍼즐 종료 시 리셋
    /// </summary>
    public void ResetInput()
    {
        passwordInputField.text = "";
        ResetDisplayTexts();
    }

    /// <summary>
    /// Notebook/Content{idx} 키를 기반으로 다국어 텍스트 적용
    /// </summary>
    private void UpdateMainPanelContent()
    {
        for (int i = 0; i < contentText.Length; i++)
        {
            if (contentText[i] == null) continue;

            string tableKey = $"Notebook/Content{i + 1}";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            contentText[i].text = localized;
        }
    }
}