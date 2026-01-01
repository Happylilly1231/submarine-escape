using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동(이동, 회전, 점프, 회피 등)
/// </summary>
public class PlayerMove : MonoBehaviour
{
    private CharacterController _controller;
    private Animator _animator;
    private PlayerStat _playerStat;
    private PlayerStatus _playerStatus;

    // 이동
    private Vector2 _moveInput; // 이동 입력
    private Vector3 _moveDir; // 이동 방향
    [SerializeField] private float moveSpeed; // 이동 속도
    private float _walkSpeed = 1.5f; // 걷기 속도
    private float _runSpeed = 4f; // 달리기 속도

    // 회전
    private Vector2 _lookInput; // 시야 입력
    private float _mouseX; // 마우스 x좌표
    public float MouseX => _mouseX;
    private float _mouseY; // 마우스 y좌표
    public float MouseY => _mouseY;
    [SerializeField] float mouseSensitivity = 1f; // 마우스 감도

    // 점프 & 중력
    private float _jumpSpeed = 3f; // 점프 속도
    private bool _jumpInput = false; // 점프 입력
    private float _ySpeed = 0f; // y 속도
    private float _gravity = -9.81f; // 중력
    private bool _isJumping; // 점프 중 여부
    public bool IsJumping => _isJumping;

    // 회피
    private float _dodgeSpeed = 2.5f; // 회피 속도
    private bool _isDodging = false; // 회피 중인지 여부
    public bool IsDodging => _isDodging;

    private float _currentDodgeTime = 0f; // 현재 회피 진행 시간(회피 시간으로 초기화돼서 0까지 감소)
    private float _dodgeTime = 1.2f; // 회피 시간

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _playerStat = GetComponent<PlayerStat>();
        _playerStatus = GetComponent<PlayerStatus>();
    }

    void Start()
    {
        moveSpeed = _walkSpeed; // 걷기 속도를 기본 속도로 설정
        Cursor.visible = false; // 마우스 커서 안 보이게 하기
        Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
    }

    /// <summary>
    /// W, A, S, D키 입력에 따라 이동 입력 값 업데이트
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 마우스(delta) 좌표 벡터 가져오기
    /// </summary>
    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Left Shift키 입력되는 동안만 달리기 속도로 설정, 떼면 걷기 속도로 설정
    /// </summary>
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
            moveSpeed = _runSpeed * _playerStatus.SpeedScale;
        else if (context.canceled)
            moveSpeed = _walkSpeed * _playerStatus.SpeedScale;
    }

    /// <summary>
    /// Space키가 입력되고, 컨트롤러가 바닥에 닿아있을 때 점프 입력값 true로 업데이트
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _controller.isGrounded)
        {
            _jumpInput = true;
        }
    }

    /// <summary>
    /// Escape키 입력에 따라 정지/정지 해제
    /// </summary>
    public void OnPause(InputAction.CallbackContext context)
    {
        // 정지 버튼(ESC) 눌렀을 때
        if (context.performed)
        {
            if (GameManager.instance.IsPausing) // 정지 중이면
            {
                GameManager.instance.Resume(); // 정지 해제(플레이)
            }
            else // 플레이 중이면
            {
                GameManager.instance.Pause(); // 정지
            }
        }
    }

    /// <summary>
    /// Left Control키가 입력되고 스태미나가 정상적으로 사용되면 회피 시작
    /// </summary>
    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed && _playerStat.UseStamina()) // 스태미나 사용했을 때
        {
            _isDodging = true; // 회피 중 true
            _currentDodgeTime = _dodgeTime; // 현재 회피 진행 시간을 회피 시간으로 초기화
            _animator.SetTrigger("Dodge"); // 회피 애니메이션 재생
        }
    }

    /// <summary>
    /// 정지 중이 아닐 때 회전, 회피/이동
    /// </summary>
    void Update()
    {
        if (!GameManager.instance.IsPausing && !_playerStatus.IsStunned) // 정지 중이 아닐 때 & 플레이어 상태가 스턴이 아닐 때
        {
            Rotate(); // 회전

            if (_isDodging) // 회피 중이면
            {
                Dodge(); // 회피
            }
            else // 회피 중이 아닐 때만
            {
                Move(); // 이동
            }
        }
    }

    /// <summary>
    /// 마우스 입력에 따른 플레이어 좌우 회전
    /// </summary>
    void Rotate()
    {
        // 마우스 입력
        _mouseX = _lookInput.x * mouseSensitivity;
        _mouseY = _lookInput.y * mouseSensitivity;

        // 플레이어 좌우 회전
        transform.Rotate(Vector3.up * MouseX);
    }

    /// <summary>
    /// 이동, 점프(중력 적용)
    /// </summary>
    void Move()
    {
        // 이동 방향
        _moveDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _moveDir.Normalize(); // 정규화

        // 애니메이션 파라미터 설정
        _animator.SetFloat("Speed", _moveDir.magnitude); // Idle or 이동(달리기 & 걷기)
        _animator.SetBool("isRunning", moveSpeed == _runSpeed * _playerStatus.SpeedScale); // 달리기 애니메이션

        // 중력 적용
        if (_controller.isGrounded) // 바닥에 닿아있으면
        {
            if (_ySpeed < 0f)
                _ySpeed = -0.8f; // 바닥에 붙도록 작은 값만큼 y 속도를 아래로 줌

            // // 점프 중 아님으로 초기화
            // if (_isJumping)
            //     _isJumping = false;

            // 점프
            if (_jumpInput) // 점프 입력이 들어왔을 때
            {
                _isJumping = true;
                _ySpeed = _jumpSpeed; // y 속도를 점프 속도로 초기화
                _animator.SetTrigger("Jump"); // 점프 애니메이션 재생
                _jumpInput = false; // 점프 입력을 false로 설정(중복 실행 안되도록)
            }
        }
        else // 공중
        {
            _ySpeed += _gravity * Time.deltaTime; // 중력에 따른 y 속도 계산
        }

        // 이동 + 점프
        Vector3 velocity = _moveDir * moveSpeed + Vector3.up * _ySpeed;
        _controller.Move(velocity * Time.deltaTime);

        // 바닥에 닿아있는지 여부 애니메이터에 넘기기(모든 y 계산이 다 끝난 뒤에 실행)
        _animator.SetBool("isGrounded", _controller.isGrounded);
    }

    /// <summary>
    /// 회피(중력 적용)
    /// </summary>
    void Dodge()
    {
        // 중력 적용
        if (_controller.isGrounded) // 바닥에 닿아있으면
        {
            if (_ySpeed < 0f)
                _ySpeed = -0.8f; // 바닥에 붙도록 작은 값만큼 y 속도를 아래로 줌
        }
        else // 공중
        {
            _ySpeed += _gravity * Time.deltaTime; // 중력에 따른 y 속도 계산
        }

        Vector3 velocity = _moveDir * _dodgeSpeed + Vector3.up * _ySpeed;
        _controller.Move(velocity * Time.deltaTime);

        _currentDodgeTime -= Time.deltaTime;

        if (_currentDodgeTime <= 0)
        {
            _currentDodgeTime = 0f;
            _isDodging = false;
            Debug.Log("회피 끝");
        }
    }
}
