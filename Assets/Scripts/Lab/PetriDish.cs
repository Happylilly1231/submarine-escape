using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PetriDish : LabEquipment, IInteractable
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

    // 괴물 피 떨어뜨려서 H1 인자 들어갔는지 여부 테스트
    [SerializeField] private Item bioDataExtractorItem; // 생체 데이터 추출기 아이템
    private InventoryManager _inventoryManager;
    private ItemEquipController _itemEquipController;
    public bool isMonsterBloodDropped = false; // 내부 괴물 피가 페트리 접시에 떨어뜨려졌는지 여부
    public bool isMonsterBloodMixed = false; // 내부 괴물 피가 흔들려 섞였는지 여부
    public bool isMixing = false; // 섞는 중 여부

    protected override void Awake()
    {
        slots = new SlotData[6]; // 원심분리 결과가 최대 6개이므로 6개 확보
        for (int i = 0; i < slots.Length; i++) slots[i] = new SlotData();
        RefreshVisuals(); // 초기 상태 갱신

        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
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

                    if (!isMonsterBloodDropped)
                    {
                        sampleRenderers[i].color = Color.red;
                    }
                    else // 조합 결과물에 내부 괴물 피를 떨어뜨렸을 때 -> H1 인자 들어있는지 여부에 따른 적합성 테스트
                    {
                        // 적합성 판정
                        if (IsH1) // H1 인자 O -> 정상(빨간색) 
                        {
                            sampleRenderers[i].color = Color.red;
                        }
                        else // H1 인자 X -> 비정상(검정색)
                        {
                            sampleRenderers[i].color = Color.black;
                        }
                    }
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

    public string GetInteractText()
    {
        if (IsRequiredItemSelected())
        {
            if (isMonsterBloodMixed) return "";

            if (!_containsResult) return "No Compound Detected";
            if (isMonsterBloodDropped) return "Mixing Required";

            if (_itemEquipController.HeldItemObject.GetComponent<BioDataExtractor>().CurrentBloodSegments > 0)
                return "Drop Monster Blood [E]";
            else
                return "Extractor Is Empty"; // 추출기가 비어있음 메시지로 알려줌
        }
        else
            return "";
    }

    public void Interact()
    {
        if (!_containsResult || isMonsterBloodDropped) return;

        if (IsRequiredItemSelected() && _itemEquipController.HeldItemObject.TryGetComponent(out BioDataExtractor bioDataExtractor) && bioDataExtractor.CurrentBloodSegments > 0)
        {
            isMonsterBloodDropped = true;
            bioDataExtractor.Use(); // 추출기 피 1칸 소모
            Debug.Log(gameObject.name + "에 괴물 피를 떨어뜨렸습니다.");
        }
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return item == bioDataExtractorItem;
    }

    /// <summary>
    /// 상호작용에 필요한 아이템이 선택되어있는지 여부 반환하는 함수
    /// </summary>
    /// <returns></returns>
    private bool IsRequiredItemSelected()
    {
        Item selectedItem = null;
        if (_itemEquipController.HeldItemData != null)
        {
            selectedItem = _itemEquipController.HeldItemData;
        }
        else if (_inventoryManager.SelectedSlotIndex >= 0 && _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex] != null)
        {
            selectedItem = _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item;
        }

        return CanInteractwithSelectedItem(selectedItem);
    }

    /// <summary>
    /// 괴물 피와 섞기
    /// </summary>
    public void MixMonsterBlood()
    {
        isMixing = true;
        SubmarineInGameManager.instance.SetFocus(true);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOLocalRotate(new Vector3(90f, 0f, 0f), 0.3f).SetEase(Ease.OutCubic)); // 바로 세우기

        seq.Append(transform.DOShakePosition(1.5f, new Vector3(0.02f, 0f, 0.02f), 4, 90f, false, true).SetRelative(true)); // 흔들기
        seq.JoinCallback(() =>
        {
            sampleRenderers[1].gameObject.SetActive(true);
        });
        seq.Join(sampleRenderers[1].DOColor(new Color(138f / 255f, 0f, 41f / 255f), 0.75f));

        if (IsH1)
            seq.Append(sampleRenderers[1].DOColor(Color.red, 0.75f));
        else
            seq.Append(sampleRenderers[1].DOColor(Color.black, 0.75f));

        // for (int i = 0; i < sampleRenderers.Length; i++)
        // {
        //     if (i >= MaxSlots) break;

        //     // 데이터가 있으면 켜고, 없으면 끔
        //     bool hasData = !Slots[i].IsEmpty;
        //     sampleRenderers[i].gameObject.SetActive(hasData);

        //     // 데이터가 있다면 해당 샘플의 아이콘으로 스프라이트 교체
        //     if (hasData && Slots[i].sample != null)
        //     {
        //         if (_containsResult)
        //         {
        //             seq.Join(sampleRenderers[i].DOColor(new Color(138f / 255f, 0f, 41f / 255f), 1.5f));

        //         }
        //     }
        // }

        seq.OnComplete(() =>
        {
            RefreshVisuals(); // 비주얼 업데이트 
            if (rb != null)
                rb.isKinematic = false;
            SubmarineInGameManager.instance.SetFocus(false);
            isMonsterBloodMixed = true;
            isMixing = false;
        });

        // seq.Append(sampleRenderers[i].DOColor(new Color(120f / 255f, 90f / 255f, 100f / 255f), 1.5f));
    }
}
