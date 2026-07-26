using UnityEngine;
using UnityEngine.Localization.Settings;

[CreateAssetMenu(fileName = "NewReport", menuName = "Database/ReportData")]
public class ReportData : ScriptableObject
{
    public string reportId;
    public string title;
    [TextArea(10, 20)]
    public string content;

    // 1. 보고서 제목 번역
    public string LocalizedTitle
    {
        get
        {
            if (string.IsNullOrEmpty(reportId)) return title;

            string cleanName = reportId.Replace(" ", "");
            string tableKey = $"Research/Report/{cleanName}/Title";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            return string.IsNullOrEmpty(localized) ? title : localized;
        }
    }

    // 2. 보고서 본문 번역
    public string LocalizedContent
    {
        get
        {
            if (string.IsNullOrEmpty(reportId)) return content;

            string cleanName = reportId.Replace(" ", "");
            string tableKey = $"Research/Report/{cleanName}/Content";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            return string.IsNullOrEmpty(localized) ? content : localized;
        }
    }
}
