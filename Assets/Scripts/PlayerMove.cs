using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    CharacterController controller;
    CapsuleCollider col;
    Animator anim;

    // 이동
    Vector2 moveInput; // 이동 입력
    [SerializeField] float moveSpeed; // 이동 속도
    float walkSpeed = 3f; // 걷기 속도
    float runSpeed = 7f; // 달리기 속도

    // 시야 회전
    Vector2 lookInput; // 시야 입력
    public float mouseX;
    public float mouseY;
    [SerializeField] float mouseSensitivity = 1f; // 마우스 감도

    // 점프 & 중력
    bool jumpInput = false; // 점프 입력
    float jumpSpeed = 3f; // 점프 속도
    float ySpeed = 0f; // y 속도
    float gravity = -9.81f; // 중력

    // 정지
    public bool isPausing = false; // 정지 중인지 여부

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        moveSpeed = walkSpeed;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
            moveSpeed = runSpeed;
        else if (context.canceled)
            moveSpeed = walkSpeed;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && controller.isGrounded)
        {
            jumpInput = true;
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        // 정지 버튼(ESC) 눌렀을 때
        if (context.performed)
        {
            if (isPausing) // 정지 중
            {
                // 정지 해제
                isPausing = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
                Time.timeScale = 1.0f; // 시간 흐르게
            }
            else // 플레이 중
            {
                // 정지
                isPausing = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
                Time.timeScale = 0f; // 시간 정지
            }
        }
    }

    void Update()
    {
        if (!isPausing)
        {
            Rotate(); // 회전
            Move(); // 이동
        }
    }

    // 회전
    void Rotate()
    {
        // 마우스 입력
        mouseX = lookInput.x * mouseSensitivity;
        mouseY = lookInput.y * mouseSensitivity;

        // 플레이어 좌우 회전
        transform.Rotate(Vector3.up * mouseX);
    }

    // 이동
    void Move()
    {
        // 이동 방향
        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        moveDir.Normalize(); // 정규화

        // 애니메이션 파라미터 설정
        anim.SetFloat("Speed", moveDir.magnitude); // Idle or 이동(달리기 & 걷기)
        anim.SetBool("isRunning", moveSpeed == runSpeed); // 달리기 애니메이션

        // 중력 적용
        if (controller.isGrounded) // 바닥에 닿아있으면
        {
            if (ySpeed < 0f)
                ySpeed = -0.8f; // 바닥에 붙도록 작은 값만큼 y 속도를 아래로 줌

            // 점프
            if (jumpInput) // 점프 입력이 들어왔을 때
            {
                ySpeed = jumpSpeed; // y 속도를 점프 속도로 초기화
                anim.SetTrigger("Jump"); // 점프 애니메이션 재생
                jumpInput = false; // 점프 입력을 false로 설정(중복 실행 안되도록)
            }
        }
        else // 공중
        {
            ySpeed += gravity * Time.deltaTime; // 중력에 따른 y 속도 계산
        }

        // 이동 + 점프
        Vector3 velocity = moveDir * moveSpeed + Vector3.up * ySpeed;
        controller.Move(velocity * Time.deltaTime);

        // 바닥에 닿아있는지 여부 애니메이터에 넘기기(모든 y 계산이 다 끝난 뒤에 실행)
        anim.SetBool("isGrounded", controller.isGrounded);
    }
}
