using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class ElectricalBox : InteractableBase
{
    [SerializeField] private GameObject electricalBox_door;

    private PowerController _powerController;

    private void Start()
    {
        _powerController = FindObjectOfType<PowerController>();
    }

    #region 상호작용 인터페이스 구현
    public override string GetInteractText()
    {
        if (!_powerController.IsComplete) return LocalizationHelper.GetLocalizedInteractText("Interact/Open", "E");
        else return "";
    }

    public override void Interact()
    {
        if (_powerController.IsComplete) return;
        _powerController.ActivatePuzzle();
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
    #endregion

    public void OpenDoor(Action onComplete)
    {
        electricalBox_door.transform.DOLocalRotate(new Vector3(90, 0, -30), 0.8f)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void CloseDoor(Action onComplete)
    {
        // 기존: new Vector3(90, 0, 66)
        electricalBox_door.transform.DOLocalRotate(new Vector3(90, 0, 66), 0.8f)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => onComplete?.Invoke());
    }
}
