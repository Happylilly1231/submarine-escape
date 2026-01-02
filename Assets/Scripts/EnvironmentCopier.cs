using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// Lighting의 Environment를 복사하고 붙여넣을 수 있는 도구를 에디터에 추가한다.
/// <para>- [복사]: 원본 씬 열기 - 상단 메뉴 Tools > Lighting > Copy Environment Settings 클릭</para>
/// <para>- [붙여넣기]: 대상 씬 열기 - 상단 메뉴 Tools > Lighting > Paste Environment Settings 클릭</para>
/// </summary>
public class EnvironmentCopier : EditorWindow
{
    private static RenderSettingsData copiedSettings;

    // 복사할 데이터를 담아둘 구조체
    private class RenderSettingsData
    {
        public Material skybox;
        public AmbientMode ambientMode;
        public Color ambientSkyColor;
        public Color ambientEquatorColor;
        public Color ambientGroundColor;
        public bool fog;
        public Color fogColor;
        public FogMode fogMode;
        public float fogDensity;
    }

    [MenuItem("Tools/Lighting/Copy Environment Settings")]
    public static void CopySettings()
    {
        copiedSettings = new RenderSettingsData
        {
            skybox = RenderSettings.skybox,
            ambientMode = RenderSettings.ambientMode,
            ambientSkyColor = RenderSettings.ambientSkyColor,
            ambientEquatorColor = RenderSettings.ambientEquatorColor,
            ambientGroundColor = RenderSettings.ambientGroundColor,
            fog = RenderSettings.fog,
            fogColor = RenderSettings.fogColor,
            fogMode = RenderSettings.fogMode,
            fogDensity = RenderSettings.fogDensity
        };
        Debug.Log("Lighting Environment settings copied!");
    }

    [MenuItem("Tools/Lighting/Paste Environment Settings")]
    public static void PasteSettings()
    {
        if (copiedSettings == null)
        {
            Debug.LogError("No lighting settings copied!");
            return;
        }

        Undo.RecordObject(FindObjectOfType<Light>(), "Paste Lighting Settings"); // 실행 취소 가능하도록 기록

        RenderSettings.skybox = copiedSettings.skybox;
        RenderSettings.ambientMode = copiedSettings.ambientMode;
        RenderSettings.ambientSkyColor = copiedSettings.ambientSkyColor;
        RenderSettings.ambientEquatorColor = copiedSettings.ambientEquatorColor;
        RenderSettings.ambientGroundColor = copiedSettings.ambientGroundColor;
        RenderSettings.fog = copiedSettings.fog;
        RenderSettings.fogColor = copiedSettings.fogColor;
        RenderSettings.fogMode = copiedSettings.fogMode;
        RenderSettings.fogDensity = copiedSettings.fogDensity;

        Debug.Log("Lighting Environment settings pasted!");
    }
}