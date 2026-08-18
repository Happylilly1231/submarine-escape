using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Syringe : LabEquipment
{
    public override bool IsGrabbable => true;
    public override int MaxSlots
    {
        get
        {
            if (slots == null) return 1;

            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty) count = i + 1;
            }
            // 최소 1개는 보이게 설정
            return Mathf.Max(1, count);
        }
    }
    public override EAcceptableType AcceptedTypes => EAcceptableType.TestTube; // 시험관만 넣을 수 있음
    protected override Vector3 gripPositionOffset => new Vector3(0.31f, -0.12f, 0.46f);
    protected override Vector3 gripRotationOffset => new Vector3(57.4f, -81.85f, 0);
    protected override Vector3 slotScaleOffset => new Vector3(0.01f, 0.01f, 0.01f);

    public bool IsSuccess = false;
    public bool HasContent = false; // 현재 내용물이 있는지 여부

    [Header("Syringe Visuals")]
    [SerializeField] private GameObject liquidVisual; // 주사기 안의 액체

    protected override void Awake()
    {
        slots = new SlotData[6];
        for (int i = 0; i < slots.Length; i++) slots[i] = new SlotData();
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

        HasContent = true;

        // 내용물 활성화
        SetActiveLiquidVisual(true);
    }

    public override void Remove(int slotIdx)
    {
        base.Remove(slotIdx);

        // 슬롯이 전부 비었는지 체크 후 _isResultTube 해제
        bool isEmptyAll = true;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty)
            {
                isEmptyAll = false;
                break;
            }
        }

        if (isEmptyAll)
        {
            HasContent = false;
            IsSuccess = false; // 성공 변수도 초기화
        }

        // 시험관을 빼면 비주얼 비활성화
        SetActiveLiquidVisual(false);
    }

    /// <summary>
    /// 비주얼 활성화 여부 설정
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveLiquidVisual(bool isActive)
    {
        liquidVisual.SetActive(isActive);
    }
}
