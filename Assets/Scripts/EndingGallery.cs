using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EEndingType
{
    MonsterDeath,    // 괴물에게 죽음
    SubmarineExplode, // 잠수함 폭발
    FallingDeath,     // 낙사
    EscapeSuccess     // 탈출 성공
}

[System.Serializable]
public class EndingFrameData
{
    public EEndingType type;      // 엔딩 종류
    public Image displayImage;    // UI 상의 Image 컴포넌트
    public Sprite lockedSprite;   // 잠겨있을 때 보일 이미지
    public Sprite unlockedSprite; // 해금되었을 때 보일 실제 엔딩 이미지
}

public class EndingGallery : MonoBehaviour
{
    [SerializeField] private List<EndingFrameData> endingFrames;
    [SerializeField] private Button ReturnToTitleBtn;

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
    }

    public void UpdateGallery()
    {
        foreach (var frame in endingFrames)
        {
            string key = "Ending_" + frame.type.ToString();

            // 해금 여부 확인 (PlayerPrefs)
            bool isUnlocked = PlayerPrefs.GetInt(key, 0) == 1;

            // 이미지 교체
            frame.displayImage.sprite = isUnlocked ? frame.unlockedSprite : frame.lockedSprite;
        }
    }
}
