using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class EndingUIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject gameClearUI;
    [SerializeField] private Volume gameOverVolume;
    [SerializeField] private Volume gameClearVolume;
    [SerializeField] private Button[] gameOverButtons;
    [SerializeField] private Button[] gameClearButtons;

    // [SerializeField] private Button restartButton;
    // [SerializeField] private Button returnToTitleButton;
    // [SerializeField] private Button quitButton;

    [SerializeField] private TextMeshProUGUI endingTitleText; // 엔딩 제목 텍스트


    void Awake()
    {
        if (GameManager.instance.IsClear) // 게임 클리어
        {
            gameClearVolume.weight = 1f;
            gameOverVolume.weight = 0f;

            gameClearUI.SetActive(true);
            gameOverUI.SetActive(false);

            gameClearButtons[0].onClick.AddListener(GameManager.instance.StartGame);
            gameClearButtons[1].onClick.AddListener(GameManager.instance.ReturnToTitle);
            gameClearButtons[2].onClick.AddListener(GameManager.instance.QuitGame);
        }
        else // 게임 오버
        {
            gameClearVolume.weight = 0f;
            gameOverVolume.weight = 1f;

            gameClearUI.SetActive(false);
            gameOverUI.SetActive(true);

            gameOverButtons[0].onClick.AddListener(GameManager.instance.StartGame);
            gameOverButtons[1].onClick.AddListener(GameManager.instance.ReturnToTitle);
            gameOverButtons[2].onClick.AddListener(GameManager.instance.QuitGame);
        }

        EEndingType lastEndingType = EndingSaveManager.Instance.GetLatestEndingType();

        endingTitleText.text = GetLocalizedEndingTitle(lastEndingType);
    }

    /// <summary>
    /// 번역된 엔딩 타이틀
    /// </summary>
    public string GetLocalizedEndingTitle(EEndingType type)
    {
        // 공백 제거
        string cleanEndingType = type.ToString().Replace(" ", "");

        string tableKey = $"Ending/{cleanEndingType}/Title";
        string localizedEndingTitle = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

        // 테이블에 키가 없거나 할 경우 -> 기존 displayName 반환
        return string.IsNullOrEmpty(localizedEndingTitle) ? "" : localizedEndingTitle;
    }
}
