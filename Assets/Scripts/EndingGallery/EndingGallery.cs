using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum EEndingType
{
    MonsterDeath,    // 괴물에게 죽음
    SubmarineExplode, // 잠수함 폭발
    FallingDeath,     // 낙사
    KeypadExplosion, // 키패드 폭발 대미지로 죽음
    Mutation,    // 괴물화
    Hypothermia,   // 저체온증으로 죽음
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
    public bool isUnlocked; // 해금 여부

    /// <summary>
    /// 번역된 엔딩 타이틀
    /// </summary>
    public string LocalizedEndingTitle
    {
        get
        {
            // 공백 제거
            string cleanEndingType = type.ToString().Replace(" ", "");

            string tableKey = $"Ending/{cleanEndingType}/Title";
            string localizedEndingTitle = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            // 테이블에 키가 없거나 할 경우 -> 기존 displayName 반환
            return string.IsNullOrEmpty(localizedEndingTitle) ? endingTitle : localizedEndingTitle;
        }
    }

    /// <summary>
    /// 번역된 엔딩 타이틀
    /// </summary>
    public string LocalizedEndingDescription
    {
        get
        {
            // 공백 제거
            string cleanEndingType = type.ToString().Replace(" ", "");

            string tableKey = $"Ending/{cleanEndingType}/Description";
            string localizedEndingDescription = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            // 테이블에 키가 없거나 할 경우 -> 기존 displayName 반환
            return string.IsNullOrEmpty(localizedEndingDescription) ? endingDescription : localizedEndingDescription;
        }
    }
}

public class EndingGallery : MonoBehaviour
{
    [SerializeField] private List<EndingFrameData> endingFrames;
    [SerializeField] private Button ReturnToTitleBtn;
    [SerializeField] private EndingDetailView detailView;
    [SerializeField] private AudioClip bgmSound;

    void Awake()
    {
        ReturnToTitleBtn.onClick.AddListener(GameManager.instance.ReturnToTitle);
    }

    void Start()
    {
        GameManager.instance.SetCursorVisible(true); // 커서 보이게
        UpdateGallery();
        SetupClickEvents();
        AudioManager.Instance.PlayBGM(bgmSound); // 브금 재생
    }

    private void OnEnable()
    {
        // 언어 변경 이벤트 구독
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    /// <summary>
    /// 언어가 바뀌면 자동으로 호출되는 콜백
    /// </summary>
    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateGallery();
    }

    private void OnDisable()
    {
        // 이벤트 해제 (메모리 누수 방지)
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;

        AudioManager.Instance.StopBGM(); // 브금 종료
    }

    /// <summary>
    /// 엔딩 갤러리 관리
    /// <para> - 엔딩별 최초/최단 기록 및 UI 상태 업데이트 </para>
    /// </summary>
    public void UpdateGallery()
    {
        foreach (var frame in endingFrames)
        {
            EndingRecord record = EndingSaveManager.Instance.GetEndingRecord(frame.type);

            frame.isUnlocked = record != null && record.isUnlocked;

            if (frame.isUnlocked)
            {
                frame.firstClearTime = record.firstTime;
                frame.bestClearTime = record.bestTime;
            }

            frame.displayImage.sprite = frame.isUnlocked ? frame.unlockedSprite : frame.lockedSprite;
            frame.clickButton.gameObject.SetActive(frame.isUnlocked);
            frame.endingFrame.SetTitle(frame.LocalizedEndingTitle);
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
            detailView.ShowDetail(frame.unlockedSprite, frame.LocalizedEndingDescription, frame.firstClearTime, frame.bestClearTime);
        }
    }
}
