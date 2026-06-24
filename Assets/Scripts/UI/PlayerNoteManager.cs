using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct PlayerNoteData
{
    [System.ComponentModel.Description("1~2페이지: 기억 이미지 활성화 여부")]
    public bool[] memoryImageStates;

    [System.ComponentModel.Description("3~4페이지: 데이터베이스 샘플 등 체크박스 상태")]
    public bool[] checkStates;

    [System.ComponentModel.Description("5~8페이지: 자유 타이핑 메모 텍스트 데이터")]
    public string[] memoPageTexts;
}

public class PlayerNoteManager : MonoBehaviour
{
    [SerializeField] private Transform leftPage;
    [SerializeField] private Transform rightPage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject[] memoryImgObjs;
    [SerializeField] private Toggle[] checkboxes;
    [SerializeField] private TMP_InputField[] memoPageInputFields;

    private GameObject[] _leftPages;
    private GameObject[] _rightPages;

    private TextMeshProUGUI _leftPageNumText;
    private TextMeshProUGUI _rightPageNumText;

    private int _currentSpreadIdx = 0; // 열린 페이지(2개) 인덱스
    private int _totalPageCount; // 전체 페이지 수

    private PlayerNoteData currentPlayerNoteData;

    public static PlayerNoteManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        _leftPages = new GameObject[leftPage.childCount - 1];
        for (int i = 0; i < leftPage.childCount - 1; i++)
        {
            _leftPages[i] = leftPage.GetChild(i).gameObject;
        }
        _leftPageNumText = leftPage.GetChild(leftPage.childCount - 1).GetComponent<TextMeshProUGUI>();

        _rightPages = new GameObject[rightPage.childCount - 1];
        for (int i = 0; i < rightPage.childCount - 1; i++)
        {
            _rightPages[i] = rightPage.GetChild(i).gameObject;
        }
        _rightPageNumText = rightPage.GetChild(rightPage.childCount - 1).GetComponent<TextMeshProUGUI>();

