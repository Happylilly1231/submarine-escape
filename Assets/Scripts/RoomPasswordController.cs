using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NavKeypad;

public class RoomPasswordController : MonoBehaviour
{
    [SerializeField] private InteractableKeypad keypad1Interactable;
    [SerializeField] private InteractableKeypad keypad2Interactable;

    private Keypad _keypad1;
    private Keypad _keypad2;

    private void Awake()
    {
        if (keypad1Interactable != null)
            _keypad1 = keypad1Interactable.GetComponent<Keypad>();

        if (keypad2Interactable != null)
            _keypad2 = keypad2Interactable.GetComponent<Keypad>();
    }

    private void OnEnable()
    {
        // 1번 키패드 이벤트 연결
        if (_keypad1 != null)
        {
            _keypad1.OnAccessGranted.AddListener(keypad1Interactable.UnLock);
            _keypad1.OnAccessDenied.AddListener(keypad1Interactable.ExitPuzzle);
        }

        // 2번 키패드 이벤트 연결
        if (_keypad2 != null)
        {
            _keypad2.OnAccessGranted.AddListener(keypad2Interactable.UnLock);
            _keypad2.OnAccessDenied.AddListener(keypad2Interactable.ExitPuzzle);
        }
    }

    private void OnDisable()
    {
        // 이벤트 해제
        if (_keypad1 != null)
        {
            _keypad1.OnAccessGranted.RemoveListener(keypad1Interactable.UnLock);
            _keypad1.OnAccessDenied.RemoveListener(keypad1Interactable.ExitPuzzle);
        }

        if (_keypad2 != null)
        {
            _keypad2.OnAccessGranted.RemoveListener(keypad2Interactable.UnLock);
            _keypad2.OnAccessDenied.RemoveListener(keypad2Interactable.ExitPuzzle);
        }
    }

    /// <summary>
    /// 선원실 키패드 암호 설정
    /// </summary>
    public void SetUpPuzzle()
    {
        if (_keypad1 != null)
        {
            int password1 = 0525;
            _keypad1.keypadCombo = password1;
        }

        if (_keypad2 != null)
        {
            int password2 = 9264;
            _keypad2.keypadCombo = password2;
        }
    }
}
