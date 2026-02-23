using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum TorpedoLoadPanelButtonType { Power, Joystick, MoveUp, MoveDown, SetUp, CameraSwitch, Load, TorpedoTubeDoorButton }

public class TorpedoLoadPanelButton : HoverInteractable
{
    [SerializeField] private TorpedoLoadPanelButtonType buttonType;
    public TorpedoLoadPanelButtonType ButtonType => buttonType;
    [SerializeField] private TorpedoTube linkedTorpedoTube; // 어뢰 발사관 문 버튼만 연결된 어뢰 발사관 알기 위해 사용
    public TorpedoTube LinkedTorpedoTube => linkedTorpedoTube;

    public bool isCurrentActive = true; // 현재 활성화 여부
    //public int torpedoTubeId; // 어뢰 발사관 문 버튼만 사용

    private Material material;

    public override void Awake()
    {
        base.Awake();

        if (buttonType == TorpedoLoadPanelButtonType.SetUp
        || buttonType == TorpedoLoadPanelButtonType.Load
        || buttonType == TorpedoLoadPanelButtonType.TorpedoTubeDoorButton)
        {
            material = GetComponent<Renderer>()?.material;

            if (!isCurrentActive)
            {
                material.DisableKeyword("_EMISSION");
            }
        }
    }

    private void OnEnable()
    {
        switch (buttonType)
        {
            case TorpedoLoadPanelButtonType.SetUp:
                TorpedoLoadPanel.OnSetUpButtonStateChanged += SetActiveButton;
                break;
            case TorpedoLoadPanelButtonType.Load:
                TorpedoLoadPanel.OnLoadButtonStateChanged += SetActiveButton;
                break;
            case TorpedoLoadPanelButtonType.TorpedoTubeDoorButton:
                linkedTorpedoTube.OnDoorOpenStateChanged += ChangeDoorButtonColor;
                break;
        }
    }

    private void OnDisable()
    {
        switch (buttonType)
        {
            case TorpedoLoadPanelButtonType.SetUp:
                TorpedoLoadPanel.OnSetUpButtonStateChanged -= SetActiveButton;
                break;
            case TorpedoLoadPanelButtonType.Load:
                TorpedoLoadPanel.OnLoadButtonStateChanged -= SetActiveButton;
                break;
            case TorpedoLoadPanelButtonType.TorpedoTubeDoorButton:
                linkedTorpedoTube.OnDoorOpenStateChanged -= ChangeDoorButtonColor;
                break;
        }
    }

    private void SetActiveButton(bool isActive)
    {
        isCurrentActive = isActive;
        if (isActive)
            material.EnableKeyword("_EMISSION");
        else
            material.DisableKeyword("_EMISSION");
    }

    private void ChangeDoorButtonColor(bool isOpen)
    {
        if (isOpen)
        {
            material.color = Color.green;
            material.SetColor("_EmissionColor", Color.green);
        }
        else
        {
            material.color = Color.red;
            material.SetColor("_EmissionColor", Color.red);
        }
    }
}
