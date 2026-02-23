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
    [SerializeField] private Material deactivateButtonMaterial;
    [SerializeField] private Transform openViewPoint;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI topInfoText;

    private MeshRenderer _meshRenderer;
    public bool IsActive { get; private set; }
    public int DoorIndex { get; private set; }
    private bool _isSelected = false;

    public override void Awake()
    {
        base.Awake();

        _meshRenderer = buttonPart.GetComponent<MeshRenderer>();
        DeactivateButton(); // 처음에 버튼은 모두 비활성화 상태
        DoorIndex = torpedoTube.TorpedoTubeNum - 1;
    }

    private void OnEnable()
    {
        torpedoTube.OnUnlocked += ActivateButton;
        torpedoTube.OnDoorOpenStateChanged += ChangeDoorButtonColor;
    }

    private void OnDisable()
    {
        torpedoTube.OnUnlocked -= ActivateButton;
        torpedoTube.OnDoorOpenStateChanged -= ChangeDoorButtonColor;
    }

    public override void OnHoverEnter()
    {
        base.OnHoverEnter();

        if (!IsActive)
        {
            outline.OutlineColor = Color.red;
            infoText.color = Color.red;
            infoText.gameObject.SetActive(true);
            if (!torpedoTube.IsUnlocked) // 잠금 해제도 안된 경우
                infoText.text = $"DOOR {torpedoTube.TorpedoTubeNum} LOCKED";
            else // 문이 이미 열려있는 경우
                infoText.text = $"DOOR {torpedoTube.TorpedoTubeNum} ALREADY OPENED";
        }
        else
        {
            outline.OutlineColor = Color.green;
            infoText.color = Color.green;
            infoText.gameObject.SetActive(true);
            if (!_isSelected)
                infoText.text = "SELECT [CLICK]";
        }
    }

    public override void OnHoverExit()
    {
        base.OnHoverExit();

        infoText.gameObject.SetActive(false);
        infoText.text = "";
    }

    /// <summary>
    /// 버튼 활성화
    /// </summary>
    private void ActivateButton()
    {
        IsActive = true;
        _meshRenderer.material = redButtonMaterial;
    }

    /// <summary>
    /// 버튼 비활성화
    /// </summary>
    private void DeactivateButton()
    {
        IsActive = false;

        // 선택되어 있었으면 -> 선택 해제
        if (_isSelected)
        {
            buttonPart.DOLocalMoveZ(-0.973f, 0.5f); // 버튼 나옴
            _isSelected = false;
        }

        _meshRenderer.material = deactivateButtonMaterial;
    }

    /// <summary>
    /// 버튼 선택
    /// </summary>
    public void SelectButton()
    {
        _isSelected = true;
        buttonPart.DOLocalMoveZ(-1.02f, 0.5f); // 버튼 눌림
        _meshRenderer.material = greenButtonMaterial;
        infoText.text = "";
        topInfoText.text = "PUMP [SPACE]";
    }

    /// <summary>
    /// 버튼 선택 해제
    /// </summary>
    public void UnselectButton()
    {
        _isSelected = false;
        buttonPart.DOLocalMoveZ(-0.973f, 0.5f); // 버튼 나옴
        _meshRenderer.material = redButtonMaterial;
    }

    /// <summary>
    /// 문 앞으로 카메라 이동 후 문 열기
    /// </summary>
    public void ViewDoorOpen()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(Camera.main.transform.DOMove(openViewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.Join(Camera.main.transform.DORotateQuaternion(openViewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            torpedoTube.SetDoorOpenState(true); // 문 열기
        });
    }

    /// <summary>
    /// 문 버튼 색 변경
    /// </summary>
    /// <param name="isOpen">문 열린 여부</param>
    private void ChangeDoorButtonColor(bool isOpen)
    {
        if (isOpen)
        {
            DeactivateButton();
        }
        else
        {
            ActivateButton();
        }
    }
}
