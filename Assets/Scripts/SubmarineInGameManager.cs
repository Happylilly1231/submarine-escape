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
    private bool _haveToShowCursor = false; // 커서가 현재 보여야 하는지 여부(true일 때는 Resume(재시작)을 해도 커서를 숨기지 않음)
    public bool HaveToShowCursor { get => _haveToShowCursor; set => _haveToShowCursor = value; }
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
    private GameObject playerGeo;
    private PlayerInteractor _playerInteractor;
    private PlayerInput _playerInput;

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

    // 이벤트
    public event Action OnAlertStarted; // 경보 발생 시작 이벤트

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip alertSound;
    private AudioSource _audioSource;

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

        // 플레이어 Geo(외형) 가져오기
        playerGeo = player.transform.GetChild(0).gameObject; // Player의 첫번째 자식

        // 플레이어 인터랙터 가져오기
        _playerInteractor = player.GetComponent<PlayerInteractor>();
        _playerInput = player.GetComponent<PlayerInput>();

        InitGame();
    }

    /// <summary>
    /// Escape키 입력에 따라 메뉴 열기/열기 해제
    /// </summary>
    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        // 정지 버튼(ESC) 눌렀을 때
        if (context.performed)
        {
            GameManager.instance.ToggleMenu();
            if (_isPausing) // 정지 중이면
            {
                Resume(); // 정지 해제(플레이)
            }
            else // 플레이 중이면
            {
                Pause(); // 정지
            }
        }
    }

    /// <summary>
    /// 게임 초기 설정
    /// </summary>
    private void InitGame()
    {
        Resume();
    }

    /// <summary>
    /// 게임 정지
    /// </summary>
    public void Pause()
    {
        Debug.Log("정지");
        _isPausing = true; // 정지 중으로 설정
        _playerInput.currentActionMap.Disable(); // 플레이어 상호작용 아예 막기
        _playerInput.actions["ToggleMenu"].Enable();
        GameManager.instance.SetCursorVisible(true); // 커서 보이기
        Time.timeScale = 0f; // 시간 정지
        AudioListener.pause = true; // 오디오 듣기 정지
    }

    /// <summary>
    /// 게임 정지 해제
    /// </summary>
    public void Resume()
    {
        if (_isMapOpened)
        {
            Debug.Log("맵 켜져 있는 상태");
            _playerInput.actions["ToggleMap"].Enable(); // 맵 켜고 끄는 버튼만 활성화
            return;
        }

        Debug.Log("재시작");
        _isPausing = false; // 정지 중 아님으로 설정

        if (!_haveToShowCursor) // 현재 커서가 보여야 하는 게 아니면(퍼즐 UI 등이 켜져 있는 게 아닐 때만)
        {
            _playerInput.currentActionMap.Enable(); // 상호작용 되도록 함
            GameManager.instance.SetCursorVisible(false); // 커서 숨기기
        }
        // _playerInput.actions["ToggleMap"].Enable(); // 맵 켜고 끄는 버튼은 활성화 필요(맵이 켜질 때는 커서가 보이는 상태라 다른 상호작용은 안되어도 끌 수는 있어야 하기 때문)
        Time.timeScale = 1.0f; // 시간 정지 해제
        AudioListener.pause = false; // 오디오 듣기 정지 해제
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

    // 플레이어 활성화 / 비활성화
    public void SetPlayerGeoActive(bool isActive)
    {
        playerGeo.SetActive(isActive);
    }

    /// <summary>
    /// UI에 포커스 여부 설정 - 플레이어 인터랙터의 퍼즐 푸는 중 여부, 플레이어 인풋 활성화 여부, 커서, 카메라, 플레이어 이동 조작
    /// </summary>
    /// <param name="isFocus">포커스 여부</param>
    public void SetFocusUI(bool isFocus)
    {
        if (isFocus)
        {
            _playerInteractor.IsPuzzleActive = true;
            _playerInteractor.ClearDetectionText();
            _playerInput.currentActionMap.Disable(); // 상호작용 아예 막기
            _playerInput.actions["ToggleMenu"].Enable(); // 정지 버튼은 활성화 필요
            _haveToShowCursor = true; // 커서 보여야 함으로 설정
            GameManager.instance.SetCursorVisible(true); // 커서 보이기
            Camera.main.GetComponent<PlayerCameraController>().enabled = false; // 카메라 조작 불가
            player.GetComponent<PlayerMove>().SetMoveable(false); // 플레이어 이동 불가능
        }
        else
        {
            _playerInteractor.IsPuzzleActive = false;
            _playerInput.currentActionMap.Enable(); // 상호작용 되도록 함
            _haveToShowCursor = false; // 커서 보여야 함 아님으로 설정
            GameManager.instance.SetCursorVisible(false); // 커서 숨기기
            Camera.main.GetComponent<PlayerCameraController>().enabled = true; // 카메라 조작 불가
            player.GetComponent<PlayerMove>().SetMoveable(true); // 플레이어 이동 불가능
        }
    }
}
