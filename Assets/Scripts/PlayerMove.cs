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

    private bool _isRunning = false;

    // 점프 & 중력
    private float _jumpSpeed = 3f; // 점프 속도
    private bool _jumpInput = false; // 점프 입력
    private float _ySpeed = 0f; // y 속도
    private float _gravity = -9.81f; // 중력
    private bool _isJumping; // 점프 중 여부
    private bool _isJumpingDown = false; // 점프 하강 중 여부
    private float fallDamageSpeed = -10f;   // 이 속도보다 빠르면 데미지

    // 회피
    private bool _dodgeInput;
    private float _dodgeSpeed = 2.5f; // 회피 속도
    private bool _isDodging = false; // 회피 중인지 여부
    public bool IsDodging => _isDodging;

    private float _currentDodgeTime = 0f; // 현재 회피 진행 시간(회피 시간으로 초기화돼서 0까지 감소)
    private float _dodgeTime = 1.2f; // 회피 시간

    // 사다리 타기
    private float _climbSpeed = 1.5f; // 사다리 타기 속도
    private bool _isClimbing = false; // 타는 중 여부
    private bool _isMovingToTargetSafe = false; // 사다리에서 목표 위치로 안전 이동 중인지 여부

    private bool _canMove = true; // 이동 가능 여부
    private bool _isApplingGravity = true; // 중력 적용 중 여부

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip runSound;
    [SerializeField] private AudioClip jumpLandingSound;
    [SerializeField] private AudioClip climbingLadderSound;

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
        if (!_canMove) return;

        if (context.performed)
        {
            _isRunning = true;
        }
        else if (context.canceled)
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Space키가 입력되고, 컨트롤러가 바닥에 닿아있을 때 점프 입력값 true로 업데이트
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!_canMove) return;

        if (context.performed && _controller.isGrounded)
        {
            _jumpInput = true;
        }
    }

    /// <summary>
    /// Left Control키가 입력되고 스태미나가 정상적으로 사용되면 회피 시작
    /// </summary>
    public void OnDodge(InputAction.CallbackContext context)
    {
        if (!_canMove) return;

        if (_isClimbing) // 사다리 오르는 중 -> 회피 불가능
            return;

        _dodgeInput = context.performed;

        if (_dodgeInput && _playerStat.UseStamina()) // 스태미나 사용했을 때
        {
            _isDodging = true; // 회피 중 true
            _currentDodgeTime = _dodgeTime; // 현재 회피 진행 시간을 회피 시간으로 초기화
            _animator.SetTrigger("Dodge"); // 회피 애니메이션 재생
        }
    }

    /// <summary>
    /// 정지 중이 아닐 때 회전, 회피/이동
    /// </summary>
    private void Update()
    {
        // 정지 중 -> 이동 불가
        if (SubmarineInGameManager.instance.IsPausing) return;

        // 움직임 허용 안됨 -> 이동 불가, 단, 중력 적용 중일 때는 중력에 의한 움직임만 예외적으로 가능
        if (!_canMove)
        {
            if (_isApplingGravity) // 중력 적용 중일 때만 -> 중력에 의한 움직임만 실행
            {
                CalculateGravity(); // 중력 연산
                _controller.Move(Vector3.up * _ySpeed * Time.deltaTime); // y 이동만 처리
                _animator.SetBool("isGrounded", _controller.isGrounded); // 바닥에 닿아있는지 여부 애니메이터에 넘기기(모든 y 계산이 다 끝난 뒤에 실행)
            }
            return;
        }

        if (_isRunning)
            moveSpeed = _runSpeed * _playerStatus.SpeedScale;
        else
            moveSpeed = _walkSpeed * _playerStatus.SpeedScale;

        // 플레이어 상태가 스턴일 때 -> 오직 중력만 계산
        if (_playerStatus.IsStunned)
        {
            CalculateGravity(); // 중력 연산

            // 중력에 따른 이동
            Vector3 velocity = Vector3.up * _ySpeed;
            _controller.Move(velocity * Time.deltaTime);

            return;
        }

        // 상하 회전 값 업데이트
        _mouseY = _lookInput.y * GameManager.instance.MouseSensitivity;

        // 사다리에서 목표 위치로 안전 이동 중이면 -> 움직일 수 X
        if (_isMovingToTargetSafe)
            return;

        // 사다리 타기 중 -> 사다리 타기
        if (_isClimbing)
        {
            ClimbLadder(); // 사다리 타기
            return;
        }

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

    /// <summary>
    /// 마우스 입력에 따른 플레이어 좌우 회전
    /// </summary>
    private void Rotate()
    {
        // 마우스 입력
        _mouseX = _lookInput.x * GameManager.instance.MouseSensitivity;

        // 플레이어 좌우 회전
        transform.Rotate(Vector3.up * MouseX);
    }

    /// <summary>
    /// 중력 연산
    /// </summary>
    private void CalculateGravity()
    {
        if (_controller.isGrounded) // 바닥에 닿아있으면
        {
            // 높은 곳에서 착지 시(점프 중 아닐 때) -> 고정 낙하 대미지
            if (_ySpeed < fallDamageSpeed)
            {
                _playerStat.Damage(5f, EEndingType.FallingDeath);
            }

            // 점프 착지(점프가 끝나서 바닥에 닿은 거면) -> 점프 중 아님으로 설정
            if (_isJumping && _ySpeed <= 0f) // 점프 시작 시 바로 바닥에서 떨어지지 않을 수 있기 때문에 ySpeed가 0 이하인지도 함께 검사
            {
                audioSource.PlayOneShot(jumpLandingSound);
                AudioManager.Instance.PlayGlobalOneShot(jumpLandingSound);
                _isJumping = false;
            }

            if (_ySpeed < 0f)
                _ySpeed = -0.8f; // 바닥에 붙도록 작은 값만큼 y 속도를 아래로 줌
        }
        else // 공중
        {
            _ySpeed += _gravity * Time.deltaTime; // 중력에 따른 y 속도 계산

            // 점프 하강 -> 점프 하강 애니메이션 설정
            if (!_isJumpingDown && _ySpeed < 0f)
            {
                _isJumpingDown = true;
                _animator.SetBool("isJumpingDown", true);
            }
        }
    }

    /// <summary>
    /// 회피(중력 적용)
    /// </summary>
    private void Dodge()
    {
        CalculateGravity(); // 중력 연산

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

    /// <summary>
    /// 이동, 점프(중력 적용)
    /// </summary>
    private void Move()
    {
        // 이동 방향
        _moveDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _moveDir.Normalize(); // 정규화

        // 애니메이션 파라미터 설정
        _animator.SetFloat("Speed", _moveDir.magnitude); // Idle or 이동(달리기 & 걷기)

        _animator.SetBool("isRunning", _isRunning); // 달리기 애니메이션

        // 바닥에 닿아있을 때만 -> 점프 가능
        if (_controller.isGrounded)
        {
            // 점프 입력 -> 점프
            if (_jumpInput) // 점프 입력이 들어왔을 때
            {
                _isJumping = true;
                _ySpeed = _jumpSpeed; // y 속도를 점프 속도로 초기화
                _animator.SetTrigger("Jump"); // 점프 애니메이션 재생
                _isJumpingDown = false;
                _animator.SetBool("isJumpingDown", false);
                _jumpInput = false; // 점프 입력을 false로 설정(중복 실행 안되도록)
            }
        }

        // 걷기 / 달리기 소리 재생
        if (_moveDir.magnitude < 0.1f || _isJumping)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
        else if (!_isRunning)
            AudioManager.Instance.PlaySoundSafe(audioSource, walkSound, 0.75f);
        else if (_isRunning)
            AudioManager.Instance.PlaySoundSafe(audioSource, runSound, 1.3f);

        // 중력 연산
        CalculateGravity();

        // 이동 + 점프
        Vector3 velocity = _moveDir * moveSpeed + Vector3.up * _ySpeed;
        _controller.Move(velocity * Time.deltaTime);

        // 바닥에 닿아있는지 여부 애니메이터에 넘기기(모든 y 계산이 다 끝난 뒤에 실행)
        _animator.SetBool("isGrounded", _controller.isGrounded);
    }

    /// <summary>
    /// 사다리 타기
    /// </summary>
    private void ClimbLadder()
    {
        // 위 아래 방향 설정
        int v = 0;
        if (_moveInput.y > 0.1f) // 위 이동 키(W) -> 오르기
            v = 1;
        else if (_moveInput.y < -0.1f) // 아래 이동 키(S) -> 내려가기
            v = -1;

        // 위 아래 이동
        Vector3 velocity = Vector3.up * v * _climbSpeed;
        _controller.Move(velocity * Time.deltaTime);

        // 애니메이션 설정
        _animator.SetInteger("climbDirection", v);

        if (v != 0)
        {
            AudioManager.Instance.PlaySoundSafe(audioSource, climbingLadderSound);
        }
    }

    /// <summary>
    /// 트리거 감지되는 동안
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        // 사다리 맨 위에 닿은 경우
        if (other.CompareTag("LadderTop"))
        {
            // 사다리에서 목표 위치로 안전 이동 중이면 -> 아무것도 x
            if (_isMovingToTargetSafe)
                return;

            // 타기 시작 / 그만 타기
            if (!_isClimbing) // 타는 중 X
            {
                // 위 이동 키(W) 누르기 -> 타기 시작
                if (_moveInput.y > 0.1f)
                {
                    Vector3 startPos = other.transform.GetChild(0).position;
                    StartCoroutine(MoveToTargetSafe(startPos, true)); // 해당 위치로 안전 이동 시작
                }
            }
            else // 타는 중
            {
                // 위 이동 키(W) -> 그만 타기
                if (_moveInput.y > 0.1f)
                {
                    Vector3 rightExitPos = other.transform.GetChild(1).position; // 오른쪽 바닥 위치
                    Vector3 leftExitPos = other.transform.GetChild(2).position; // 왼쪽 바닥 위치

                    // 기본 목표 위치: 오른쪽 바닥 위치
                    Vector3 exitPos = rightExitPos;
                    float rightDist = Vector3.Distance(SubmarineInGameManager.instance.innerMonsterTransform.position, rightExitPos);
                    if (rightDist <= 3f)
                    {
                        // 단, 괴물이 오른쪽 바닥 위치에 가깝게 있다면 -> 목표 위치: 왼쪽 바닥 위치
                        float leftDist = Vector3.Distance(SubmarineInGameManager.instance.innerMonsterTransform.position, leftExitPos);
                        if (rightDist < leftDist)
                            exitPos = leftExitPos;
                    }

                    StartCoroutine(MoveToTargetSafe(exitPos, false)); // 해당 위치로 안전 이동 시작
                }
            }
        }

        // 사다리 맨 밑에 닿은 경우
        if (other.CompareTag("LadderBottom"))
        {
            // 사다리에서 목표 위치로 안전 이동 중이면 -> 아무것도 x
            if (_isMovingToTargetSafe)
                return;

            Debug.Log("_isClimbing: " + _isClimbing);

            // 타기 시작 / 그만 타기
            if (!_isClimbing) // 타는 중 X
            {
                // 위 이동 키(W) 누르기 -> 타기 시작
                if (_moveInput.y > 0.1f)
                {
                    Vector3 startPos = other.transform.GetChild(0).position;
                    StartCoroutine(MoveToTargetSafe(startPos, true)); // 해당 위치로 안전 이동 시작
                }
            }
            else // 타는 중
            {
                // 아래 이동 키(S) -> 그만 타기
                if (_moveInput.y < -0.1f)
                {
                    Vector3 exitPos = other.transform.GetChild(1).position;
                    StartCoroutine(MoveToTargetSafe(exitPos, false)); // 해당 위치로 안전 이동 시작
                }
            }
        }
    }

    /// <summary>
    /// 내부 괴물이 목표 위치에서 플레이어와 겹칠지(캐릭터 컨트롤러 사이즈 범위 내 존재) 검사
    /// </summary>
    /// <param name="targetCenter">목표 위치 중심</param>
    bool IsMonsterInsideAt(Vector3 targetCenterPos)
    {
        // 캐릭터 컨트롤러 사이즈와 똑같게 범위 설정
        float radius = _controller.radius;
        float height = _controller.height;
        Vector3 bottom = targetCenterPos + Vector3.up * radius;
        Vector3 top = targetCenterPos + Vector3.up * (height - radius);

        // 검사
        return Physics.CheckCapsule(
            bottom,
            top,
            radius,
            SubmarineInGameManager.instance.MonsterLayer,
            QueryTriggerInteraction.Collide
        );
    }

    /// <summary>
    /// 사다리에서 목표 위치로 안전 이동
    /// </summary>
    /// <param name="targetPos">목표 위치</param>
    /// <param name="isStart">사다리 타기 시작인지 여부</param>
    IEnumerator MoveToTargetSafe(Vector3 targetPos, bool isStart)
    {
        // 초기 설정
        _isMovingToTargetSafe = true; // 안전 이동 중으로 설정
        transform.rotation = Quaternion.Euler(0f, -90f, 0f); // 사다리쪽 바라보게 하기
        Vector3 startPos = transform.position; // 시작 위치 저장

        // 안전 이동
        while (true)
        {
            // 목표 위치까지의 방향, 거리 계산
            Vector3 dir = targetPos - transform.position;
            float distance = dir.magnitude;

            // 도달하면 -> 이동 종료
            if (distance < 0.1f)
                break;

            // 다음 프레임에 갈 위치 예측
            Vector3 move = dir.normalized * 3f * Time.deltaTime;
            Vector3 nextPos = transform.position + move;

            // 다음 위치에 괴물이 있으면 -> 갈 수 없음 => 안전한 시작 위치로 롤백 후 아예 종료
            if (IsMonsterInsideAt(nextPos))
            {
                _isMovingToTargetSafe = false; // 안전 이동 중 아님으로 설정
                _controller.Move(startPos - transform.position); // 안전한 시작 위치로 롤백
                yield break; // 코루틴 종료
            }

            // 안전 -> 이동
            _controller.Move(move);

            yield return null;
        }

        // 도착
        _isMovingToTargetSafe = false; // 안전 이동 중 아님으로 설정

        // 이동 완료되었음 -> 실제 타기 시작 / 그만 타기 로직 실행
        if (isStart) // 타기 시작이었으면
            StartClimb(); // 사다리 타기 시작
        else // 그만 타기였으면
            ExitClimb(); // 그만 타기
    }

    /// <summary>
    /// 사다리 타기 시작
    /// </summary>
    private void StartClimb()
    {
        _ySpeed = 0f; // 추가: 중력 누적 초기화
        Debug.Log("사다리 타기 시작!");
        audioSource.Stop();
        _isClimbing = true; // 사다리 타는 중으로 설정
        _animator.SetBool("isClimbing", true);
        _animator.SetTrigger("ClimbStart");
    }

    /// <summary>
    /// 사다리 그만 타기
    /// </summary>
    private void ExitClimb()
    {
        _ySpeed = 0f; // 추가: 중력 누적 초기화
        audioSource.Stop();
        _isClimbing = false; // 사다리 타는 중 아님으로 설정
        _animator.SetBool("isClimbing", false);
        Debug.Log("사다리 그만 타기!");
    }

    /// <summary>
    /// 이동 가능 여부 설정
    /// </summary>
    public void SetMoveable(bool isMoveable, bool isApplingGravity = true)
    {
        _canMove = isMoveable;
        if (isMoveable)
            _isApplingGravity = true;
        else
            _isApplingGravity = isApplingGravity;

        // 이동 불가 경우
        if (!isMoveable)
        {
            // 애니메이션 가만히 있는 걸로 초기화
            moveSpeed = _walkSpeed * _playerStatus.SpeedScale;
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("isRunning", false);
            if (_isJumping) // 점프 중이면 -> 점프 하강
            {
                _isJumpingDown = true;
                _animator.SetBool("isJumpingDown", true);
            }

            // 소리 멈추기
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 플레이어 순간 이동
    /// </summary>
    /// <param name="position">목표 위치</param>
    /// <param name="rotation">목표 회전</param>
    public void PlayerTeleport(Vector3 position, Quaternion rotation)
    {
        _controller.enabled = false; // 플레이어 캐릭터 컨트롤러 잠시 끄기

        // 플레이어 위치 이동 & 회전
        transform.SetPositionAndRotation(position, rotation);

        _controller.enabled = true; // 플레이어 캐릭터 컨트롤러 다시 켜기
    }
}
