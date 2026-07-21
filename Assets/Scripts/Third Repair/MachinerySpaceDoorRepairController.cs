using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class MachinerySpaceDoorRepairController : PuzzleController, IInteractable
{
    protected override bool IsHoverRequired => true;

    protected override bool IsMouseRequiredAtFirst => true;

    [SerializeField] private GameObject boxDoor;
    [SerializeField] private GameObject emergencyLockdownSwitch;
    [SerializeField] private GameObject emergencyLockdownSwitchHandle;
    [SerializeField] private GameObject sparkParticle;

    public bool IsEmergencyLockdown { get; private set; } = true;
    private bool _isSwitchMoving = false;

    private Collider _boxCollider;
    private AudioSource _sparkAudioSource;

    private void Awake()
    {
        _boxCollider = GetComponent<Collider>();
        _sparkAudioSource = GetComponent<AudioSource>();
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (IsEmergencyLockdown)
            return "Open [E]";
        else
            return "";
    }

    public void Interact()
    {
        if (!IsEmergencyLockdown)
            return;

        ActivatePuzzle(); // 패널 활성화
    }
    #endregion

    #region PuzzleController
    public override void ActivatePuzzle()
    {
        SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화
        itemEquipController.UnequipItem(); // 아이템 장착 해제
        SubmarineInGameManager.instance.InteractorUI.SetActive(true); // 상호작용 UI 활성화

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        Click.performed += OnClickPerformed;

        OpenDoor(); // 함 문 열기
    }

    public override void ExitPuzzle()
    {
        // 스위치 움직이는 중에는 퍼즐 종료 불가
        if (_isSwitchMoving)
            return;

        base.ExitPuzzle();

        Click.performed -= OnClickPerformed;

        CloseDoor(); // 함 문 닫기
    }
    #endregion

    #region 입력 이벤트 함수
    /// <summary>
    /// 마우스 좌클릭 -> 스위치를 클릭했을 때만 Off 가능
    /// </summary>
    /// <param name="context"></param>
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // 스위치 움직이는 중이거나 이미 락다운이 해제된 경우 클릭 이벤트 받지 않음
        if (_isSwitchMoving || !IsEmergencyLockdown)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == emergencyLockdownSwitch)
            {
                EmergencyLockdownSwitchOff();
            }
        }
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 함 문 열기
    /// </summary>
    public void OpenDoor()
    {
        _boxCollider.enabled = false; // 안의 콜라이더 감지 되도록 박스 전체의 콜라이더는 끄기
        sparkParticle.SetActive(true); // 전기 파티클 켜기
        _sparkAudioSource.Play(); // 스파크 소리 켜기
        boxDoor.transform.DOLocalRotate(new Vector3(0f, -130f, 0f), 0.8f)
            .SetEase(Ease.OutBounce);
    }

    /// <summary>
    /// 함 문 닫기
    /// </summary>
    public void CloseDoor()
    {
        _boxCollider.enabled = true; // 박스 전체 콜라이더 복구
        sparkParticle.SetActive(false); // 스파크 파티클 끄기 
        _sparkAudioSource.Stop(); // 스파크 소리 끄기
        boxDoor.transform.DOLocalRotate(Vector3.zero, 0.8f)
            .SetEase(Ease.OutBounce);
    }

    /// <summary>
    /// 비상 폐쇠 스위치 끄기
    /// </summary>
    public void EmergencyLockdownSwitchOff()
    {
        _isSwitchMoving = true;
        emergencyLockdownSwitchHandle.transform.DOLocalRotate(Vector3.zero, 1f)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                IsEmergencyLockdown = false;
                _isSwitchMoving = false;
                ExitPuzzle(); // 퍼즐 자동 종료
            });
    }
    #endregion
}
