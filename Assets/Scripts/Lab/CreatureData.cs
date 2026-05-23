using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CreatureSample
{
    public string sampleName;       // 샘플 이름
    [TextArea]
    public string sampleDetail;     // 샘플 상세 정보
    public int stockCount;          // 현재 재고 개수
}

[CreateAssetMenu(fileName = "New Creature Data", menuName = "Database/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("생물 기본 정보")]
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
}
