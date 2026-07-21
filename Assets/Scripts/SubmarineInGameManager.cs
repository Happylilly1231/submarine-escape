using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 경보 구역
/// </summary>
public enum AlertArea
{
    None,
    Galley, // 1
    Storage01, // 2
    EngineRoom, // 3
    ControlRoom, // 4
    TorpedoRoom, // 5
    EscapeRoom,
    MachinerySpace
}

[Serializable]
public class AlertAreaInfo
{
    public AlertArea alertArea; // 경보 구역
    public GameObject[] destroyEquipments; // 파괴 장치 배열
    public List<Transform> destroyPosTransforms = new List<Transform>(); // 파괴 위치 배열 (탈출실, 기계실 - 파괴할 장치는 없지만, 임의로 탈출실 안, 기계실 안을 파괴 위치로 설정)
    public List<Transform> sequenceCameraPosList = new List<Transform>();
}

/// <summary>
/// 잠수함 씬의 인게임 매니저
/// <para>- 경보 발생/해제</para>
/// <para>- 파괴될 장비 배열 저장</para>
/// <para>- 잠수함 씬에서 공통적으로 접근하는 변수들(플레이어, 괴물 등)을 모아두고 관리한다.</para>
/// <para>- UI에 포커스 여부 설정 함수</para>
/// </summary>
public class SubmarineInGameManager : MonoBehaviour
{
    // 경보
    // [SerializeField] private Button alertButton; // 임시 - 경보 버튼
    private bool _isAlerting = false; // 경보 발생 중 여부
    public bool IsAlerting => _isAlerting;
    // [SerializeField] private Transform escapeRoomPos; // 탈출실 위치
    // public bool hasEverOpenedEscapeDoor = false; // 탈출실 문이 한 번이라도 열렸는지 여부

    // // 파괴될 장비
    // [SerializeField] private GameObject[] destroyEquipments; // 파괴되는 장비 배열
    // public int currentDestroyEquipmentIndex = 0; // 현재 파괴될 장비 인덱스
    // private GameObject _currentDestroyEquipment; // 현재 파괴될 장비
    // public GameObject CurrentDestroyEquipment => _currentDestroyEquipment;
    // private Transform _currentTargetPos; // 현재 목표 위치(탈출실 / 장비의 파괴 위치)
    // public Transform CurrentTargetPos => _currentTargetPos;

    // 문
    private Door[] _doors;
    public Door[] Doors { get => _doors; set => _doors = value; }

    // 플레이어
    public GameObject player;
    public PlayerInput playerInput;
    public PlayerInteractor playerInteractor { get; private set; }
    public PlayerCameraController playerCameraController { get; private set; }
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

    // 이벤트
    public event Action OnAlertStarted; // 경보 발생 시작 이벤트
    public event Action OnAlertEnded; // 경보 발생 종료 이벤트
    public event Action OnMachinerySpaceAlertStarted; // 기계실 경보 발생 시작 이벤트
    public event Action OnNonMachinerySpaceAlertStarted; // 기계실 경보 제외 경보 발생 시작 이벤트

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip alertSound;
    private AudioSource _audioSource;

    // 인벤토리 & 아이템 관련
    [SerializeField] private InventoryManager inventoryManager;
    public InventoryManager InventoryManager => inventoryManager;
    [SerializeField] private ItemEquipController itemEquipController;
    public ItemEquipController ItemEquipController => itemEquipController;

    // UI
    [SerializeField] private GameObject statUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject interactorUI;
    public GameObject InteractorUI => interactorUI;
    public bool isInGameMenuActive = false; // 플레이어가 인게임 메뉴를 보고 있는 상태

    // 경보
    [SerializeField] private List<AlertAreaInfo> alertAreaInfoList = new List<AlertAreaInfo>();
    public Dictionary<AlertArea, AlertAreaInfo> AlertAreaInfoDict { get; private set; } = new Dictionary<AlertArea, AlertAreaInfo>();
    public AlertArea CurrentAlertArea { get; private set; } = AlertArea.None; // 현재 경보 발생 구역
    public int CurrentDestroyEquipmentIndex { get; private set; } = -1; // 현재 경보 발생 구역에서 괴물이 파괴할 장치 인덱스 (-1: 없음)
    public GameObject CurrentDestroyEquipmentObj { get; private set; } = null; // 현재 경보 발생 구역에서 괴물이 파괴할 장치 오브젝트
    public Transform CurrentDestroyPos { get; private set; } = null;
    public Transform CurrentSequenceCameraPos { get; private set; } = null;
    public MachinerySpaceDoorRepairController machinerySpaceDoorRepairController;


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

