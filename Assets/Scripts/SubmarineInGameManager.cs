using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 잠수함 씬의 인게임 매니저
/// <para>- 경보 발생/해제</para>
/// <para>- 파괴될 장비 배열 저장</para>
/// <para>- 잠수함 씬에서 공통적으로 접근하는 변수들(플레이어, 괴물 등)을 모아두고 관리한다.</para>
/// <para>- UI에 포커스 여부 설정 함수</para>
/// </summary>
public class SubmarineInGameManager : MonoBehaviour
{
    // 정지
    private bool _isPausing = false; // 정지 중 여부
    public bool IsPausing { get => _isPausing; set => _isPausing = value; }
    // private bool _haveToShowCursor = false;
    // public bool HaveToShowCursor { get => _haveToShowCursor; set => _haveToShowCursor = value; } // 커서가 현재 보여야 하는지 여부(true일 때는 Resume(재시작)을 해도 커서를 숨기지 않음)
    private bool _isActionMapActiveBeforePause = true; // 정지 전 액션 맵 활성화 여부
    private bool _isMapOpened = false;
    public bool IsMapOpened { get => _isMapOpened; set => _isMapOpened = value; }

    // 경보
    [SerializeField] private Button alertButton; // 임시 - 경보 버튼
    private bool _isAlerting = false; // 경보 발생 중 여부
    public bool IsAlerting => _isAlerting;
    [SerializeField] private Transform escapeRoomPos; // 탈출실 위치
    public bool hasEverOpenedEscapeDoor = false; // 탈출실 문이 한 번이라도 열렸는지 여부

    // 파괴될 장비
    [SerializeField] private GameObject[] destroyEquipments; // 파괴되는 장비 배열
    public int currentDestroyEquipmentIndex = 0; // 현재 파괴될 장비 인덱스
    private GameObject _currentDestroyEquipment; // 현재 파괴될 장비
    public GameObject CurrentDestroyEquipment => _currentDestroyEquipment;
    private Transform _currentTargetPos; // 현재 목표 위치(탈출실 / 장비의 파괴 위치)
    public Transform CurrentTargetPos => _currentTargetPos;

    // 문
    private Door[] _doors;
    public Door[] Doors { get => _doors; set => _doors = value; }

    // 플레이어
    public GameObject player;
    public PlayerInput playerInput;
    private GameObject playerGeo;
    private PlayerInteractor _playerInteractor;
    private PlayerCameraController _playerCameraController;
    public PlayerMove playerMove { get; private set; }

    // 내부 괴물
    public Transform innerMonsterTransform;
    private LayerMask monsterLayer;
    public LayerMask MonsterLayer => monsterLayer;

    // 심해 괴물
    [SerializeField] private DeepSeaMonsterController deepSeaMonsterController;
    public DeepSeaMonsterController DeepSeaMonsterController => deepSeaMonsterController;

    // 어뢰 발사 성공 여부(현재는 1, 2, 3차 다 가능)
    private bool _isFireSuccess = false;
    public bool IsFireSuccess { get => _isFireSuccess; set => _isFireSuccess = value; }

    // 현재 퍼즐
    public PuzzleController CurrentPuzzleController { get; private set; } = null;

    // 포커스
    private int _focusRequestCount = 0; // 포커스 요청 횟수 카운트
    private bool _isCurrentlyFocused = false; // 현재 포커스 상태

    // 이벤트
    public event Action OnAlertStarted; // 경보 발생 시작 이벤트

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip alertSound;
    private AudioSource _audioSource;

    // 인벤토리 & 아이템 관련
    [SerializeField] private InventoryManager inventoryManager;
    public InventoryManager InventoryManager => inventoryManager;
    [SerializeField] private ItemEquipController itemEquipController;
    public ItemEquipController ItemEquipController => itemEquipController;

    // 싱글톤 변수
    public static SubmarineInGameManager instance;

    /// <summary>
    /// 싱글톤 구현
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            _audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 문 가져오기(후에 doorManager를 추가해 옮길 수 있음)
        _doors = FindObjectsOfType<Door>();

        // 내부 괴물 레이어 가져오기
        monsterLayer = LayerMask.GetMask("Monster");

        // 플레이어 관련 필요한 것 가져오기
        playerGeo = player.transform.GetChild(0).gameObject; // 플레이어 Geo(외형) 가져오기 (Player의 첫번째 자식)
        playerInput = player.GetComponent<PlayerInput>(); // 플레이어 입력 컴포넌트 가져오기
        _playerInteractor = player.GetComponent<PlayerInteractor>(); // 플레이어 인터랙터 컴포넌트 가져오기
        _playerCameraController = Camera.main.GetComponent<PlayerCameraController>(); // 플레이어 카메라 컨트롤러 컴포넌트 가져오기
        playerMove = player.GetComponent<PlayerMove>(); // 플레이어 이동 컴포넌트 가져오기

