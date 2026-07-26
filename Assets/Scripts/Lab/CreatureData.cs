using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

[System.Serializable]
public struct CreatureSample
{
    public string sampleId; // 샘플 id (샘플의 itemName과 동일)
    public string sampleName;       // 샘플 이름
    [TextArea]
    public string sampleDetail;     // 샘플 상세 정보
    public int stockCount;          // 현재 재고 개수

    // 번역된 샘플 이름 (Key 형식: Item/Name/{sampleId = 샘플의 itemName(공백X)})
    public string LocalizedSampleName
    {
        get
        {
            if (string.IsNullOrEmpty(sampleId)) return sampleName;

            string cleanName = sampleId.Replace(" ", "");
            string tableKey = $"Item/Name/{cleanName}";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            return string.IsNullOrEmpty(localized) ? sampleName : localized;
        }
    }

    public string GetLocalizedSampleDetail
    {
        get
        {
            if (string.IsNullOrEmpty(sampleId)) return sampleDetail;

            string cleanName = sampleId.Replace(" ", "");
            string tableKey = $"Research/Sample/{cleanName}/Detail";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            return string.IsNullOrEmpty(localized) ? sampleDetail : localized;
        }
    }
}

[CreateAssetMenu(fileName = "New Creature Data", menuName = "Database/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("생물 기본 정보")]
    [SerializeField] private string creatureId; // 생물 Id
    [SerializeField] private string creatureName; // 생물 이름
    [SerializeField] private Sprite creatureImage; // 생물 이미지
    [TextArea]
    [SerializeField] private string biologicalInfo; // 생체 정보
    [Header("샘플 정보")]
    [SerializeField] private CreatureSample[] samples = new CreatureSample[3];
    [SerializeField] string shelfLocation; // 창고 선반 위치 (UI 텍스트용)
    [SerializeField] private int shelfIndex; // 창고 선반 인덱스 (배열 접근용 인덱스)

    // 읽기 전용
    public string CreatureName => creatureName;
    public Sprite CreatureImage => creatureImage;
    public string BiologicalInfo => biologicalInfo;
    public CreatureSample[] Samples => samples;
    public string ShelfLocation => shelfLocation;
    public int ShelfIndex => shelfIndex;

    // 번역된 생물 이름 (Key 형식: Creature/{creatureName(공백X)}/Name)
    public string LocalizedCreatureName
    {
        get
        {
            if (string.IsNullOrEmpty(creatureId)) return creatureName;

            string cleanName = creatureId.Replace(" ", "");
            string tableKey = $"Research/Creature/{cleanName}/Name";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            return string.IsNullOrEmpty(localized) ? creatureName : localized;
        }
    }

    // 번역된 생체 정보 (Key: Creature/{creatureName(공백X)}/Info)
    public string LocalizedBiologicalInfo
    {
        get
        {
            if (string.IsNullOrEmpty(creatureId)) return biologicalInfo;

            string cleanName = creatureId.Replace(" ", "");
            string tableKey = $"Research/Creature/{cleanName}/Info";
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", tableKey);

            return string.IsNullOrEmpty(localized) ? biologicalInfo : localized;
        }
    }
}
