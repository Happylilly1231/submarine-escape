using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class GalleryCameraController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("카메라 회전")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 70f;

    // 입력
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _ascendAction;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isAscending;

    // 카메라 회전값
    private float _pitch;
    private float _yaw;

    private CharacterController _controller;

    public bool CanMove { get; private set; } = true;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();

        // 기존 심해 Input Action 재사용
        _moveAction = _playerInput.actions.FindAction("DeepSea_Move");

        _lookAction = _playerInput.actions.FindAction("DeepSea_Look");

        _ascendAction = _playerInput.actions.FindAction("DeepSea/Ascend");

        // 초기 카메라 회전값 저장
        _pitch = Mathf.DeltaAngle(
            0f, transform.eulerAngles.x);

        _yaw = transform.eulerAngles.y;

        // 마우스 커서 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!CanMove) return;

        ReadInputs();
        Rotate();
        Move();
    }

    #region 입력

    private void ReadInputs()
    {
        // WASD
        _moveInput = _moveAction.ReadValue<Vector2>();

        // 마우스
        _lookInput = _lookAction.ReadValue<Vector2>();

        // Space
        _isAscending = _ascendAction.IsPressed();
    }

    #endregion

    #region 회전

    private void Rotate()
    {
        // 좌우 회전
        _yaw += _lookInput.x * mouseSensitivity;

        // 상하 회전
        _pitch -= _lookInput.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        transform.rotation =
            Quaternion.Euler(_pitch, _yaw, 0f);
    }

    #endregion

    #region 이동

    private void Move()
    {
        Vector3 moveDir;

        if (_isAscending)
        {
            Quaternion yawRotation =
                Quaternion.Euler(0f, _yaw, 0f);

            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;

            moveDir = (
                forward * _moveInput.y +
                right * _moveInput.x
            ).normalized;

            moveDir.y = 1f;
            moveDir.Normalize();
        }
        else
        {
            moveDir = (
                transform.forward * _moveInput.y +
                transform.right * _moveInput.x
            ).normalized;
        }

        // 충돌을 고려한 카메라 이동
        _controller.Move(
            moveDir * moveSpeed * Time.deltaTime
        );
    }

    #endregion
}