        InitGame();
    }

    private void OnDisable()
    {
        // AudioManager.Instance.StopBGM();
    }

    /// <summary>
    /// Escape키 입력에 따라 메뉴 열기/열기 해제
    /// </summary>
    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        // 정지 버튼(ESC) 눌렀을 때
        if (context.performed)
        {
            ToggleMenuAndSetPause();
        }
    }

    public void ToggleMenuAndSetPause()
    {
        GameManager.instance.ToggleMenu();
        if (_isPausing) // 정지 중이면
        {
            _playerInteractor.SetActiveInteractorUI(true); // 상호작용 UI 켜기
            Resume(); // 정지 해제(플레이)
        }
        else // 플레이 중이면
        {
            _playerInteractor.SetActiveInteractorUI(false); // 상호작용 UI 끄기
            Pause(); // 정지
        }
    }

    /// <summary>
    /// 게임 초기 설정
    /// </summary>
    private void InitGame()
    {
        GameManager.instance.SetHaveToShowCursor(false); // 커서 보여야 하지 않음으로 설정

        // AudioManager.Instance.PlayBGM(AudioManager.Instance.fanSound);

        Resume(); // 재시작
    }

    /// <summary>
    /// 게임 정지
    /// </summary>
    public void Pause()
    {
        Debug.Log("정지");
        _isPausing = true; // 정지 중으로 설정
        _isActionMapActiveBeforePause = playerInput.currentActionMap.enabled;
        playerInput.currentActionMap.Disable(); // 플레이어 상호작용 아예 막기
        playerInput.actions["ToggleMenu"].Enable();
        GameManager.instance.SetCursorVisible(true); // 커서 보이기
        Time.timeScale = 0f; // 시간 정지
        AudioListener.pause = true; // 오디오 듣기 정지
    }

    public void IntroPause()
    {
        // 인트로 중에는 시간 정지는 아니고 플레이어의 상호작용만 막는 상태
        Debug.Log("인트로 시퀀스 시작");
        _isPausing = false;
        playerInput.currentActionMap.Disable(); // 플레이어 상호작용 아예 막기
        GameManager.instance.SetCursorVisible(false); // 커서 보이기
        AudioListener.pause = false; // 오디오 듣기 정지 해제
    }

    /// <summary>
    /// 게임 정지 해제
    /// </summary>
    public void Resume()
    {
        if (_isMapOpened)
        {
            Debug.Log("맵 켜져 있는 상태");
            playerInput.actions["ToggleMap"].Enable(); // 맵 켜고 끄는 버튼만 활성화
            return;
        }

        Debug.Log("재시작");
        _isPausing = false; // 정지 중 아님으로 설정

        Time.timeScale = 1.0f; // 시간 정지 해제
        AudioListener.pause = false; // 오디오 듣기 정지 해제

        // 액션 맵 복구 로직
        playerInput.actions["ToggleMenu"].Disable();
        // 퍼즐 중이라면 Puzzle 맵 활성화
        if (CurrentPuzzleController != null) playerInput.SwitchCurrentActionMap("Puzzle");
        // 일반 상태라면 Player 맵 활성화
        else playerInput.SwitchCurrentActionMap("Player");
        // 어떤 상황이든 Permanent 맵은 항상 켜져 있어야 함
        playerInput.actions.FindActionMap("Permanent")?.Enable();

        // 커서 숨기기
        GameManager.instance.SetCursorVisible(false); // (커서 보여야 하면 안 숨김)
    }

    /// <summary>
    /// 경보 발생
    /// </summary>
    public void AlertOn()
    {
        if (hasEverOpenedEscapeDoor) // 탈출실 문이 한 번이라도 열린 경우(이후 어뢰실 장비에서 경보 발생해도 경보 발생 위치는 탈출실 위치로 설정됨(우선순위 더 높음))
        {
            _currentTargetPos = escapeRoomPos; // 현재 목표 위치 -> 탈출실 위치로 설정
        }
        else // 어뢰실 장비에서 경보 발생하는 경우
        {
            // 인덱스 범위 넘는 경우 예외 처리
            if (currentDestroyEquipmentIndex >= destroyEquipments.Length)
            {
                // AlertOff();
                return;
            }

            // 현재 파괴 정보 설정
            _currentDestroyEquipment = destroyEquipments[currentDestroyEquipmentIndex]; // 현재 파괴될 장비 설정
            _currentTargetPos = _currentDestroyEquipment.transform.GetChild(0); // 현재 목표 위치 -> 현재 파괴될 장비 위치의 파괴 위치(첫번째 자식)로 변경
        }

        // 임시 - 경보 버튼 빨간색으로 변경(후에 지워야 함)
        ColorBlock colorBlock = alertButton.colors;
        colorBlock.normalColor = Color.red;
        alertButton.colors = colorBlock;

        _isAlerting = true; // 경보 발생 중으로 설정
        AudioManager.Instance.PlaySoundSafe(_audioSource, alertSound);
        Debug.Log("경보 발생!");

        // 경보 발생 시작 이벤트 알림
        OnAlertStarted?.Invoke();
    }

    /// <summary>
    /// 경보 해제
    /// </summary>
    public void AlertOff()
    {
        _currentDestroyEquipment = null; // 현재 파괴될 장비 없으므로 null로 초기화
        _currentTargetPos = null; // 현재 목표 위치 null로 초기화
        _isAlerting = false; // 경보 발생 중 아님으로 설정
        Debug.Log("경보 해제");
        _audioSource.Stop();

        // 임시 - 경보 버튼 하얀색으로 변경(후에 지워야 함)
        ColorBlock colorBlock = alertButton.colors;
        colorBlock.normalColor = Color.white;
        alertButton.colors = colorBlock;
    }

    // 플레이어 모습 활성화 / 비활성화
    public void SetPlayerGeoActive(bool isActive)
    {
        playerGeo.SetActive(isActive);
    }

    public void SetCameraControllerEnable(bool isEnable)
    {
        _playerCameraController.enabled = isEnable;
    }

    /// <summary>
    /// 포커스 여부 설정
    /// <para>포커스</para>
    /// <para>- 게임 시간 정지</para>
    /// <para>- 카메라 조작 불가</para>
    /// <para>- 플레이어 이동 불가능</para>
    /// <para>- 퍼즐 진행 중이었다면 종료</para>
    /// </summary>
    /// <param name="isFocus">포커스 여부</param>
    public void SetFocus(bool isFocus)
    {
        // 카운트 업데이트
        if (isFocus) // 포커스 요청
            _focusRequestCount++; // 포커스 요청 카운트 1 증가
        else // 포커스 해제 요청
            _focusRequestCount = Mathf.Max(0, _focusRequestCount - 1); // 포커스 요청 카운트 1 감소

        // 목표 포커스 상태: 포커스 요청 카운트가 1 이상이면 포커스 / 0이면 포커스 해제
        bool targetFocusState = _focusRequestCount > 0;

        // 현재 포커스 상태와 목표 포커스 상태가 다를 때만 실제 수행
        if (targetFocusState != _isCurrentlyFocused)
        {
            _isCurrentlyFocused = targetFocusState; // 현재 상태 업데이트

            GameTime.Instance.SetPause(isFocus); // 포커스 -> 게임 시간 정지

            // 현재 퍼즐 진행 중이었다면 그 퍼즐 종료
            if (CurrentPuzzleController != null)
            {
                CurrentPuzzleController.ExitPuzzle();
                CurrentPuzzleController = null;
            }

            // 해제는 포커스와 반대로 작동
            _playerInteractor.SetActiveAimUI(!isFocus); // 포커스 -> 조준점 UI 끄기
            _playerCameraController.enabled = !isFocus; // 포커스 -> 카메라 조작 불가
            playerMove.SetMoveable(!isFocus); // 포커스 -> 플레이어 이동 불가능

            if (isFocus) // 포커스
            {
                // 상호작용 감지 텍스트 클리어
                _playerInteractor.ClearDetectionText();

                playerInput.DeactivateInput(); // 모든 액션 비활성화
            }
            else
            {
                playerInput.ActivateInput(); // 모든 액션 활성화
            }
        }
    }

    /// <summary>
    /// 퍼즐 포커스 여부 설정
    /// </summary>
    /// <param name="isFocus">포커스 여부</param>
    /// <param name="puzzleController">퍼즐 컨트롤러</param>
    public void SetPuzzleFocus(bool isFocus, PuzzleController puzzleController = null)
    {
        if (isFocus)
            CurrentPuzzleController = puzzleController;
        else
            CurrentPuzzleController = null;

        // 해제는 포커스와 반대로 작동
        _playerInteractor.SetActiveAimUI(!isFocus); // 포커스 -> 조준점 UI 끄기
        _playerInteractor.IsPuzzleActive = isFocus; // interactor의 퍼즐 상호작용 여부는 포커스 여부와 동일하게 설정
        _playerCameraController.enabled = !isFocus; // 포커스 -> 카메라 조작 불가
        playerMove.SetMoveable(!isFocus); // 포커스 -> 플레이어 이동 불가능

        if (isFocus) // 포커스
        {
            // 상호작용 감지 텍스트 클리어
            _playerInteractor.ClearDetectionText();
        }
        else // 포커스 해제
        {
            // 커서 보여야 한다고 되어 있었으면 -> 커서 보이지 않아도 됨으로 설정(퍼즐에서 플레이로 돌아가니까), 커서 숨기기
            if (GameManager.instance.HaveToShowCursor)
            {
                GameManager.instance.SetHaveToShowCursor(false);
                GameManager.instance.SetCursorVisible(false);
            }
        }
    }
}
