using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private Light flashlightLight; // 손전등의 라이트 컴포넌트
    private PlayerInput _playerInput; // 플레이어 입력 컴포넌트
    private InputAction _toggleFlashlightAction; // 손전등 토글 입력 액션
    private CrewRoom1KeyPad _crewRoom1KeyPad;
    private CrewRoom1KeyPadPanel _crewRoom1KeyPadPanel;

    void Awake()
    {
        _playerInput = SubmarineInGameManager.instance.player.GetComponent<PlayerInput>();
        _crewRoom1KeyPad = FindObjectOfType<CrewRoom1KeyPad>();
        _crewRoom1KeyPadPanel = FindObjectOfType<CrewRoom1KeyPadPanel>();

        if (_playerInput != null)
        {
            _toggleFlashlightAction = _playerInput.actions["ToggleFlashlight"];
        }
    }

    void OnEnable()
    {
        if (_toggleFlashlightAction != null)
        {
            _toggleFlashlightAction.performed += OnToggleFlashlight;
        }
    }

    void OnDisable()
    {
        if (_toggleFlashlightAction != null)
        {
            _toggleFlashlightAction.performed -= OnToggleFlashlight;
        }
    }

    private void OnToggleFlashlight(InputAction.CallbackContext context)
    {
        if ((_crewRoom1KeyPad != null && _crewRoom1KeyPad.IsFocused) || (_crewRoom1KeyPadPanel != null && _crewRoom1KeyPadPanel.IsFocused)) return;
        flashlightLight.enabled = !flashlightLight.enabled;
    }
}
