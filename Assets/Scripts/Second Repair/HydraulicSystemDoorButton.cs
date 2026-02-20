using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class HydraulicSystemDoorButton : HoverInteractable
{
    [SerializeField] private TorpedoTube torpedoTube;
    [SerializeField] private Transform buttonPart;
    [SerializeField] private Material greenButtonMaterial;
    [SerializeField] private Material redButtonMaterial;
    [SerializeField] private Transform openViewPoint;
    [SerializeField] private TextMeshProUGUI toolTipText;

    private MeshRenderer _meshRenderer;
    public bool IsActive { get; private set; }
    public int DoorIndex { get; private set; }

    public override void Awake()
    {
        base.Awake();

        _meshRenderer = buttonPart.GetComponent<MeshRenderer>();
        IsActive = false;
        DoorIndex = torpedoTube.TorpedoTubeNum - 1;
    }

    private void OnEnable()
    {
        torpedoTube.OnUnlocked += ActivateButton;
    }

    private void OnDisable()
    {
        torpedoTube.OnUnlocked -= ActivateButton;
    }

    private void ActivateButton()
    {
        IsActive = true;
        _meshRenderer.material = redButtonMaterial;
    }

    public void SelectButton()
    {
        buttonPart.DOLocalMoveZ(-1.02f, 0.5f); // 버튼 눌림
        _meshRenderer.material = greenButtonMaterial;
    }

    public void UnselectButton()
    {
        buttonPart.DOLocalMoveZ(-0.973f, 0.5f); // 버튼 나옴
        _meshRenderer.material = redButtonMaterial;
    }

    public override void OnHoverEnter()
    {
        base.OnHoverEnter();

        if (!IsActive)
        {
            toolTipText.gameObject.SetActive(true);
            toolTipText.text = $"DOOR {torpedoTube.TorpedoTubeNum} LOCKED";
        }
        else
        {
            toolTipText.gameObject.SetActive(false);
            toolTipText.text = "";
        }
    }

    public override void OnHoverExit()
    {
        base.OnHoverExit();

        toolTipText.gameObject.SetActive(false);
        toolTipText.text = "";
    }

    public void OpenDoor()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(Camera.main.transform.DOMove(openViewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.Join(Camera.main.transform.DORotateQuaternion(openViewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            torpedoTube.SetDoorOpenState(true); // 퍼즐 시작
        });
    }
}
