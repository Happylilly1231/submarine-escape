using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Syringe : LabEquipment
{
    public override bool IsGrabbable => true;
    public override int MaxSlots => 1;
    public override EAcceptableType AcceptedTypes => EAcceptableType.TestTube; // 시험관만 넣을 수 있음
    protected override Vector3 gripPositionOffset => new Vector3(0.31f, -0.12f, 0.46f);
    protected override Vector3 gripRotationOffset => new Vector3(57.4f, -81.85f, 0);
    protected override Vector3 slotScaleOffset => new Vector3(0.01f, 0.01f, 0.01f);

    [Header("Syringe Visuals")]
    [SerializeField] private GameObject liquidVisual; // 주사기 안의 액체

    protected override void Awake()
    {
        base.Awake();
        // 시작 시에는 주사기가 비어있으므로 비활성화
        if (liquidVisual != null) liquidVisual.SetActive(false);
    }

    /// <summary>
    /// 주사기에는 '조합 결과물' 상태인 시험관만 넣을 수 있도록 제한
    /// </summary>
    public override bool CanInsert(object obj)
    {
        if (obj is TestTube tube)
        {
            return tube.IsResultTube;
        }
        return false;
    }

    public override void Insert(int slotIdx, object obj)
    {
        base.Insert(slotIdx, obj);

        // 내용물 활성화
        if (liquidVisual != null)
        {
            liquidVisual.SetActive(true);
        }
    }

    public override void Remove(int slotIdx)
    {
        base.Remove(slotIdx);

        // 시험관을 빼면 비주얼 비활성화
        if (liquidVisual != null)
        {
            liquidVisual.SetActive(false);
        }
    }
}
