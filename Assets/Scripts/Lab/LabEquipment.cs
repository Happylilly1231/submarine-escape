using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[System.Flags]
public enum EAcceptableType
{
    None = 0,
    Sample = 1 << 0,    // 일반 샘플 아이템
    PetriDish = 1 << 1, // 페트리 접시
    TestTube = 1 << 2   // 시험관
}

[System.Serializable]
public class SlotData
{
    public Item sample; // 꽂힌 샘플
    public LabEquipment equipment;  // 꽂힌 기구(시험관, 페트리 접시)
    public bool IsEmpty => sample == null && equipment == null; // 해당 슬롯이 비어있는지
    public bool HasEqipment => equipment != null; // 실험기구가 꽂혀있는지
}

public abstract class LabEquipment : MonoBehaviour
{
    public abstract bool IsGrabbable { get; }
    public abstract int MaxSlots { get; } // 최대 슬롯
    public abstract EAcceptableType AcceptedTypes { get; } // 기구가 허용하는 타입들
    protected abstract Vector3 gripPositionOffset { get; }
    protected abstract Vector3 gripRotationOffset { get; }
    protected abstract Vector3 slotScaleOffset { get; }
    public virtual Vector3 GetSlotPosition(int index) => Vector3.zero;
    public virtual Vector3 GetSlotRotation(int index) => Vector3.zero;

    [SerializeField] public Sprite equipmentSprite;
    [SerializeField] protected SlotData[] slots; // 현재 넣어있는 샘플 데이터들
    public SlotData[] Slots => slots;

    [Header("아이템 장착 위치 및 오른손 IK")]
    [SerializeField] private Transform itemViewRoot; // 아이템을 들고 있는 위치
    [SerializeField] private TwoBoneIKConstraint rightHandIK; // 오른손 IK

    protected virtual void Awake()
    {
        slots = new SlotData[MaxSlots];
        for (int i = 0; i < MaxSlots; i++) slots[i] = new SlotData();
    }

    /// <summary>
    /// 넣을 수 있는 타입 체크
    /// </summary>
    public virtual bool CanInsert(object obj)
    {
        if (obj is Item item && (AcceptedTypes & EAcceptableType.Sample) != 0)
        {
            return item.ItemType == EItemType.Sample;
        }
        if (obj is PetriDish && (AcceptedTypes & EAcceptableType.PetriDish) != 0) return true;
        if (obj is TestTube && (AcceptedTypes & EAcceptableType.TestTube) != 0) return true;

        return false;
    }

    /// <summary>
    /// 샘플/실험 기구 넣기
    /// <para> - 샘플: 데이터 추가 </para>
    /// <para> - 실험 기구: 손에 들고 있는 오브젝트 비활성화 및 데이터 추가</para>
    /// </summary>
    public virtual void Insert(int slotIdx, object obj)
    {
        if (slotIdx < 0 || slotIdx >= MaxSlots) return;

        // 조합 결과물이 들어간 시험관의 내용물을 페트리 접시에 붓기
        if (this is PetriDish petri && obj is TestTube testTube && testTube.IsResultTube)
        {
            List<Item> tubeSamples = testTube.GetAllNestedSamples();

            for (int i = 0; i < tubeSamples.Count; i++)
            {
                //if (i >= petri.MaxSlots) break;
                petri.Slots[i].sample = tubeSamples[i];
            }

            // 페트리 접시를 '결과물 상태'로 표시하기 위한 변수가 필요할 수 있습니다.
            // petri.MarkAsResult(); // 아래 2번 항목에서 설명

            SampleSlotUIManager.Instance.UpdateSampleUI();
            return;
        }

        // 페트리 접시를 넣으려고 할 때 (샘플만 옮기기)
        if (this is TestTube && obj is PetriDish petriDish)
        {
            // 페트리 접시 안의 모든 샘플 가져오기
            List<Item> extractedSamples = petriDish.GetAllNestedSamples();

            // 시험관의 빈 슬롯부터 순서대로 샘플 채우기
            int targetIdx = 0;
            foreach (Item s in extractedSamples)
            {
                if (targetIdx >= slots.Length) break;
                slots[targetIdx].sample = s;
                targetIdx++;
            }

            // 페트리 접시 내부 데이터 삭제
            for (int i = 0; i < petriDish.Slots.Length; i++)
            {
                petriDish.Remove(i);
            }
        }
        // 주사기에 시험관을 넣을 때 (샘플만 옮기기)
        else if (this is Syringe syringe && obj is TestTube tube)
        {
            // 시험관 안의 모든 샘플 가져오기
            List<Item> tubeSamples = tube.GetAllNestedSamples();

            // 시험관의 빈 슬롯부터 순서대로 샘플 채우기
            int targetIdx = 0;
            foreach (Item s in tubeSamples)
            {
                if (targetIdx >= slots.Length) break;
                slots[targetIdx].sample = s;
                targetIdx++;
            }

            // 데이터 이동 후 시험관 내부 비우기
            for (int i = 0; i < tube.Slots.Length; i++)
            {
                tube.Remove(i);
            }

            // 주의: 이 방식은 시험관 오브젝트는 그대로 손에 들려 있고 데이터만 빠져나갑니다.
        }
        // 일반 샘플 넣기
        else if (obj is Item item) slots[slotIdx].sample = item;
        // 다른 실험기구(시험관) 넣기
        else if (obj is LabEquipment equipment)
        {
            slots[slotIdx].equipment = equipment;
            equipment.PickUpToEquipment(this, slotIdx);
        }

        SampleSlotUIManager.Instance.UpdateSampleUI();
    }

