using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePointPanel : PuzzleController, IInteractable
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    private Collider _collider;
    private SavePointBtn _savePointBtn;

    [SerializeField] private GameObject interactUI;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _savePointBtn = GetComponent<SavePointBtn>();
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        return LocalizationHelper.GetLocalizedInteractText("Interact/SavePointPanel", "E");
    }

    public void Interact()
    {
        ActivatePuzzle(); // 패널 활성화
    }
    #endregion

    #region PuzzleController
    public override void ActivatePuzzle()
    {
        base.ActivatePuzzle();

        interactUI.SetActive(false);
        _savePointBtn.UpdateSavePointUI();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        _collider.enabled = false; // 콜라이더 비활성화 (UI로 쏘는 레이를 가리지 않도록)
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        interactUI.SetActive(true);
        _collider.enabled = true;
    }
    #endregion
}
