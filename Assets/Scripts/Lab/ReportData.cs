using UnityEngine;

[CreateAssetMenu(fileName = "NewReport", menuName = "Database/ReportData")]
public class ReportData : ScriptableObject
{
    public string title;
    [TextArea(10, 20)]
    public string content;
}