            foreach (var info in alertAreaInfoList)
            {
                if (info.alertArea == AlertArea.None) continue;

                for (int i = 0; i < info.destroyEquipments.Length; i++)
                {
                    info.destroyPosTransforms.Add(info.destroyEquipments[i].transform.GetChild(0));
                }

                if (!AlertAreaInfoDict.ContainsKey(info.alertArea))
                {
                    AlertAreaInfoDict.Add(info.alertArea, info);
                }
                else
                {
                    Debug.LogWarning($"[AlertManager] 중복된 구역 설정이 있습니다: {info.alertArea}");
                }
            }
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
        playerInput = player.GetComponent<PlayerInput>(); // 플레이어 입력 컴포넌트 가져오기
        playerInteractor = player.GetComponent<PlayerInteractor>(); // 플레이어 인터랙터 컴포넌트 가져오기
        playerCameraController = Camera.main.GetComponent<PlayerCameraController>(); // 플레이어 카메라 컨트롤러 컴포넌트 가져오기
        playerMove = player.GetComponent<PlayerMove>(); // 플레이어 이동 컴포넌트 가져오기

        // InitGame();
    }

    private void OnDisable()
    {
        // AudioManager.Instance.StopBGM();
    }

    // Ctrl + F1 디버깅 탭 토글(ESC로 메뉴를 연 상태에서만 사용 가능)
    public void OnToggleDebug(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!MenuUIController.instance.MenuUI.activeSelf) return; // 메뉴가 열려있지 않을 때는 디버깅 UI 활성화/비활성화 불가능

            MenuUIController.instance.ToggleDebuggingUI(); // 디버깅 UI 활성화/비활성화
        }
    }

    /// <summary>
    /// 게임 초기 설정
    /// </summary>
    public void InitGame()
    {
        // 게임 시작 시 None 상태의 UI, 커서, 인풋 설정을 실행 - 타이틀 씬에서 커서 안 보이게 설정
        FocusManager.Instance.ResetFocusState(GameFocusState.None);
    }

    // public void IntroPause()
    // {
    //     // 인트로 중에는 시간 정지는 아니고 플레이어의 상호작용만 막는 상태
    //     Debug.Log("인트로 시퀀스 시작");
    //     // InputManager.instance.DisableAllInputs(); // 모든 입력 비활성화
    //     // _isPausing = false;
    //     // playerInput.currentActionMap.Disable(); // 플레이어 상호작용 아예 막기
    //     // GameManager.instance.SetCursorVisible(false); // 커서 보이지 않게 하기
    //     // AudioListener.pause = false; // 오디오 듣기 정지 해제
    // }

    public AlertAreaInfo GetAlertAreaInfo(AlertArea alertArea)
    {
        if (AlertAreaInfoDict.TryGetValue(alertArea, out AlertAreaInfo info))
        {
            return info;
        }

        Debug.LogError($"[AlertManager] {alertArea} 구역 정보를 찾을 수 없습니다!");
        return null;
    }

    /// <summary>
    /// 경보 발생
    /// </summary>
    /// <param name="alertArea">구역</param>
    /// <param name="destroyEquimentIndex">파괴될 장치 인덱스(따로 없으면 -1)</param>
    public void AlertOn(AlertArea alertArea, int destroyEquimentIndex = -1)
    {
        // 탈출실 경보가 발생 중이라면 -> 다른 경보는 전부 무효 처리 (어차피 괴물이 탈출실 안으로 들어갈 때 게임 오버됨)
        if (CurrentAlertArea == AlertArea.EscapeRoom)
            return;

        // AlertArea prevAlertArea = CurrentAlertArea;
        // int prevDestroyEquipmentIndex = CurrentDestroyEquipmentIndex;
        // GameObject prevDestroyEquipmentObj = CurrentDestroyEquipmentObj;
        // Transform prevDestroyPos = CurrentDestroyPos;
        // Transform prevSequenceCameraPos = CurrentSequenceCameraPos;

        // 경보 발생 중이었을 때 (탈출실 제외)
        if (_isAlerting)
        {
            // 경보 발생 끝 이벤트 알림
            OnAlertEnded?.Invoke();
        }

        _isAlerting = true; // 경보 발생 중으로 설정

        // 다른 경보 발생 중에 기계실 경보가 울리는 게 아니라면 -> 현재 경보 발생 위치 변경 (기계실로 바꿔주는 건 괴물 쪽에서 처리)
        if (!(alertArea == AlertArea.MachinerySpace && CurrentAlertArea != AlertArea.None))
            ChangeCurrentAlertPos(alertArea, destroyEquimentIndex);

        AudioManager.Instance.PlaySoundSafe(_audioSource, alertSound); // 경보 소리 재생

        // 경보 발생 시작 이벤트 알림
        OnAlertStarted?.Invoke();

        // 기계실 경보 발생해야 하는 경우 -> 괴물에 이벤트 알려주고 바로 종료 (아직 기계실 경보로 바꾸지 않음)
        if (alertArea == AlertArea.MachinerySpace)
        {
            OnMachinerySpaceAlertStarted?.Invoke();
        }
        else
        {
            OnNonMachinerySpaceAlertStarted?.Invoke();
        }

        Debug.Log("경보 발생!");
    }

    /// <summary>
    /// 현재 경보 발생 위치 변경
    /// </summary>
    /// <param name="alertArea"></param>
    /// <param name="destroyEquimentIndex"></param>
    public void ChangeCurrentAlertPos(AlertArea alertArea, int destroyEquimentIndex = -1)
    {
        CurrentAlertArea = alertArea;
        CurrentDestroyEquipmentIndex = destroyEquimentIndex;

        AlertAreaInfo alertAreaInfo = GetAlertAreaInfo(alertArea);
        if (destroyEquimentIndex == -1)
        {
            CurrentDestroyEquipmentObj = null;
            CurrentDestroyPos = alertAreaInfo.destroyPosTransforms[0];
            if (alertAreaInfo.sequenceCameraPosList.Count > 0)
                CurrentSequenceCameraPos = alertAreaInfo.sequenceCameraPosList[0];
        }
        else
        {
            CurrentDestroyEquipmentObj = alertAreaInfo.destroyEquipments[destroyEquimentIndex];
            CurrentDestroyPos = alertAreaInfo.destroyPosTransforms[destroyEquimentIndex];
            if (alertAreaInfo.sequenceCameraPosList.Count > 0)
                CurrentSequenceCameraPos = alertAreaInfo.sequenceCameraPosList[destroyEquimentIndex];
        }
    }

    /// <summary>
    /// 경보 해제
    /// </summary>
    public void AlertOff()
    {
        _isAlerting = false; // 경보 발생 중 아님으로 설정

        CurrentAlertArea = AlertArea.None;
        CurrentDestroyEquipmentIndex = -1;
        CurrentDestroyEquipmentObj = null;
        CurrentDestroyPos = null;
        CurrentSequenceCameraPos = null;

        _audioSource.Stop();
        Debug.Log("경보 해제");

        // 경보 발생 끝 이벤트 알림
        OnAlertEnded?.Invoke();

        // // 임시 - 경보 버튼 하얀색으로 변경(후에 지워야 함)
        // ColorBlock colorBlock = alertButton.colors;
        // colorBlock.normalColor = Color.white;
        // alertButton.colors = colorBlock;
    }

    // /// <summary>
    // /// 경보 발생
    // /// </summary>
    // public void AlertOn()
    // {
    //     if (hasEverOpenedEscapeDoor) // 탈출실 문이 한 번이라도 열린 경우(이후 어뢰실 장비에서 경보 발생해도 경보 발생 위치는 탈출실 위치로 설정됨(우선순위 더 높음))
    //     {
    //         _currentTargetPos = escapeRoomPos; // 현재 목표 위치 -> 탈출실 위치로 설정
    //     }
    //     else // 어뢰실 장비에서 경보 발생하는 경우
    //     {
    //         // 인덱스 범위 넘는 경우 예외 처리
    //         if (currentDestroyEquipmentIndex >= destroyEquipments.Length)
    //         {
    //             // AlertOff();
    //             return;
    //         }

    //         // 현재 파괴 정보 설정
    //         _currentDestroyEquipment = destroyEquipments[currentDestroyEquipmentIndex]; // 현재 파괴될 장비 설정
    //         _currentTargetPos = _currentDestroyEquipment.transform.GetChild(0); // 현재 목표 위치 -> 현재 파괴될 장비 위치의 파괴 위치(첫번째 자식)로 변경
    //     }

    //     // 임시 - 경보 버튼 빨간색으로 변경(후에 지워야 함)
    //     ColorBlock colorBlock = alertButton.colors;
    //     colorBlock.normalColor = Color.red;
    //     alertButton.colors = colorBlock;

    //     _isAlerting = true; // 경보 발생 중으로 설정
    //     AudioManager.Instance.PlaySoundSafe(_audioSource, alertSound);
    //     Debug.Log("경보 발생!");

    //     // 경보 발생 시작 이벤트 알림
    //     OnAlertStarted?.Invoke();
    // }

    // /// <summary>
    // /// 경보 해제
    // /// </summary>
    // public void AlertOff()
    // {
    //     _currentDestroyEquipment = null; // 현재 파괴될 장비 없으므로 null로 초기화
    //     _currentTargetPos = null; // 현재 목표 위치 null로 초기화
    //     _isAlerting = false; // 경보 발생 중 아님으로 설정
    //     Debug.Log("경보 해제");
    //     _audioSource.Stop();

    //     // 임시 - 경보 버튼 하얀색으로 변경(후에 지워야 함)
    //     ColorBlock colorBlock = alertButton.colors;
    //     colorBlock.normalColor = Color.white;
    //     alertButton.colors = colorBlock;
    // }

    /// <summary>
    /// 인게임 UI(인벤토리, 스탯, 상호작용) 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetActiveInGameUI(bool isActive)
    {
        // 활성화 여부 설정
        inventoryUI.SetActive(isActive); // 인벤토리 UI 
        statUI.SetActive(isActive); // 스탯 UI
        interactorUI.SetActive(isActive); // 상호작용 UI
    }
}
