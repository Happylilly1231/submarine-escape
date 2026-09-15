using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Input System 네임스페이스

public class PanicCrosshair : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject crosshairTotalPartUI;
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Image targetZoneImage;
    [SerializeField] private GameObject fireMarkPart;

    [Header("색상 설정")]
    [SerializeField] private Color normalColor = new Color(1f, 114f / 255f, 57f / 255f, 5f / 255f);
    [SerializeField] private Color activeColor = new Color(1f, 114f / 255f, 57f / 255f, 25f / 255f);
    [SerializeField] private Color fireAvailableColor = new Color(1f, 114f / 255f, 57f / 255f, 25f / 255f);
    [SerializeField] private Color crosshairDefaultColor = new Color(1f, 114f / 255f, 57f / 255f, 25f / 255f);
    [SerializeField] private Color crosshairActiveColor = new Color(1f, 114f / 255f, 57f / 255f, 25f / 255f);

    [Header("조작 및 흔들림 수치")]
    [SerializeField] private float noiseSpeed = 3.5f;
    [SerializeField] private float noiseAmount = 350f;
    [SerializeField] private float controlSpeed = 450f;
    [SerializeField] private float targetRadius = 70f;
    [SerializeField] private float maxRadius = 350f;

    // 인풋 관련
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _fireAction;

    private Vector2 currentOffset;
    private float seedX;
    private float seedY;
    private bool isActive = false;

    public bool IsInTargetZone { get; private set; }
    public bool CanFire { get; private set; } = false;

    // 이벤트
    public event Action<bool> OnFireTriggered;

    private void Awake()
    {
        // 인풋 가져오기
        _playerInput = FindAnyObjectByType<PlayerInput>();
        _moveAction = _playerInput.actions.FindAction("DeepSea_Move");
        _fireAction = _playerInput.actions.FindAction("DeepSea/Ascend");

        // 랜덤 시드
        seedX = UnityEngine.Random.Range(0f, 100f);
        seedY = UnityEngine.Random.Range(100f, 200f);

        // fireMarkPart.SetActive(false);
        crosshairTotalPartUI.SetActive(true);
    }

    public void ToggleCrosshair(bool enable)
    {
        isActive = enable;
        crosshairTotalPartUI.SetActive(isActive);

        if (enable)
        {
            currentOffset = UnityEngine.Random.insideUnitCircle.normalized * (targetRadius + 100f);

            // Input Action 이벤트 등록 및 활성화
            _fireAction.performed += OnFirePerformed;

            crosshairImage.color = crosshairActiveColor;
        }
        else
        {
            // 이벤트 해제 및 비활성화
            _fireAction.performed -= OnFirePerformed;

            // if (fireMarkPart.activeSelf)
            // {
            //     fireMarkPart.SetActive(false);
            // }
            targetZoneImage.color = normalColor;
            crosshairImage.color = crosshairDefaultColor;
        }
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (!isActive || !CanFire) return;

        // Fire 입력이 들어왔을 때, 현재 조준선이 중앙에 있는지 여부를 담아 이벤트 발송
        OnFireTriggered?.Invoke(IsInTargetZone);
    }

    private void Update()
    {
        if (!isActive) return;

        // 1. 손떨림 펄린 노이즈
        float nX = (Mathf.PerlinNoise(Time.unscaledTime * noiseSpeed, seedX) - 0.5f) * 2f;
        float nY = (Mathf.PerlinNoise(Time.unscaledTime * noiseSpeed, seedY) - 0.5f) * 2f;
        Vector2 noiseDir = new Vector2(nX, nY) * noiseAmount * Time.unscaledDeltaTime;

        // 2. Input System에서 WASD Vector2 값 읽기
        Vector2 inputDir = _moveAction.ReadValue<Vector2>() * controlSpeed * Time.unscaledDeltaTime;

        // 3. 위치 계산 및 범위 제한
        currentOffset += noiseDir + inputDir;
        currentOffset = Vector2.ClampMagnitude(currentOffset, maxRadius);
        crosshairUI.anchoredPosition = currentOffset;

        // 4. 중앙 영역 진입 체크
        IsInTargetZone = currentOffset.magnitude <= targetRadius;
        Debug.Log(IsInTargetZone);
        if (IsInTargetZone)
        {
            if (CanFire)
            {
                // if (!fireMarkPart.activeSelf)
                // {
                //     fireMarkPart.SetActive(true);
                // }
                targetZoneImage.color = fireAvailableColor;
                crosshairImage.color = Color.green;
            }
            else
            {
                // if (fireMarkPart.activeSelf)
                // {
                //     fireMarkPart.SetActive(false);
                // }
                targetZoneImage.color = activeColor;
                crosshairImage.color = crosshairActiveColor;
            }
        }
        else
        {
            // if (fireMarkPart.activeSelf)
            // {
            //     fireMarkPart.SetActive(false);
            // }
            targetZoneImage.color = normalColor;
            crosshairImage.color = crosshairActiveColor;
        }
    }

    /// <summary>
    /// 발사 가능 여부 설정
    /// </summary>
    /// <param name="isAvailable"></param>
    public void SetFireAvailable(bool isAvailable)
    {
        CanFire = isAvailable;
    }
}