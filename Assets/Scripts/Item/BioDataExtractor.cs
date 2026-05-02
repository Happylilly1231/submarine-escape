using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생체 데이터 추출기 데이터
/// </summary>
[System.Serializable]
public class BioDataExtractorData : IItemStateData
{
    public int currentBloodSegments; // 현재 피 차있는 칸 (0~3)
}

public class BioDataExtractor : MonoBehaviour, IStatableItem
{
    [SerializeField] private GameObject[] bloodSegments; // 피 차있는 칸
    public int CurrentBloodSegments { get; set; } = 0; // 현재 피 차있는 칸 (0~3)

    public int ItemInstanceNum { get; set; } = 0;

    public IItemStateData GetStateData()
    {
        return new BioDataExtractorData { currentBloodSegments = CurrentBloodSegments }; // 현재 추출기의 상태 데이터 반환
    }

    public void SetStateData(IItemStateData data)
    {
        // 현재 추출기의 상태를 인자로 받은 상태 데이터로 변경
        if (data is BioDataExtractorData extractionData)
        {
            CurrentBloodSegments = extractionData.currentBloodSegments;
        }

        // 피 칸 업데이트
        UpdateBloodSegments();
    }

    public void Awake()
    {
        UpdateBloodSegments(); // 피 칸 업데이트 (처음에 아예 안 차있게)
    }

    /// <summary>
    /// 채우기(충전)
    /// </summary>
    public void Fill()
    {
        if (CurrentBloodSegments < bloodSegments.Length)
        {
            CurrentBloodSegments = bloodSegments.Length; // 최대로 채우기
            UpdateBloodSegments(); // 피 칸 업데이트
            Debug.Log("피 최대 충전 완료");
        }
    }

    /// <summary>
    /// 소모
    /// </summary>
    /// <param name="segment">소모 칸 수</param>
    public void Use(int segment = 1)
    {
        if (CurrentBloodSegments > 0)
        {
            CurrentBloodSegments -= segment;
            UpdateBloodSegments(); // 피 칸 업데이트
            Debug.Log("피 1칸 소모");
        }
    }

    /// <summary>
    /// 피 칸 업데이트
    /// </summary>
    public void UpdateBloodSegments()
    {
        for (int i = 0; i < bloodSegments.Length; i++)
        {
            // 현재 순회 중인 인덱스가 현재 피가 차있는 칸보다 작으면 활성화
            bloodSegments[i].SetActive(i < CurrentBloodSegments);
        }

        // 아이템 상태가 갱신되었으므로 -> 현재 아이템 상태를 딕셔너리에서도 갱신
        if (ItemInstanceNum != 0)
            StatableItemManager.Instance.UpdateSavedItemState(ItemInstanceNum, GetStateData());
    }

    /// <summary>
    /// 피가 이미 추출기에 가득 차있는지 검사
    /// </summary>
    /// <returns>가득 차있는지 여부</returns>
    public bool CheckIsFull()
    {
        return CurrentBloodSegments == bloodSegments.Length;
    }
}
