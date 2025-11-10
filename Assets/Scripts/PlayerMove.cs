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
    [SerializeField] float moveSpeed = 2f; // 이동 속도
    float walkSpeed = 2f; // 걷기 속도

    // 시야 회전
    Vector2 lookInput; // 시야 입력
    [SerializeField] Transform cameraTransform; // 1인칭 카메라의 Transform
    [SerializeField] float mouseSensitivity = 1f; // 마우스 감도
    float xRotation = 0f; // 카메라 상하 회전 제한용

    // 달리기
    bool isRunning; // 달리기 중인지 여부
    float runSpeed = 5f; // 달리기 속도


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        moveSpeed = walkSpeed;
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

    void Update()
    {
        Rotate(); // 회전
    }

    void FixedUpdate()
    {
        Move(); // 이동
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
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 시야 상하 회전 범위 제한
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
}
