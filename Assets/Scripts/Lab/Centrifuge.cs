using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Centrifuge : LabEquipment
{
    public override bool IsGrabbable => false;
    public override int MaxSlots => 3;
    public override EAcceptableType AcceptedTypes => EAcceptableType.TestTube; // 시험관만 넣을 수 있음
    protected override Vector3 gripPositionOffset => Vector3.zero;
    protected override Vector3 gripRotationOffset => Vector3.zero;
    protected override Vector3 slotScaleOffset => new Vector3(0.007f, 0.017f, 0.007f);
    public override Vector3 GetSlotPosition(int index) => slotPositions[index];
    public override Vector3 GetSlotRotation(int index) => slotRotations[index];

    [Header("Slot Transform Settings")]
    [SerializeField] private Vector3[] slotPositions = new Vector3[3];
    [SerializeField] private Vector3[] slotRotations = new Vector3[3];

    [Header("Centrifuge Visuals")]
    [SerializeField] private Transform glassTransform; // 뚜껑 오브젝트
    public Transform rotorTransform; // 회전판 오브젝트

    private bool _isOperating = false;
    public bool IsOperating => _isOperating;

    private InventoryManager _inventoryManager;

    private bool _isPowerOn = false; // 전력 켜져 있는지 여부
    public bool IsPowerOn => _isPowerOn;

    protected override void Awake()
    {
        base.Awake();
        _inventoryManager = FindObjectOfType<InventoryManager>();
    }

    private void OnEnable()
    {
        LightingManager.instance.OnLightChanged += SetPower;
    }

    void OnDisable()
    {
        LightingManager.instance.OnLightChanged -= SetPower;
    }

    /// <summary>
    /// 전력 켜거나 끄기
    /// </summary>
    private void SetPower(bool isPowerOn)
    {
        _isPowerOn = isPowerOn;
    }

    // 작동 가능 조건 체크 - 시험관 3개
    public bool CanStartOperation()
    {
        // 슬롯 3개가 꽉 찼는지 확인
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots[i].IsEmpty) return false;

            // 해당 슬롯의 시험관 안에 샘플이 하나라도 있는지 확인
            LabEquipment testTube = Slots[i].equipment;
            bool hasSample = false;
            for (int j = 0; j < testTube.MaxSlots; j++)
            {
                if (!testTube.Slots[j].IsEmpty)
                {
                    hasSample = true;
                    break;
                }
            }
            if (!hasSample) return false; // 하나라도 샘플이 없는 시험관이 있으면 불가
        }
        return true;
    }

    /// <summary>
    /// 작동 시작
    /// </summary>
    public void StartCentrifuge()
    {
        if (_isOperating || !_isPowerOn) return;

        _isOperating = true;

        // 뚜껑 닫기
        glassTransform.DOLocalRotate(new Vector3(90f, 0, 0), 1.2f)
            .OnComplete(() =>
            {
                // 회전판 돌리기
                Tween rotorTween = rotorTransform.DOLocalRotate(new Vector3(0, 360f, 0), 0.3f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental)
                    .SetEase(Ease.Linear);

                DOVirtual.DelayedCall(3f, () =>
                {
                    // 회전 멈추기
                    rotorTween.Kill();
                    ShowFinalResult();

                    // 뚜껑 다시 열기
                    glassTransform.DOLocalRotate(new Vector3(0, 0, 0), 1.2f)
                        .SetEase(Ease.OutCubic)
                        .OnComplete(() =>
                    {
                        _isOperating = false;
                        _inventoryManager.UpdateActionText();
                    });
                });
            });
    }

    /// <summary>
    /// 최종 결과 표시
    /// </summary>
    public void ShowFinalResult()
    {
        List<Item> allCollectedSamples = new List<Item>();

        // 모든 슬롯에서 모든 샘플 추출 및 데이터 삭제
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots[i].HasEqipment)
            {
                LabEquipment tube = Slots[i].equipment;

                // 모든 중첩 샘플 가져오기
                List<Item> samplesInTube = tube.GetAllNestedSamples();
                allCollectedSamples.AddRange(samplesInTube);

                // 추출 후 해당 시험관 내부 데이터는 비움
                for (int j = 0; j < tube.MaxSlots; j++) tube.Remove(j);
            }
        }

        // 2번 시험관에 모든 샘플 데이터 넣기
        if (Slots[1].HasEqipment && Slots[1].equipment is TestTube targetTube)
        {
            targetTube.UpdateResultData(allCollectedSamples);
        }

        SampleSlotUIManager.Instance.UpdateSampleUI();
    }
}
