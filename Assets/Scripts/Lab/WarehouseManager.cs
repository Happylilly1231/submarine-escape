using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct SampleGroup
{
    public string creatureName; // 생물 이름
    public SampleData[] samples; // 해당 생물의 샘플
}

[System.Serializable]
public struct SampleData
{
    public string sampleName; // 샘플 이름
    public List<GameObject> sampleObjects; // 필드에 배치된 실제 오브젝트들
}

public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager instance;

    public List<SampleGroup> warehouseStocks = new List<SampleGroup>();

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    /// <summary>
    /// 샘플이 획득되어 파괴될 때 호출 (재고 감소)
    /// </summary>
    /// <param name="sampleObj"></param>
    public void ReportDestroyed(GameObject sampleItem)
    {
        foreach (var group in warehouseStocks)
        {
            foreach (var data in group.samples)
            {
                if (data.sampleObjects.Contains(sampleItem))
                {
                    data.sampleObjects.Remove(sampleItem);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 특정 샘플의 현재 남은 개수 반환
    /// </summary>
    public int GetStockCount(string creatureName, string sampleName)
    {
        var group = warehouseStocks.Find(x => x.creatureName == creatureName);
        if (group.creatureName != null)
        {
            var data = group.samples.FirstOrDefault(s => s.sampleName == sampleName);
            if (data.sampleObjects != null)
            {
                // 리스트에서 null(파괴된 오브젝트)을 제거하고 남은 개수만 반환
                data.sampleObjects.RemoveAll(obj => obj == null);
                return data.sampleObjects.Count;
            }
        }
        return 0;
    }
}
