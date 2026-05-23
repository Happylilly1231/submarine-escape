using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndingDetailView : MonoBehaviour
{
    [SerializeField] private Button closeBtn;
    [Header("UI Components")]
    [SerializeField] private GameObject detailPanel; // 엔딩 확대 패널
    [SerializeField] private Image endingIamge; // 엔딩 이미지
    [SerializeField] private TextMeshProUGUI description; // 엔딩 설명
    [SerializeField] private TextMeshProUGUI firstTimeText; // 최초 기록
    [SerializeField] private TextMeshProUGUI bestTimeText;  // 최단 기록

    void Awake()
    {
        CloseDetail();
        closeBtn.onClick.AddListener(CloseDetail);
    }

    public void ShowDetail(Sprite image, string info, float firstTime, float bestTime, string beatPlayerName)
    {
        endingIamge.sprite = image;
        description.text = info;

        firstTimeText.text = FormatTime(firstTime);
        bestTimeText.text = $"[{beatPlayerName}] {FormatTime(bestTime)}";

        detailPanel.SetActive(true);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void CloseDetail()
    {
        detailPanel.SetActive(false);
    }
}
