using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // TextMeshPro를 사용 중이라면 유지, 일반 UI Text면 Text로 변경

public class HeliCoordDevice : MonoBehaviour
{
    [Header("UI Text Displays (2개만 할당)")]
    [SerializeField] private TextMeshProUGUI textX; // 또는 public Text textX;
    [SerializeField] private TextMeshProUGUI textY; // 또는 public Text textY;

    [Header("UI Buttons (순서 상관없이 직관적 연결)")]
    [SerializeField] private Button btnXTenUp;
    [SerializeField] private Button btnXTenDown;
    [SerializeField] private Button btnXOneUp;
    [SerializeField] private Button btnXOneDown;

    [SerializeField] private Button btnYTenUp;
    [SerializeField] private Button btnYTenDown;
    [SerializeField] private Button btnYOneUp;
    [SerializeField] private Button btnYOneDown;

    private InputAction _toggleMenuAction;
    private MenuUIController _menuUIController;
    private HeliCoordDeviceController _heliCoordDeviceController;

    private bool isOpen = false;

    private void Start()
    {
        _toggleMenuAction = PlayerManager.Instance.playerInput.actions["ToggleMenu"];

        _menuUIController = FindObjectOfType<MenuUIController>();
        _heliCoordDeviceController = FindObjectOfType<HeliCoordDeviceController>();

        SetupButtonEvents();
        UpdateUI();
    }

    private void SetupButtonEvents()
    {
        // X 좌표 버튼 이벤트 (+10, -10, +1, -1)
        btnXTenUp.onClick.AddListener(() => ChangeX(10));
        btnXTenDown.onClick.AddListener(() => ChangeX(-10));
        btnXOneUp.onClick.AddListener(() => ChangeX(1));
        btnXOneDown.onClick.AddListener(() => ChangeX(-1));

        // Y 좌표 버튼 이벤트 (+10, -10, +1, -1)
        btnYTenUp.onClick.AddListener(() => ChangeY(10));
        btnYTenDown.onClick.AddListener(() => ChangeY(-10));
        btnYOneUp.onClick.AddListener(() => ChangeY(1));
        btnYOneDown.onClick.AddListener(() => ChangeY(-1));
    }

    private void ChangeX(int amount)
    {
        _heliCoordDeviceController.ArrivalX = CalculateValue(_heliCoordDeviceController.ArrivalX, amount);
        UpdateUI();
    }

    private void ChangeY(int amount)
    {
        _heliCoordDeviceController.ArrivalY = CalculateValue(_heliCoordDeviceController.ArrivalY, amount);
        UpdateUI();
    }

    /// <summary>
    /// 십의 자리/일의 자리 연산 및 0~99 범위 순환 로직
    /// </summary>
    private int CalculateValue(int currentValue, int amount)
    {
        int ten = currentValue / 10;
        int one = currentValue % 10;

        if (amount == 10) ten = (ten + 1) % 10;         // 십의 자리 Up (9 -> 0)
        else if (amount == -10) ten = (ten + 9) % 10;   // 십의 자리 Down (0 -> 9)
        else if (amount == 1) one = (one + 1) % 10;     // 일의 자리 Up (9 -> 0)
        else if (amount == -1) one = (one + 9) % 10;   // 일의 자리 Down (0 -> 9)

        return (ten * 10) + one;
    }

    /// <summary>
    /// 2자리 수 전용 포맷팅 ("00", "05", "12")
    /// </summary>
    private void UpdateUI()
    {
        if (textX != null) textX.text = _heliCoordDeviceController.ArrivalX.ToString("D2"); // D2: 항상 2자리로 표현
        if (textY != null) textY.text = _heliCoordDeviceController.ArrivalY.ToString("D2");
    }

    public void ViewDevice()
    {
        if (isOpen) return;
        isOpen = true;

        SubmarineInGameManager.instance.SetActiveInGameUI(false);

        transform.localPosition = new Vector3(0f, 0f, 0.18f);
        transform.localRotation = Quaternion.Euler(-80f, 0f, 0f);

        // ESC키 입력 액션 변경
        _toggleMenuAction.performed += OnToggleHeliCoord;

        FocusManager.Instance.PushFocusState(GameFocusState.Diary); // 임시로 다이어리 포커스 같이 씀
        UpdateUI();
    }

    public void CloseDevice()
    {
        if (!isOpen) return;
        isOpen = false;

        SubmarineInGameManager.instance.SetActiveInGameUI(true);

        transform.localPosition = new Vector3(0.2f, -0.09f, 0.3f);
        transform.localRotation = Quaternion.Euler(-70f, 0f, 40f);

        _toggleMenuAction.performed -= OnToggleHeliCoord;

        // 이전 포커스 상태로 복구
        FocusManager.Instance.PopFocusState();

        _toggleMenuAction.performed += _menuUIController.OnToggleMenu;
    }

    private void OnToggleHeliCoord(InputAction.CallbackContext context)
    {
        CloseDevice();
    }

    private void OnDisable()
    {
        _toggleMenuAction.performed -= OnToggleHeliCoord;
    }
}