using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button endingFrameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button menuButton;

    // 생성된 C# 클래스 이름 (파일 이름과 동일)
    private PlayerInputActions playerInputActions;

    void Awake()
    {
        startButton.onClick.AddListener(GameManager.instance.StartGame);
        endingFrameButton.onClick.AddListener(GameManager.instance.EndingGallery);
        quitButton.onClick.AddListener(GameManager.instance.QuitGame);
        menuButton.onClick.AddListener(GameManager.instance.ToggleMenu);

        playerInputActions = new PlayerInputActions(); // 메모리에 인풋 시스템 인스턴스 생성
    }

    void OnEnable()
    {
        // 타이틀에서 사용할 액션 맵 활성화
        playerInputActions.Player.Enable();
    }

    void OnDisable()
    {
        // 스크립트가 비활성화될 때 인풋도 비활성화
        playerInputActions.Player.Disable();
        playerInputActions.Dispose(); // 연결된 모든 리소스 해제(메모리 청소)
    }

    void Start()
    {
        // 타이틀 화면이 시작되면 커서를 보이게 설정
        if (GameManager.instance != null)
        {
            GameManager.instance.SetCursorVisible(true);
        }
    }

    void Update()
    {
        // 입력 감지 (예: 아무 키나 눌러서 시작 또는 특정 액션)
        if (playerInputActions.Player.ToggleMenu.WasPressedThisFrame())
        {
            GameManager.instance.ToggleMenu();
        }
    }
}

