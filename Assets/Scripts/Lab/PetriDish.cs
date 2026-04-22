using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetriDish : LabEquipment
{
    public override bool IsGrabbable => true;
    public override int MaxSlots
    {
        get
        {
            // 결과물이 담긴 상태라면, 슬롯 배열에서 비어있지 않은 데이터의 개수를 반환
            if (_containsResult)
            {
                int count = 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null && !slots[i].IsEmpty) count = i + 1;
                }
                return Mathf.Max(1, count); // 최소 1개 보장
            }

            // 일반 상태일 때는 기존처럼 2개만 노출
            return 2;
        }
    }
    public override EAcceptableType AcceptedTypes => EAcceptableType.Sample; // 샘플만 넣을 수 있음
    protected override Vector3 gripPositionOffset => new Vector3(0.27f, -0.13f, 0.44f);
    protected override Vector3 gripRotationOffset => new Vector3(60f, 0, 0);
    protected override Vector3 slotScaleOffset => new Vector3(1.2f, 1.2f, 1.2f);

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer[] sampleRenderers; // 샘플 이미지들
    [SerializeField] private Sprite circle;

    // 결과물이 담겼는지 확인하는 플래그
    private bool _containsResult = false;
    public bool ContainsResult => _containsResult;
    public bool IsSuccess { get; private set; } = false;
    public bool IsH1 { get; private set; } = false;
    public bool IsH2S1 { get; private set; } = false;

    protected override void Awake()
    {
        slots = new SlotData[6]; // 원심분리 결과가 최대 6개이므로 6개 확보
        for (int i = 0; i < slots.Length; i++) slots[i] = new SlotData();
        RefreshVisuals(); // 초기 상태 갱신
    }

    public override void Insert(int slotIdx, object obj)
    {
        base.Insert(slotIdx, obj);
        RefreshVisuals();
    }

    public override void Remove(int slotIdx)
    {
        if (_containsResult) return;
        base.Remove(slotIdx);
        RefreshVisuals();
    }

    /// <summary>
    /// 현재 Slots 데이터를 기반으로 자식 이미지들의 활성화 및 스프라이트 변경
    /// </summary>
    private void RefreshVisuals()
    {
        if (sampleRenderers == null) return;

        for (int i = 0; i < sampleRenderers.Length; i++)
        {
            if (i >= MaxSlots) break;

            // 데이터가 있으면 켜고, 없으면 끔
            bool hasData = !Slots[i].IsEmpty;
            sampleRenderers[i].gameObject.SetActive(hasData);

            // 데이터가 있다면 해당 샘플의 아이콘으로 스프라이트 교체
            if (hasData && Slots[i].sample != null)
            {
                if (_containsResult)
                {
                    sampleRenderers[i].sprite = circle;
                    sampleRenderers[i].color = Color.red;
                }

                else
                {
                    sampleRenderers[i].sprite = Slots[i].sample.ItemImage;
                    sampleRenderers[i].color = Color.white;
                }
            }
        }
    }

    /// <summary>
    /// 조합 결과물이 담겼는지 확인
    /// </summary>
    /// <param name="isResult"></param>
    public void SetAsResult(bool isResult, bool isSuccess, bool isH1, bool isH2S1)
    {
        _containsResult = isResult;
        IsSuccess = isSuccess;
        IsH1 = isH1;
        IsH2S1 = isH2S1;
        SampleSlotUIManager.Instance.UpdateSampleUI();
    }
}