    /// <summary>
    /// 샘플/실험 기구 빼기
    /// <para> - 샘플: 데이터 삭제 </para>
    /// <para> - 실험 기구: 기구 손에 들면서 데이터 삭제 </para>
    /// </summary>
    public virtual void Remove(int slotIdx)
    {
        if (slotIdx < 0 || slotIdx >= MaxSlots) return;

        SlotData slot = slots[slotIdx];

        if (slot.HasEqipment)
        {
            LabEquipment equipment = slot.equipment;
            slot.equipment = null;
            equipment.PickUp();
        }
        else if (slot.sample != null)
        {
            slot.sample = null;
        }

        SampleSlotUIManager.Instance.UpdateSampleUI();
    }

    /// <summary>
    /// 실험 기구 손에 들기
    /// </summary>
    public virtual void PickUp()
    {
        this.gameObject.SetActive(true);
        SetLayerRecursive(this.gameObject, LayerMask.NameToLayer("HeldItem"));

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        // Rigidbody가 있다면 물리 연산 중지
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.SetParent(itemViewRoot);
        transform.localPosition = gripPositionOffset;
        transform.localRotation = Quaternion.Euler(gripRotationOffset);
        transform.localScale = slotScaleOffset;

        rightHandIK.weight = 1f;
    }

    public virtual void PickUpToEquipment(LabEquipment parent, int idx)
    {
        SetLayerRecursive(this.gameObject, LayerMask.NameToLayer("Default"));

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        if (parent is Centrifuge centrifuge)
        {
            this.gameObject.SetActive(true);
            transform.SetParent(centrifuge.rotorTransform.GetChild(0), false);
            transform.localScale = centrifuge.slotScaleOffset;

            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }
        else if (parent is Microscope microscope)
        {
            this.gameObject.SetActive(true);
            transform.SetParent(microscope.transform, false);
            transform.localScale = microscope.slotScaleOffset;

            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }
        else
        {
            this.gameObject.SetActive(false);
            transform.SetParent(parent.transform);

            Vector3 parentScale = parent.transform.lossyScale;
            transform.localScale = new Vector3(
                slotScaleOffset.x / parentScale.x,
                slotScaleOffset.y / parentScale.y,
                slotScaleOffset.z / parentScale.z
            );
        }

        transform.localPosition = parent.GetSlotPosition(idx);
        transform.localRotation = Quaternion.Euler(parent.GetSlotRotation(idx));

        rightHandIK.weight = 0f;
    }

    /// <summary>
    /// 실험 기구 놓기
    /// </summary>
    public virtual void Place()
    {
        SetLayerRecursive(this.gameObject, LayerMask.NameToLayer("Default"));

        transform.SetParent(null);

        // transform.position = targetPosition;
        // transform.rotation = Quaternion.Euler(targetRotation);
        transform.localScale = slotScaleOffset;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // Rigidbody가 없다면 추가
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;  // 물리 법칙 적용
        rb.useGravity = true;    // 중력 적용

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
            col.isTrigger = false;
        }

        rightHandIK.weight = 0f;
    }

    /// <summary>
    /// 손에 들 실험기구의 모든 자식들까지 레이어 변경
    /// </summary>
    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// 해당 기구에 있는 샘플 데이터 가져옴
    /// </summary>
    /// <returns></returns>
    public List<Item> GetAllNestedSamples()
    {
        List<Item> allSamples = new List<Item>();

        foreach (var slot in slots)
        {
            if (slot.sample != null) allSamples.Add(slot.sample);
            if (slot.HasEqipment) allSamples.AddRange(slot.equipment.GetAllNestedSamples());
        }
        return allSamples;
    }
}
