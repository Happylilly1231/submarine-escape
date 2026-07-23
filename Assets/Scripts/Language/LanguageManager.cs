using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageManager : MonoBehaviour
{
    private bool _isChanging = false;

    public static LanguageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 언어 변경 함수
    /// </summary>
    /// <param name="localeID">0: 영어 / 1: 한국어</param>
    public void ChangeLanguage(int localeID)
    {
        if (_isChanging) return;
        StartCoroutine(SetLocale(localeID));
    }

    private IEnumerator SetLocale(int localeID)
    {
        _isChanging = true;

        // Localization 시스템이 준비될 때까지 대기
        yield return LocalizationSettings.InitializationOperation;

        // 선택한 언어로 변경
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];

        _isChanging = false;
    }

    /// <summary>
    /// 현재 설정된 언어의 인덱스를 가져오는 함수
    /// </summary>
    public void GetCurrentLanguageIndex(Action<int> onComplete)
    {
        StartCoroutine(GetCurrentLanguageIndexCoroutine(onComplete));
    }

    public IEnumerator GetCurrentLanguageIndexCoroutine(Action<int> onComplete)
    {
        // 초기화 완료될 때까지 대기
        yield return LocalizationSettings.InitializationOperation;

        var currentLocale = LocalizationSettings.SelectedLocale;
        int currentIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(currentLocale);

        onComplete?.Invoke(currentIndex);
    }
}
