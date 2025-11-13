using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    Rigidbody rb;
    CapsuleCollider col;
    Animator anim;

    // 이동
    Vector2 moveInput; // 이동 입력
    [SerializeField] float moveSpeed; // 이동 속도
    float walkSpeed = 3f; // 걷기 속도

    // 시야 회전
    Vector2 lookInput; // 시야 입력
    [SerializeField] Transform cameraTransform; // 1인칭 카메라의 Transform
    [SerializeField] float mouseSensitivity = 1f; // 마우스 감도
    float xRotation = 0f; // 카메라 상하 회전 제한용

    // 달리기
    bool isRunning = false; // 달리기 중인지 여부
    float runSpeed = 7f; // 달리기 속도
    bool isGrounded = false; // 바닥에 닿아있는지 여부

    // 점프
    bool jumpInput = false; // 점프 입력
    bool isJumping = false; // 점프 중인지 여부
    float jumpForce = 3f; // 점프 힘

    // 정지
    bool isPausing = false; // 정지 중인지 여부

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        isRunning = context.performed;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumpInput = context.performed;
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
            Rotate(); // 회전
    }

    void FixedUpdate()
    {
        if (!isPausing)
        {
            Move(); // 이동
            Jump(); // 점프
        }
    }

    // 회전
    void Rotate()
    {
        // 마우스 입력
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // 플레이어 좌우 회전
        transform.Rotate(Vector3.up * mouseX);

        // 시야 상하 회전
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 55f); // 시야 상하 회전 범위 제한
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // 이동
    void Move()
    {
        // 이동 방향
        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        moveDir.Normalize(); // 정규화

        // 애니메이션 파라미터 설정
        anim.SetFloat("Speed", moveDir.magnitude); // Idle / 이동(달리기, 걷기)
        anim.SetBool("isRunning", isRunning); // 달리기 애니메이션

        // 이동
        if (moveDir.magnitude > 0.1f) // 이동 중일 때
        {
            // 속도 설정
            if (isRunning) // 달리기 상태
                moveSpeed = runSpeed; // 달리기 속도
            else // 걷기 상태
                moveSpeed = walkSpeed; // 걷기 속도

            // 이동
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // 점프
    void Jump()
    {
        // 바닥에 닿아있는지 검사
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.1f);

        anim.SetBool("isGrounded", isGrounded);

        // 바닥에 닿아있을 때
        if (isGrounded)
        {
            if (jumpInput && !isJumping) // 점프 입력을 받았고, 점프 중이 아니라면 -> 점프 가능
            {
                isJumping = true; // 점프 중으로 설정
                anim.SetTrigger("Jump"); // 점프 애니메이션
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 점프
            }
            else if (isJumping) // 바닥에 닿아있을 때 점프 중이라면 -> 점프 중 아님으로 설정
            {
                isJumping = false; // 점프 중 아님으로 설정
            }
        }
    }
}
