using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum EEndingType
{
    MonsterDeath,    // 괴물에게 죽음
    SubmarineExplode, // 잠수함 폭발
    FallingDeath,     // 낙사
    KeypadExplosion, // 키패드 폭발 대미지로 죽음
    Mutation,    // 괴물화
    EscapeSuccess     // 탈출 성공
}

[System.Serializable]
public class EndingFrameData
{
    public EEndingType type;      // 엔딩 종류
    public Image displayImage;    // UI 상의 Image 컴포넌트
    public Sprite lockedSprite;   // 잠겨있을 때 보일 이미지
    public Sprite unlockedSprite; // 해금되었을 때 보일 실제 엔딩 이미지
    public Button clickButton; // 확대 버튼
    public EndingFrame endingFrame; // 엔딩
    public string endingTitle; // 엔딩 제목
    [TextArea]
    public string endingDescription; // 엔딩 설명
    // 해당 엔딩을 클리어하는 데 걸린 시간
    // [HideInInspector] public float firstClearTime; // 최초 기록
    // [HideInInspector] public float bestClearTime;  // 최단 기록
    public bool isUnlocked; // 해금 여부
    [HideInInspector] public float currentPlayerTime; // 현재 플레이어의 클리어 시간
    [HideInInspector] public float bestClearTime;  // 최단 기록
    [HideInInspector] public string beatPlayerName; // 최단 기록을 달성한 플레이어 이름
}

public class EndingGallery : MonoBehaviour
{
    [SerializeField] private List<EndingFrameData> endingFrames;
    [SerializeField] private Button ReturnToTitleBtn;
    [SerializeField] private EndingDetailView detailView;

    void OnEnable()
    {
        UpdateGallery();
    }


    void Awake()
    {
        ReturnToTitleBtn.onClick.AddListener(GameManager.instance.ReturnToTitle);
    }

    void Start()
    {
        UpdateGallery();
        SetupClickEvents();
    }

    // 한 명의 플레이어가 단독으로 플레이할 때 '엔딩별 최초/최단 기록'을 관리
    // public void UpdateGallery()
    // {
    //     foreach (var frame in endingFrames)
    //     {
    //         string key = "Ending_" + frame.type.ToString();

    //         // 해금 여부 확인 (PlayerPrefs)
    //         bool isUnlocked = PlayerPrefs.GetInt(key, 0) == 1;

    //         if (isUnlocked)
    //         {
    //             frame.firstClearTime = PlayerPrefs.GetFloat(key + "_FirstTime");
    //             frame.bestClearTime = PlayerPrefs.GetFloat(key + "_BestTime");
    //         }

    //         frame.displayImage.sprite = isUnlocked ? frame.unlockedSprite : frame.lockedSprite;
    //         frame.clickButton.gameObject.SetActive(isUnlocked);
    //         frame.endingFrame.SetTitle(frame.endingTitle);
    //     }
    // }

    /// <summary>
    /// 엔딩 갤러리 업데이트 - PlayerPrefs에서 전체 기록을 불러와서 각 프레임의 해금 여부와 기록을 갱신
    /// </summary>
    public void UpdateGallery()
    {
        // PlayerPrefs에서 전체 기록 JSON 데이터 불러오기
        string jsonData = PlayerPrefs.GetString("RankingsData", "");
        RankingsData rankingsData = JsonUtility.FromJson<RankingsData>(jsonData);

        foreach (var frame in endingFrames)
        {
            // 전체 기록 중 현재 엔딩에 해당하는 기록만 필터링
            List<PlayerRecord> records = new List<PlayerRecord>();
            if (rankingsData != null && rankingsData.playerRecords != null)
            {
                records = rankingsData.playerRecords.Where(r => r.endingType == frame.type).ToList();
            }

            // 오직 현재 플레이어 이름과 일치하는 기록만 필터링하여 해금 여부 판단
            var myRecords = records.Where(r => r.playerName == GameManager.instance.PlayerName).OrderByDescending(r => r.playDate).ToList();
            frame.isUnlocked = myRecords.Count > 0;

            if (frame.isUnlocked)
            {
                // 가장 최근 기록을 현재 플레이어의 기록으로 사용
                frame.currentPlayerTime = myRecords[0].clearTime;

                // 전체 기록 중 최단 기록을 찾아서 프레임에 저장
                var sortedBest = records.OrderBy(r => r.clearTime).First();
                frame.bestClearTime = sortedBest.clearTime;
                frame.beatPlayerName = sortedBest.playerName;
            }
            else
            {
                // 해금되지 않은 경우 초기값 설정
                frame.currentPlayerTime = 0f;
                frame.bestClearTime = 0f;
                frame.beatPlayerName = "";
            }

            // UI 업데이트
            frame.displayImage.sprite = frame.isUnlocked ? frame.unlockedSprite : frame.lockedSprite;
            frame.clickButton.gameObject.SetActive(frame.isUnlocked);
            frame.endingFrame.SetTitle(frame.endingTitle);
        }
    }

    private void SetupClickEvents()
    {
        foreach (var frame in endingFrames)
        {
            // 각 프레임의 버튼에 클릭 이벤트 할당
            frame.clickButton.onClick.AddListener(() =>
            {
                OnFrameClick(frame);
            });
        }
    }

    private void OnFrameClick(EndingFrameData frame)
    {
        if (frame.isUnlocked)
        {
            // 해금된 경우 상세 설명창 띄우기
            detailView.ShowDetail(frame.unlockedSprite, frame.endingDescription, frame.currentPlayerTime, frame.bestClearTime, frame.beatPlayerName);
        }
    }
}
