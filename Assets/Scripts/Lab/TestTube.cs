using System.Collections;
using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

public class TestTube : LabEquipment
{
    public override bool IsGrabbable => true;
    private bool _isResultTube = false; // 결과물 상태인지 여부
    [SerializeField] public bool IsResultTube => _isResultTube;
    public bool IsSuccess { get; private set; } = false;
    public bool IsH1 { get; private set; } = false;
    public bool IsH2S1 { get; private set; } = false;
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
    public override EAcceptableType AcceptedTypes => EAcceptableType.Sample | EAcceptableType.PetriDish; // 샘플과 페트리 접시 넣을 수 있음
    protected override Vector3 gripPositionOffset => new Vector3(0.33f, -0.13f, 0.44f);
    protected override Vector3 gripRotationOffset => new Vector3(9f, 0, 0);
    protected override Vector3 slotScaleOffset => new Vector3(0.017f, 0.05f, 0.017f);

    [Header("Visual Settings")]
    [SerializeField] private GameObject[] resultVisuals; // 결과물용
    [SerializeField] private GameObject[] normalVisuals; // 일반 샘플용

    protected override void Awake()
    {
        slots = new SlotData[6]; // 원심분리 결과가 최대 6개이므로 6개 확보
        for (int i = 0; i < slots.Length; i++) slots[i] = new SlotData();
    }

    public override void Insert(int slotIdx, object obj)
    {
        base.Insert(slotIdx, obj);
        UpdateVisuals();
    }

    public override void Remove(int slotIdx)
    {
        base.Remove(slotIdx);
        UpdateVisuals();
    }

    public void AnalyzeResult()
    {
        bool hasBioluminescence = false; // 발광체 존재 여부
        bool hasMetabolicSerum = false;  // 추출 혈액 존재 여부
        bool hasHardShell = false;      // 강화등껍질 존재 여부

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty) continue;

            string itemName = slots[i].sample.DisplayName;

            if (itemName == "발광체") hasBioluminescence = true;
            if (itemName == "대사 억제 혈청") hasMetabolicSerum = true;
            if (itemName == "강화 등껍질") hasHardShell = true;
        }

        // H2-S1 복합체 성립 조건: 대사 억제 혈청 + 강화등껍질
        bool hasH2S1 = hasMetabolicSerum && hasHardShell;

        // 현미경: H2-S1 복합체가 있다면 success, 없으면 false
        IsH2S1 = hasH2S1;
        // 피의 색깔: H1이 있으면 빨간색(true), 없으면 검정색(false)
        IsH1 = hasBioluminescence;

        Debug.Log(hasBioluminescence + " " + hasMetabolicSerum + " " + hasHardShell);
        // 최종 성공 조건: 발광체가 있어 피가 빨간색이고, H2-S1 복합체가 완성되어 고요할 때
        if (hasBioluminescence && hasH2S1)
        {
            IsSuccess = true;
            Debug.Log("<color=green>치료제 제조 성공!</color>");
        }
        else
        {
            IsSuccess = false;
            Debug.Log("<color=red>치료제 제조 실패.</color>");
        }
    }

    /// <summary>
    /// 비주얼 갱신 로직
    /// </summary>
    private void UpdateVisuals()
    {
        // 조합 결과물일 때
        bool isResult = _isResultTube;
        foreach (var obj in resultVisuals)
        {
            if (obj != null) obj.SetActive(isResult);
        }

        // 일반 샘플일 때
        // 결과물이 아닐 때만 일반 샘플 비주얼을 보여줌
        for (int i = 0; i < normalVisuals.Length; i++)
        {
            if (normalVisuals[i] == null) continue;

            // 결과물 상태가 아니고, 해당 슬롯에 데이터가 있을 때만 활성화
            bool hasNormalSample = !isResult && !slots[i].IsEmpty;
            normalVisuals[i].SetActive(hasNormalSample);

            if (hasNormalSample)
            {
                var sr = normalVisuals[i].GetComponent<SpriteRenderer>();
                if (sr != null && slots[i].sample != null)
                {
                    sr.sprite = slots[i].sample.ItemImage;
                }
            }
        }
    }

    public void UpdateResultData(List<Item> newSamples)
    {
        // 기존 슬롯 데이터 초기화
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].sample = null;
            slots[i].equipment = null;
        }

        // 새로운 샘플들을 순서대로 할당
        int count = Mathf.Min(newSamples.Count, slots.Length);
        for (int i = 0; i < count; i++)
        {
            slots[i].sample = newSamples[i];
        }

        // 데이터가 주입되면 결과물 상태로 전환
        _isResultTube = (newSamples != null && newSamples.Count > 0);
        AnalyzeResult();
        UpdateVisuals();
    }

    public override bool CanInsert(object obj)
    {
        // 조합 결과물인 페트리 접시는 시험관에 넣을 수 없음 (안 그러면 샘플 복제됨)
        if (obj is PetriDish petriDish1 && petriDish1.ContainsResult)
            return false;
        if (obj is PetriDish) return true;
        return base.CanInsert(obj);
    }
}