        _totalPageCount = _leftPages.Length + _rightPages.Length;
    }

    private void Start()
    {
        prevButton.onClick.AddListener(() => TurnPage(-1));
        nextButton.onClick.AddListener(() => TurnPage(1));

        // 첫번째 페이지들 활성화
        _leftPages[0].SetActive(true);
        _rightPages[0].SetActive(true);

        // 나머지 페이지들은 비활성화
        for (int i = 1; i < _leftPages.Length; i++)
        {
            _leftPages[i].SetActive(false);
            _rightPages[i].SetActive(false);
        }

        // 새 노트로 초기화
        InitNewNote();
    }

    /// <summary>
    /// 새 노트로 초기화
    /// </summary>
    private void InitNewNote()
    {
        currentPlayerNoteData.memoryImageStates = new bool[memoryImgObjs.Length];
        currentPlayerNoteData.checkStates = new bool[checkboxes.Length];
        currentPlayerNoteData.memoPageTexts = new string[memoPageInputFields.Length];

        for (int i = 0; i < currentPlayerNoteData.memoPageTexts.Length; i++)
        {
            currentPlayerNoteData.memoPageTexts[i] = ""; // 빈 칸으로 초기화
        }

        UpdateNoteUI();
    }

    /// <summary>
    /// 플레이어 노트 세이브할 때 호출
    /// </summary>
    /// <returns>저장할 데이터</returns>
    public PlayerNoteData GetCurrentPlayerNoteData()
    {
        return currentPlayerNoteData;
    }

    /// <summary>
    /// 플레이어 노트 세이브 데이터 로드
    /// </summary>
    /// <param name="loadedData">로드할 데이터</param>
    public void LoadPlayerNoteData(PlayerNoteData loadedData)
    {
        currentPlayerNoteData = loadedData;

        UpdateNoteUI();
    }

    /// <summary>
    /// 노트 UI 업데이트
    /// </summary>
    public void UpdateNoteUI()
    {
        for (int i = 0; i < memoryImgObjs.Length; i++)
        {
            memoryImgObjs[i].SetActive(currentPlayerNoteData.memoryImageStates[i]);
        }

        for (int i = 0; i < checkboxes.Length; i++)
        {
            checkboxes[i].isOn = currentPlayerNoteData.checkStates[i];
        }

        for (int i = 0; i < memoPageInputFields.Length; i++)
        {
            memoPageInputFields[i].text = currentPlayerNoteData.memoPageTexts[i];
        }
    }

    /// <summary>
    /// 페이지 넘기기
    /// </summary>
    /// <param name="direction">방향 - 이전: -1 / 다음: 1</param>
    private void TurnPage(int direction)
    {
        int spreadIdx = _currentSpreadIdx;
        if (direction > 0 && _currentSpreadIdx < _leftPages.Length - 1) // 다음 페이지 이동
        {
            spreadIdx++;
        }
        else if (direction < 0 && _currentSpreadIdx > 0) // 이전 페이지 이동
        {
            spreadIdx--;
        }
        else
            return;

        OpenPages(spreadIdx);

        // UI 페이지 넘버 갱신 -> (현재 페이지 넘버) / (마지막 페이지 넘버)
        _leftPageNumText.text = $"{_currentSpreadIdx * 2 + 1} / {_totalPageCount}";
        _rightPageNumText.text = $"{_currentSpreadIdx * 2 + 2} / {_totalPageCount}";
    }

    /// <summary>
    /// 페이지들 열기
    /// </summary>
    /// <param name="spreadIdx">열린 페이지(2개) 인덱스</param>
    private void OpenPages(int spreadIdx)
    {
        if (_currentSpreadIdx == spreadIdx) return;

        // 현재 페이지들 비활성화
        _leftPages[_currentSpreadIdx].SetActive(false);
        _rightPages[_currentSpreadIdx].SetActive(false);

        // 펼쳐야 하는 페이지들 활성화
        _leftPages[spreadIdx].SetActive(true);
        _rightPages[spreadIdx].SetActive(true);

        // 펼쳐야 하는 페이지들 내용 로드 필요

        // 현재 페이지들로 설정
        _currentSpreadIdx = spreadIdx;
    }

    // // 글자가 타이핑될 때마다 실행되는 함수
    // private void OnMemoTextChanged(string currentText)
    // {
    //     // 현재 보고 있는 페이지 방에 실시간으로 글자를 덮어씌움
    //     NotePages[currentMemoIdx] = currentText;
    // }

    // /// <summary>
    // /// 메모 포커스 여부 설정
    // /// </summary>
    // /// <param name="isFocus">포커스 여부</param>
    // public void SetFocusMemo(bool isFocus)
    // {
    //     if (isFocus)
    //     {
    //         memoInputField.ActivateInputField();
    //         memoInputField.MoveTextEnd(false); // 텍스트 맨 뒤로 커서 이동
    //         bool isToggleMenuEnabled = SubmarineInGameManager.instance.playerInput.actions["ToggleMenu"].enabled;
    //         InputManager.instance.SaveAndDisableAllInputs(); // 인풋 비활성화(현재 여부들 저장)
    //         SubmarineInGameManager.instance.playerInput.actions["TogglInGameMenu"].Enable();
    //         if (isToggleMenuEnabled)
    //             SubmarineInGameManager.instance.playerInput.actions["ToggleMenu"].Enable();
    //     }
    //     else
    //     {
    //         memoInputField.DeactivateInputField();
    //         InputManager.instance.RestoreInputsFromSnapshot(); // 인풋 활성화 여부 복구
    //     }
    // }

    // // 배열에 있던 텍스트를 불러와서 화면에 뿌려주는 함수
    // public void UpdateMemoUI(float direction)
    // {
    //     // 페이지 인덱스 계산 (0~4 범위 제한)
    //     if (direction > 0 && currentMemoIdx < NotePages.Length - 1) // 위 -> 다음 페이지
    //     {
    //         currentMemoIdx++;
    //     }
    //     else if (direction < 0 && currentMemoIdx > 0) // 아래 -> 이전 페이지
    //     {
    //         currentMemoIdx--;
    //     }
    //     else
    //         return;

    //     // 해당 페이지 저장된 글 불러오기
    //     memoInputField.text = NotePages[currentMemoIdx];

    //     // UI 페이지 넘버 갱신 -> (현재 페이지 넘버) / (마지막 페이지 넘버)
    //     pageNumberUIText.text = $"{currentMemoIdx + 1} / {NotePages.Length}";

    //     // 페이지가 바뀌어도 바로 타이핑할 수 있게 포커스 유지
    //     SetFocusMemo(true);
    // }

    // private void OnDestroy()
    // {
    //     // 오브젝트가 파괴될 때 리스너도 깔끔하게 제거
    //     memoInputField.onValueChanged.RemoveListener(OnMemoTextChanged);
    // }
}
