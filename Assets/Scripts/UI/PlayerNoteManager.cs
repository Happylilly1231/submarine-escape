using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// 단서
/// </summary>
[System.Serializable]
public class Clue
{
    public string clueId;
    public Button clueImgButton;
    public GameObject clueObj;
    public LocalizedSprite clueLocalizedSprite;
}

[System.Serializable]
public struct PlayerNoteData
{
    [System.ComponentModel.Description("1~2페이지: 단서 활성화 여부")]
    public bool[] clueActiveStates;

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
    [SerializeField] private Toggle[] checkboxes;
    [SerializeField] private GameObject[] creatureSampleMemoGroups;
    [SerializeField] private GameObject databaseMonitorHintText;
    [SerializeField] private TMP_InputField[] memoPageInputFields;
    [SerializeField] private Image bigClueImage;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private List<Clue> clueList = new List<Clue>();
    private Dictionary<string, Button> clueDict = new Dictionary<string, Button>();

    private GameObject[] _leftPages;
    private GameObject[] _rightPages;

    private TextMeshProUGUI _leftPageNumText;
    private TextMeshProUGUI _rightPageNumText;

    private int _currentSpreadIdx = 0; // 열린 페이지(2개) 인덱스
    private int _totalPageCount; // 전체 페이지 수

    private PlayerNoteData currentPlayerNoteData;

    private DatabaseMonitorController _databaseMonitorController;

    private LocalizedSprite _currentLocalizedSprite;

    public static PlayerNoteManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        _databaseMonitorController = FindAnyObjectByType<DatabaseMonitorController>();

        // 단서 딕셔너리로 변환
        clueDict = clueList.ToDictionary(clue => clue.clueId, clue => clue.clueImgButton);

        // 왼쪽 페이지
        _leftPages = new GameObject[leftPage.childCount - 1];
        for (int i = 0; i < leftPage.childCount - 1; i++)
        {
            _leftPages[i] = leftPage.GetChild(i).gameObject;
        }
        _leftPageNumText = leftPage.GetChild(leftPage.childCount - 1).GetComponent<TextMeshProUGUI>();

        // 오른쪽 페이지
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

        // UI 페이지 넘버 갱신 -> (현재 페이지 넘버) / (마지막 페이지 넘버)
        _leftPageNumText.text = $"{_currentSpreadIdx * 2 + 1} / {_totalPageCount}";
        _rightPageNumText.text = $"{_currentSpreadIdx * 2 + 2} / {_totalPageCount}";

        // 토글 값이 변경될 때 실행될 메서드 등록
        for (int i = 0; i < checkboxes.Length; i++)
        {
            int idx = i;
            checkboxes[i].onValueChanged.AddListener((isOn) =>
            {
                OnToggleValueChanged(idx, isOn);
            });
        }

        // 단서 누르면 줌인 함수 연결
        foreach (var clue in clueList)
        {
            if (clue.clueImgButton != null)
                clue.clueImgButton.onClick.AddListener(() => ZoomInClue(clue));
        }

        // 단서 줌아웃 함수 연결
        zoomOutButton.onClick.AddListener(ZoomOutClue);

        // 새 노트로 초기화
        InitNewNote();
    }

    /// <summary>
    /// 단서 등록 함수
    /// </summary>
    /// <param name="clueId"></param>
    public void RegisterClue(string clueId)
    {
        // 현재 단서 활성화 여부 현재 데이터에 저장
        for (int i = 0; i < clueList.Count; i++)
        {
            if (clueId == clueList[i].clueId)
            {
                currentPlayerNoteData.clueActiveStates[i] = true;
                if (clueList[i].clueImgButton != null)
                    clueList[i].clueImgButton.gameObject.SetActive(true);
                if (clueList[i].clueObj != null)
                    clueList[i].clueObj.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 단서 확대
    /// </summary>
    /// <param name="clueSprite">단서 이미지</param>
    public void ZoomInClue(Clue clue)
    {
        _currentLocalizedSprite = clue.clueLocalizedSprite;

        if (_currentLocalizedSprite != null)
        {
            // 언어 변경 감지를 위한 이벤트 구독
            _currentLocalizedSprite.AssetChanged += OnSpriteChanged;

            // 창을 열었을 때 바로 현재 언어에 맞는 이미지 적용하기
            var handle = _currentLocalizedSprite.LoadAssetAsync();
            if (handle.IsDone && handle.Result != null)
            {
                // 이미 로드되어 있다면 즉시 할당
                bigClueImage.sprite = handle.Result;
            }
            else
            {
                // 비동기 로드 완료 시 할당
                handle.Completed += (op) =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        bigClueImage.sprite = op.Result;
                    }
                };
            }
        }

        bigClueImage.transform.parent.gameObject.SetActive(true);
    }

    private void OnSpriteChanged(Sprite newSprite)
    {
        bigClueImage.sprite = newSprite;
    }

    /// <summary>
    /// 단서 확대 종료 (단서 확대 닫기 버튼에 연결)
    /// </summary>
    public void ZoomOutClue()
    {
        bigClueImage.transform.parent.gameObject.SetActive(false);

        if (_currentLocalizedSprite != null)
        {
            _currentLocalizedSprite.AssetChanged -= OnSpriteChanged;
            _currentLocalizedSprite = null;
        }
    }

    private void OnDisable()
    {
        if (_currentLocalizedSprite != null)
        {
            _currentLocalizedSprite.AssetChanged -= OnSpriteChanged;
        }
    }

    /// <summary>
    /// 토글이 켜지거나 꺼질 때 실행되는 함수
    /// </summary>
    /// <param name="i"></param>
    /// <param name="isOn"></param>
    public void OnToggleValueChanged(int i, bool isOn)
    {
        currentPlayerNoteData.checkStates[i] = isOn;
    }

    /// <summary>
    /// 새 노트로 초기화
    /// </summary>
    private void InitNewNote()
    {
        currentPlayerNoteData.clueActiveStates = new bool[clueList.Count];
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
        for (int i = 0; i < clueList.Count; i++)
        {
            if (clueList[i].clueImgButton != null)
                clueList[i].clueImgButton.gameObject.SetActive(currentPlayerNoteData.clueActiveStates[i]);
            if (clueList[i].clueObj != null)
                clueList[i].clueObj.SetActive(currentPlayerNoteData.clueActiveStates[i]);
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

        if (spreadIdx == 2) // 샘플 페이지면
        {
            // 데이터베이스 모니터를 한번이라도 로그인했으면 -> 샘플 페이지 보여줌 
            foreach (var memo in creatureSampleMemoGroups)
            {
                memo.SetActive(_databaseMonitorController.HasLogined);
            }

            // 데이터베이스 모니터 힌트 텍스트도 한번이라도 로그인 안했을 때만 보여줌
            databaseMonitorHintText.SetActive(!_databaseMonitorController.HasLogined);
        }

        // 현재 페이지들로 설정
        _currentSpreadIdx = spreadIdx;

        // UI 페이지 넘버 갱신 -> (현재 페이지 넘버) / (마지막 페이지 넘버)
        _leftPageNumText.text = $"{_currentSpreadIdx * 2 + 1} / {_totalPageCount}";
        _rightPageNumText.text = $"{_currentSpreadIdx * 2 + 2} / {_totalPageCount}";
    }
}
