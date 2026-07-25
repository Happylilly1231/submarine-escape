using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DiaryEntry
{
    public string date; // 날짜
    [TextArea(3, 10)] public string content; // 내용
}

public class DiaryController : MonoBehaviour
{
    [Header("UI 패널")]
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;

    [Header("버튼")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("일기")]
    [SerializeField] private List<DiaryEntry> diaryEntries = new List<DiaryEntry>();

    private TextMeshProUGUI _leftDateText;
    private TextMeshProUGUI _leftContentText;
    private TextMeshProUGUI _rightDateText;
    private TextMeshProUGUI _rightContentText;

    private int _currentPageIdx = 0;

    private void Awake()
    {
        TextMeshProUGUI[] leftTMPs = leftPanel.GetComponentsInChildren<TextMeshProUGUI>();
        _leftDateText = leftTMPs[0];
        _leftContentText = leftTMPs[1];

        TextMeshProUGUI[] rightTMPs = rightPanel.GetComponentsInChildren<TextMeshProUGUI>();
        _rightDateText = rightTMPs[0];
        _rightContentText = rightTMPs[1];

        prevButton.onClick.AddListener(() => TurnPage(-1));
        nextButton.onClick.AddListener(() => TurnPage(1));
    }

    private void Start()
    {
        InitDiaryContent();
    }

    /// <summary>
    /// 일기장 UI 초기화
    /// </summary>
    private void InitDiaryContent()
    {
        _leftDateText.text = diaryEntries[0].date;
        _leftContentText.text = diaryEntries[0].content;

        _rightDateText.text = diaryEntries[1].date;
        _rightContentText.text = diaryEntries[1].content;

        prevButton.interactable = false;
    }

    /// <summary>
    /// 페이지 넘기기
    /// </summary>
    /// <param name="direction"></param>
    private void TurnPage(int direction)
    {
        if (direction > 0 && _currentPageIdx < diaryEntries.Count / 2 - 1) // 다음 페이지 이동
        {
            _currentPageIdx++;
        }
        else if (direction < 0 && _currentPageIdx > 0) // 이전 페이지 이동
        {
            _currentPageIdx--;
        }
        UpdatePageUI();
    }

    /// <summary>
    /// 페이지 UI 업데이트
    /// </summary>
    private void UpdatePageUI()
    {
        int leftIdx = _currentPageIdx * 2;
        int rightIdx = leftIdx + 1;

        _leftDateText.text = diaryEntries[leftIdx].date;
        _leftContentText.text = diaryEntries[leftIdx].content;

        _rightDateText.text = diaryEntries[rightIdx].date;
        _rightContentText.text = diaryEntries[rightIdx].content;

        prevButton.interactable = (_currentPageIdx > 0);
        nextButton.interactable = (_currentPageIdx < diaryEntries.Count / 2 - 1);
    }
}
