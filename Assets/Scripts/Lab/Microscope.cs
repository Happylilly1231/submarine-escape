using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Microscope : LabEquipment
{
    public override bool IsGrabbable => false;
    public override int MaxSlots => 1;
    public override EAcceptableType AcceptedTypes => EAcceptableType.PetriDish; // 페트리 접시만 넣을 수 있음
    protected override Vector3 gripPositionOffset => Vector3.zero;
    protected override Vector3 gripRotationOffset => Vector3.zero;
    protected override Vector3 slotScaleOffset => new Vector3(7f, 7f, 7f);
    public override Vector3 GetSlotPosition(int index) => new Vector3(0, 0.26f, -0.4f);
    public override Vector3 GetSlotRotation(int index) => new Vector3(90f, 0, 0);

    private MicroscopePuzzleController _puzzleController;

    private bool _isPowerOn = false; // 전력 켜져 있는지 여부
    public bool IsPowerOn => _isPowerOn;

    protected override void Awake()
    {
        base.Awake();
        _puzzleController = GetComponent<MicroscopePuzzleController>();
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

    /// <summary>
    /// 현미경 상호작용
    /// <para> - 전력 켜져 있을 때만 현미경 사용 가능 </para>
    /// <para> - 슬롯에 페트리 접시가 있을 때, 해당 페트리 접시를 퍼즐 컨트롤러에 전달하고 퍼즐 활성화 </para>
    /// </summary>
    /// </summary>
    public void InteractMicroscope()
    {
        if (Slots[0].IsEmpty || !_isPowerOn) return;

        if (_puzzleController != null && !_puzzleController.IsPuzzleStarted)
        {
            PetriDish petridish = Slots[0].equipment as PetriDish;
            if (petridish != null)
            {
                _puzzleController.SetTargetTube(petridish);
                _puzzleController.ActivatePuzzle();
            }
        }
    }
}
