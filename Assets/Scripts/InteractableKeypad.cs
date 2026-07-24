using System.Collections;
using System.Collections.Generic;
using NavKeypad;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractableKeypad : PuzzleController, IInteractable
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    [SerializeField] private GameObject target; // 목표 (이 비밀번호를 풀음으로써 잠금 해제되는 것 (문, 상자 등))

    private bool _isUnlocked = false;
    private Collider _collider;
    private Keypad _keypad;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _keypad = GetComponent<Keypad>();
    }

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!_isUnlocked)
            return LocalizationHelper.GetLocalizedInteractText("Interact/EnterPassword", "E");

        return "";
    }

    public void Interact()
    {
        if (!_isUnlocked)
            ActivatePuzzle();
    }
    #endregion

    #region PuzzleController
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        _collider.enabled = false; // 키패드 콜라이더가 버튼 콜라이더를 가리지 않도록

        Click.performed += OnClickPerformed; // 클릭 performed 사용
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        _collider.enabled = true;

        Click.performed -= OnClickPerformed;
    }
    #endregion

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out var hit))
        {
            Debug.Log(hit.collider.name);
            if (hit.collider.TryGetComponent(out KeypadButton keypadButton))
            {
                keypadButton.PressButton();
            }
        }
    }

    /// <summary>
    /// 잠금 해제
    /// </summary>
    public void UnLock()
    {
        _isUnlocked = true; // 잠금 해제로 설정

        // 목표 대상에 따른 잠금 해제 로직
        if (target != null)
        {
            if (target.TryGetComponent(out Door door)) // 문
            {
                door.isLocked = false; // 잠금 해제
            }
        }

        ExitPuzzle(); // 퍼즐 종료
    }

    /// <summary>
    /// 키패드 초기화
    /// </summary>
    public void ResetKeypad()
    {
        _keypad.ResetKeypad();

        // 잠금 해제 여부 초기화
        _isUnlocked = false;
        if (target != null)
        {
            if (target.TryGetComponent(out Door door)) // 문
            {
                door.SetDoorOpenState(false); // 문 닫기
                door.isLocked = true; // 잠금됨으로 다시 설정
            }
        }
    }
}
