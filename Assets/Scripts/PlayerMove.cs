using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동(이동, 회전, 점프, 회피 등)
/// </summary>
public class PlayerMove : MonoBehaviour
{
    private CharacterController mController;
    private CapsuleCollider mCollider;
    private Animator mAnimator;
    private PlayerStat mPlayerStat;

    // 이동
    private Vector2 mMoveInput; // 이동 입력
    private Vector3 mMoveDir; // 이동 방향
    [SerializeField] private float mMoveSpeed; // 이동 속도
    private float mWalkSpeed = 3f; // 걷기 속도
    private float mRunSpeed = 7f; // 달리기 속도
    private float mDodgeSpeed = 5f; // 회피 속도

    // 회전
    private Vector2 mLookInput; // 시야 입력
    private float mMouseX; // 마우스 x좌표
    public float MouseX => mMouseX;
    private float mMouseY; // 마우스 y좌표
    public float MouseY => mMouseY;
    [SerializeField] float mMouseSensitivity = 1f; // 마우스 감도

    // 점프 & 중력
    private bool mJumpInput = false; // 점프 입력
    private float mJumpSpeed = 3f; // 점프 속도
    private float mYSpeed = 0f; // y 속도
    private float mGravity = -9.81f; // 중력

    // 회피
    private bool mIsDodging = false; // 회피 중인지 여부
    private float mCurrentDodgeTime = 0f; // 현재 회피 진행 시간(회피 시간으로 초기화돼서 0까지 감소)
    private float mDodgeTime = 1.5f; // 회피 시간

    void Awake()
    {
        mController = GetComponent<CharacterController>();
        mAnimator = GetComponent<Animator>();
        mPlayerStat = GetComponent<PlayerStat>();
    }

    void Start()
    {
        mMoveSpeed = mWalkSpeed; // 걷기 속도를 기본 속도로 설정
        Cursor.visible = false; // 마우스 커서 안 보이게 하기
        Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
    }

    /// <summary>
    /// W, A, S, D키 입력에 따라 이동 입력 값 업데이트
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        mMoveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 마우스(delta) 좌표 벡터 가져오기
    /// </summary>
    public void OnLook(InputAction.CallbackContext context)
    {
        mLookInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Left Shift키 입력되는 동안만 달리기 속도로 설정, 떼면 걷기 속도로 설정
    /// </summary>
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
            mMoveSpeed = mRunSpeed;
        else if (context.canceled)
            mMoveSpeed = mWalkSpeed;
    }

    /// <summary>
    /// Space키가 입력되고, 컨트롤러가 바닥에 닿아있을 때 점프 입력값 true로 업데이트
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && mController.isGrounded)
        {
            mJumpInput = true;
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
                // 정지 해제(플레이)
                GameManager.instance.IsPausing = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
                Time.timeScale = 1.0f; // 시간 흐르게
            }
            else // 플레이 중이면
            {
                // 정지
                GameManager.instance.IsPausing = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
                Time.timeScale = 0f; // 시간 정지
            }
        }
    }

    /// <summary>
    /// Left Control키가 입력되고 스태미나가 정상적으로 사용되면 회피 시작
    /// </summary>
    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed && mPlayerStat.UseStamina()) // 스태미나 사용했을 때
        {
            mIsDodging = true; // 회피 중 true
            mCurrentDodgeTime = mDodgeTime; // 현재 회피 진행 시간을 회피 시간으로 초기화
            mAnimator.SetTrigger("Dodge"); // 회피 애니메이션 재생
        }
    }

    /// <summary>
    /// 정지 중이 아닐 때 회전, 회피/이동
    /// </summary>
    void Update()
    {
        if (!GameManager.instance.IsPausing) // 정지 중이 아닐 때
        {
            Rotate(); // 회전

            if (mIsDodging) // 회피 중이면
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
        mMouseX = mLookInput.x * mMouseSensitivity;
        mMouseY = mLookInput.y * mMouseSensitivity;

        // 플레이어 좌우 회전
        transform.Rotate(Vector3.up * MouseX);
    }

    /// <summary>
    /// 이동, 점프(중력 적용)
    /// </summary>
    void Move()
    {
        // 이동 방향
        mMoveDir = transform.right * mMoveInput.x + transform.forward * mMoveInput.y;
        mMoveDir.Normalize(); // 정규화

        // 애니메이션 파라미터 설정
        mAnimator.SetFloat("Speed", mMoveDir.magnitude); // Idle or 이동(달리기 & 걷기)
        mAnimator.SetBool("isRunning", mMoveSpeed == mRunSpeed); // 달리기 애니메이션

        // 중력 적용
        if (mController.isGrounded) // 바닥에 닿아있으면
        {
            if (mYSpeed < 0f)
                mYSpeed = -0.8f; // 바닥에 붙도록 작은 값만큼 y 속도를 아래로 줌

            // 점프
            if (mJumpInput) // 점프 입력이 들어왔을 때
            {
                mYSpeed = mJumpSpeed; // y 속도를 점프 속도로 초기화
                mAnimator.SetTrigger("Jump"); // 점프 애니메이션 재생
                mJumpInput = false; // 점프 입력을 false로 설정(중복 실행 안되도록)
            }
        }
        else // 공중
        {
            mYSpeed += mGravity * Time.deltaTime; // 중력에 따른 y 속도 계산
        }

        // 이동 + 점프
        Vector3 velocity = mMoveDir * mMoveSpeed + Vector3.up * mYSpeed;
        mController.Move(velocity * Time.deltaTime);

        // 바닥에 닿아있는지 여부 애니메이터에 넘기기(모든 y 계산이 다 끝난 뒤에 실행)
        mAnimator.SetBool("isGrounded", mController.isGrounded);
    }

    /// <summary>
    /// 회피(중력 적용)
    /// </summary>
    void Dodge()
    {
        // 중력 적용
        if (mController.isGrounded) // 바닥에 닿아있으면
        {
            if (mYSpeed < 0f)
                mYSpeed = -0.8f; // 바닥에 붙도록 작은 값만큼 y 속도를 아래로 줌
        }
        else // 공중
        {
            mYSpeed += mGravity * Time.deltaTime; // 중력에 따른 y 속도 계산
        }

        Vector3 velocity = mMoveDir * mDodgeSpeed + Vector3.up * mYSpeed;
        mController.Move(velocity * Time.deltaTime);

        mCurrentDodgeTime -= Time.deltaTime;

        if (mCurrentDodgeTime <= 0)
        {
            mCurrentDodgeTime = 0f;
            mIsDodging = false;
        }
    }
}
