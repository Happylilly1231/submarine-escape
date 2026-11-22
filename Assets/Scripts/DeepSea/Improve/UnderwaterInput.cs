using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnderwaterInput : MonoBehaviour
{
    // 입력
    private PlayerInput _playerInput;
    private InputAction _horizontalDodgeAction; // 좌/우 회피 액션
    private InputAction _upwardDashAction; // 위로 대시 액션
    private InputAction _stopAction; // 정지 액션
    private InputAction _lookAction; // 보기 액션
    private InputAction _glanceBackAction; // 잠깐 뒤돌아보기 액션
    private InputAction _holdGunAction; // 총 들기/내밀기 액션
    private InputAction _fireAction; // 총 발사 액션

    public bool IsStopping { get; private set; } = false;
    public Vector2 LookInput { get; private set; }

    public event Action<float> horizontalDodgeEvent;
    public event Action upwardDashEvent;
    public event Action stopEvent;
    public event Action stopCancelEvent;
    public event Action glanceBackEvent;
    public event Action glanceBackCancelEvent;
    public event Action holdGunEvent;
    public event Action holdGunCancelEvent;
    public event Action fireEvent;

    private void Awake()
    {
        // 입력 관련 변수 가져오기
        _playerInput = GetComponent<PlayerInput>();

        // 이동 입력
        _horizontalDodgeAction = _playerInput.actions.FindAction("HorizontalDodge");
        _upwardDashAction = _playerInput.actions.FindAction("UpwardDash");
        _stopAction = _playerInput.actions.FindAction("Stop");

        // 보기 입력
        _lookAction = _playerInput.actions.FindAction("DeepSea_Look");
        _glanceBackAction = _playerInput.actions.FindAction("GlanceBack");

        // 총 관련 입력
        _holdGunAction = _playerInput.actions.FindAction("HoldGun");
        _fireAction = _playerInput.actions.FindAction("Fire");
    }

    private void OnEnable()
    {
        _horizontalDodgeAction.performed += OnHorizontalDodge;
        _upwardDashAction.performed += OnUpwardDash;
        _stopAction.performed += OnStop;
        _stopAction.canceled += OnStop;

        _lookAction.performed += OnLook;
        _lookAction.canceled += OnLook;
        _glanceBackAction.performed += OnGlanceBack;
        _glanceBackAction.canceled += OnGlanceBack;

        _holdGunAction.performed += OnHoldGun;
        _holdGunAction.canceled += OnHoldGun;
        _fireAction.performed += OnFire;
    }

    private void OnDisable()
    {
        _horizontalDodgeAction.performed -= OnHorizontalDodge;
        _upwardDashAction.performed -= OnUpwardDash;
        _stopAction.performed -= OnStop;
        _stopAction.canceled -= OnStop;

        _lookAction.performed -= OnLook;
        _lookAction.canceled -= OnLook;
        _glanceBackAction.performed -= OnGlanceBack;
        _glanceBackAction.canceled -= OnGlanceBack;

        _holdGunAction.performed -= OnHoldGun;
        _holdGunAction.canceled -= OnHoldGun;
        _fireAction.performed -= OnFire;
    }

    private void OnHorizontalDodge(InputAction.CallbackContext context)
    {
        if (context.performed) horizontalDodgeEvent?.Invoke(context.ReadValue<float>());
    }

    private void OnUpwardDash(InputAction.CallbackContext context)
    {
        if (context.performed) upwardDashEvent?.Invoke();
    }

    private void OnStop(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsStopping = true;
            stopEvent?.Invoke();
        }
        else if (context.canceled)
        {
            IsStopping = false;
            stopCancelEvent?.Invoke();
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    private void OnGlanceBack(InputAction.CallbackContext context)
    {
        if (context.performed) glanceBackEvent?.Invoke();
        else if (context.canceled) glanceBackCancelEvent?.Invoke();
    }

    private void OnHoldGun(InputAction.CallbackContext context)
    {
        if (context.performed) holdGunEvent?.Invoke();
        else if (context.canceled) holdGunCancelEvent?.Invoke();
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed) fireEvent?.Invoke();
    }
}
