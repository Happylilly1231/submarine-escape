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

    protected override void Awake()
    {
        base.Awake();
        _puzzleController = GetComponent<MicroscopePuzzleController>();
    }

    public void InteractMicroscope()
    {
        if (Slots[0].IsEmpty) return;

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
