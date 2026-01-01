using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 게임 전반의 플레이와 관련된 변수, 함수 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    // 정지
    private bool _isPausing = false; // 정지 중 여부
    public bool IsPausing { get => _isPausing; set => _isPausing = value; }

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
    private Transform _currentAlertPos; // 현재 경보 발생 위치
    public Transform CurrentAlertPos => _currentAlertPos;

    // 문
    private Door[] _doors;
    public Door[] Doors { get => _doors; set => _doors = value; }

    public event Action OnAlertStarted; // 경보 발생 시작 이벤트

    // 싱글톤 변수
    public static GameManager instance;

    /// <summary>
    /// 싱글톤 구현
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 문 가져오기(후에 doorManager를 추가해 옮길 수 있음)
        _doors = FindObjectsOfType<Door>();

        // 게임 초기 설정
        InitGame();
    }

    /// <summary>
    /// 게임 초기 설정
    /// </summary>
    private void InitGame()
    {
        _isPausing = false; // 정지 해제
    }

    /// <summary>
    /// 경보 발생
    /// </summary>
    public void AlertOn()
    {
        if (hasEverOpenedEscapeDoor) // 탈출실 문이 한 번이라도 열린 경우(이후 어뢰실 장비에서 경보 발생해도 경보 발생 위치는 탈출실 위치로 설정됨(우선순위 더 높음))
        {
            _currentAlertPos = escapeRoomPos; // 현재 경보 위치 -> 탈출실 위치로 설정
        }
        else // 어뢰실 장비에서 경보 발생하는 경우
        {
            // 인덱스 범위 넘는 경우 예외 처리
            if (currentDestroyEquipmentIndex >= destroyEquipments.Length)
            {
                AlertOff();
                return;
            }

            // 현재 파괴 정보 설정
            _currentDestroyEquipment = destroyEquipments[currentDestroyEquipmentIndex]; // 현재 파괴될 장비 설정
            _currentAlertPos = _currentDestroyEquipment.transform; // 현재 경보 위치 -> 현재 파괴될 장비 위치로 변경
        }

        // 임시 - 경보 버튼 빨간색으로 변경(후에 지워야 함)
        ColorBlock colorBlock = alertButton.colors;
        colorBlock.normalColor = Color.red;
        alertButton.colors = colorBlock;

        _isAlerting = true; // 경보 발생 중으로 설정
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
        _currentAlertPos = null; // 현재 경보 발생 위치 null로 초기화
        _isAlerting = false; // 경보 발생 중 아님으로 설정
        Debug.Log("경보 해제");

        // 임시 - 경보 버튼 하얀색으로 변경(후에 지워야 함)
        ColorBlock colorBlock = alertButton.colors;
        colorBlock.normalColor = Color.white;
        alertButton.colors = colorBlock;
    }

    /// <summary>
    /// 게임 정지
    /// </summary>
    public void Pause()
    {
        _isPausing = true; // 정지 중으로 설정
        Cursor.visible = true; // 마우스 커서 보이게 함
        Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
        Time.timeScale = 0f; // 시간 정지
    }

    /// <summary>
    /// 게임 정지 해제
    /// </summary>
    public void Resume()
    {
        _isPausing = false; // 정지 중 아님으로 설정
        Cursor.visible = false; // 마우스 커서 숨김
        Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
        Time.timeScale = 1.0f; // 시간 정지 해제
    }

    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        // 임시 - 후에 수정 예정
        Debug.Log("게임 오버!");
        Pause(); // 정지
    }
}
