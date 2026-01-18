using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EEndingType
{
    MonsterDeath,    // 괴물에게 죽음
    SubmarineExplode, // 잠수함 폭발
    FallingDeath,     // 낙사
    KeypadExplosion, // 키패드 폭발 대미지로 죽음
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
    [HideInInspector] public float firstClearTime; // 최초 기록
    [HideInInspector] public float bestClearTime;  // 최단 기록
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

    public void UpdateGallery()
    {
        foreach (var frame in endingFrames)
        {
            string key = "Ending_" + frame.type.ToString();

            // 해금 여부 확인 (PlayerPrefs)
            bool isUnlocked = PlayerPrefs.GetInt(key, 0) == 1;

            if (isUnlocked)
            {
                frame.firstClearTime = PlayerPrefs.GetFloat(key + "_FirstTime");
                frame.bestClearTime = PlayerPrefs.GetFloat(key + "_BestTime");
            }

            frame.displayImage.sprite = isUnlocked ? frame.unlockedSprite : frame.lockedSprite;
            frame.clickButton.gameObject.SetActive(isUnlocked);
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
        string key = "Ending_" + frame.type.ToString();
        bool isUnlocked = PlayerPrefs.GetInt(key, 0) == 1;

        if (isUnlocked)
        {
            // 해금된 경우 상세 설명창 띄우기
            detailView.ShowDetail(frame.unlockedSprite, frame.endingDescription, frame.firstClearTime, frame.bestClearTime);
        }
    }
}
