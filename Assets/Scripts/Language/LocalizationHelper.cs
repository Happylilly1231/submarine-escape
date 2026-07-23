using UnityEngine.Localization.Settings;

public static class LocalizationHelper
{
    /// <summary>
    /// ST_UI 테이블에서 키를 읽어오고 선택적으로 키 바인딩을 덧붙임
    /// </summary>
    public static string GetLocalizedInteractText(string key, string keyBinding = "")
    {
        string translatedText = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", key);

        if (string.IsNullOrEmpty(keyBinding))
            return translatedText;

        return $"{translatedText} [{keyBinding}]";
    }

    /// <summary>
    /// 파라미터가 포함된 번역문을 가져와 StringBuilder에 추가합니다.
    /// </summary>
    public static string GetLocalizedInteractTextWithParameter(string key, string paramValue, string keyBinding = "")
    {
        // {0} 자리 <- paramValue
        string translatedText = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", key, new object[] { paramValue });

        if (string.IsNullOrEmpty(keyBinding))
            return translatedText;
        else
            return $"{translatedText} [{keyBinding}]";
    }
}
