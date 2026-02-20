using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum TorpedoLoadPanelButtonType { Power, Joystick, MoveUp, MoveDown, SetUp, CameraSwitch, Load, TorpedoTubeDoorButton }

public class TorpedoLoadPanelButton : HoverInteractable
{
    [SerializeField] private TorpedoLoadPanelButtonType buttonType;
    public TorpedoLoadPanelButtonType ButtonType => buttonType;
    [SerializeField] private PuzzleController puzzleController;

    public bool isCurrentActive = true; // 현재 활성화 여부
    public int torpedoTubeId; // 어뢰 발사관 문 버튼만 사용

    private Material material;

    public override void Awake()
    {
        base.Awake();

        if (!isCurrentActive)
        {
            material = GetComponent<Renderer>().material;
            material.DisableKeyword("_EMISSION");
        }
    }

    private void OnEnable()
    {
        if (buttonType == TorpedoLoadPanelButtonType.SetUp)
            TorpedoLoadPanel.OnSetUpButtonStateChanged += SetActiveButton;
        if (buttonType == TorpedoLoadPanelButtonType.Load)
            TorpedoLoadPanel.OnLoadButtonStateChanged += SetActiveButton;
    }

    private void SetActiveButton(bool isActive)
    {
        isCurrentActive = isActive;
        if (isActive)
            material.EnableKeyword("_EMISSION");
        else
            material.DisableKeyword("_EMISSION");
    }

    public override void OnHoverEnter()
    {
        base.OnHoverEnter();
    }

    public override void OnHoverExit()
    {
        base.OnHoverExit();
    }

    private void OnDisable()
    {
        if (buttonType == TorpedoLoadPanelButtonType.SetUp)
            TorpedoLoadPanel.OnSetUpButtonStateChanged -= SetActiveButton;
        if (buttonType == TorpedoLoadPanelButtonType.Load)
            TorpedoLoadPanel.OnLoadButtonStateChanged -= SetActiveButton;
    }
}
