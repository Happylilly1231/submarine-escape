using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameFocusState
{
    None,           // 포커스 없는 상태
    GameTimePauseSequence,  // 게임 시간 정지 연출 (Time이 아니라 GameTime 기준 정지) (사용 목적: 이 연출을 보여주는 중에 플레이어 괴물화 가시 생성 연출, 심해 괴물 엔딩 안 보도록 막음)
    Puzzle,         // 퍼즐 포커스
    InGameMenu,     // 인게임 메뉴 포커스
    ESCMenu,        // ESC 메뉴(실제 정지) 포커스 
    UIScene,        // UI만 있고, 플레이어 없는 씬 - EndingScene 전용 포커스
}

public class FocusManager : MonoBehaviour
{
    public InGameMenuController inGameMenuController { get; set; }

    // 현재 포커스 상태
    public GameFocusState CurrentFocusState { get; private set; } = GameFocusState.None;

    // 이전 포커스 상태 기록(스택)
    private Stack<GameFocusState> _focusStateHistory = new Stack<GameFocusState>();

    public PuzzleController CurrentPuzzleController { get; private set; } = null;

    public static FocusManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // private void Start()
    // {
    //     // 게임 시작 시 None 상태의 UI, 커서, 인풋 설정을 실행 - 타이틀 씬에서 커서 안 보이게 설정
    //     ResetFocusState(GameFocusState.None);
    // }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드되면 자동으로 호출되는 함수
    /// <para> - 씬 전환 시 포커스 상태를 None으로 초기화</para>
    /// </summary>
    /// <param name="scene">로드된 씬</param>
    /// <param name="mode">로드 모드</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 시 포커스 상태를 None으로 초기화 (단, EnidngScene만 UIScene으로 초기화)
        if (scene.name == "EndingScene")
            ResetFocusState(GameFocusState.UIScene);
        else
            ResetFocusState(GameFocusState.None);
        Debug.Log($"{scene.name} 씬 로드됨. 포커스 None으로 초기화.");
    }

    /// <summary>
    /// 새로운 포커스 상태로 완전히 전환하고 스택을 초기화할 때 사용
    /// </summary>
    public void ResetFocusState(GameFocusState newState)
    {
        // 강제로 완전히 새 상태로 갈 때는 기존 히스토리를 비움
        _focusStateHistory.Clear();

        TransitionToState(newState);
    }

    /// <summary>
    /// 포커스 상태 적용
    /// </summary>
    /// <param name="newState">새 포커스 상태</param>
    public void PushFocusState(GameFocusState newState)
    {
        // 현재 상태를 스택에 푸시(저장)
        _focusStateHistory.Push(CurrentFocusState);

        // 새 포커스 상태로 전환
        TransitionToState(newState);
    }

    /// <summary>
    /// 이전 포커스 상태 복구
    /// </summary>
    public void PopFocusState()
    {
        // 스택이 비어 있는 경우
        if (_focusStateHistory.Count == 0)
        {
            // 기본 상태인 포커스 없는 상태로 전환
            TransitionToState(GameFocusState.None);
            return;
        }

        // 스택에 가장 최근에 푸시(저장)된 이전 포커스 상태 팝
        GameFocusState previousState = _focusStateHistory.Pop();

        // 해당 상태로 전환
        TransitionToState(previousState);
    }

    /// <summary>
    /// 포커스 상태 전환
    /// </summary>
    /// <param name="newState">새 포커스 상태</param>
    public void TransitionToState(GameFocusState newState)
    {
        // 이전 상태 변수 저장, 현재 포커스 상태를 새 상태로 갱신
        GameFocusState oldState = CurrentFocusState;
        CurrentFocusState = newState;

        Debug.Log($"갱신된 현재 포커스 상태:{CurrentFocusState} (이전 포커스 상태: {oldState})");

        switch (CurrentFocusState)
        {
            case GameFocusState.None:
                PlayerManager.Instance.SetPlayerCanMove(true); // 플레이어 이동/회전 가능
                SetCenterUIActive(true); // 가운데 UI 요소 켜기
                GameManager.instance.SetCursorVisible(false); // 커서 안 보이게
                break;

            case GameFocusState.GameTimePauseSequence:
                PlayerManager.Instance.SetPlayerCanMove(false); // 플레이어 이동/회전 불가능
                SetCenterUIActive(false); // 가운데 UI 요소 끄기
                GameManager.instance.SetCursorVisible(false); // 커서 안 보이게

                if (GameTime.Instance != null) GameTime.Instance.SetPause(true); // 게임 시간 정지

                // 연출이 보이도록 모든 창 다 끄고 나가기
                if (oldState == GameFocusState.Puzzle)
                {
                    // 현재 퍼즐 진행 중이었다면 그 퍼즐 종료
                    if (CurrentPuzzleController != null)
                    {
                        CurrentPuzzleController.ExitPuzzle();
                    }
                }
                else if (oldState == GameFocusState.InGameMenu)
                {
                    if (inGameMenuController != null)
                        inGameMenuController.CloseInGameMenu();
                }
                break;

            case GameFocusState.Puzzle:
                PlayerManager.Instance.SetPlayerCanMove(false); // 플레이어 이동/회전 불가능
                SetCenterUIActive(false); // 가운데 UI 요소 끄기
                if (CurrentPuzzleController != null)
                {
                    bool isCursorVisible = CurrentPuzzleController.IsCurrentMouseRequired;
                    GameManager.instance.SetCursorVisible(isCursorVisible); // 커서 보이기 여부 설정
                }
                else
                {
                    GameManager.instance.SetCursorVisible(false); // 퍼즐이 없는데 퍼즐 포커스를 호출했다면 커서 끄기 (예외 처리)
                }
                break;

            case GameFocusState.InGameMenu:
                PlayerManager.Instance.SetPlayerCanMove(false); // 플레이어 이동/회전 불가능
                SetCenterUIActive(false); // 가운데 UI 요소 끄기
                GameManager.instance.SetCursorVisible(true); // 커서 보이게
                break;

            case GameFocusState.ESCMenu:
                GameManager.instance.SetCursorVisible(true); // 커서 보이게
                break;

            case GameFocusState.UIScene:
                GameManager.instance.SetCursorVisible(true); // 커서 보이게
                break;
        }

        // 인풋 관리
        HandleInput(oldState);
    }

    /// <summary>
    /// 인풋 관리
    /// </summary>
    /// <param name="oldState">이전 상태</param>
    private void HandleInput(GameFocusState oldState)
    {
        switch (CurrentFocusState)
        {
            case GameFocusState.None:
                InputManager.instance.SwitchActionMapWithPermanent("Player"); // 플레이어 액션 맵으로 변경 및 활성화 (Permanant 맵도 같이 활성화)
                break;

            case GameFocusState.GameTimePauseSequence:
                InputManager.instance.DisableAllInputs(); // 모든 인풋 비활성화
                break;

            case GameFocusState.Puzzle:
                InputManager.instance.SwitchActionMapWithPermanent("Puzzle"); // 퍼즐 액션 맵으로 변경 및 활성화 (Permanant 맵도 같이 활성화)
                if (oldState == GameFocusState.InGameMenu) // 인게임 메뉴 -> 퍼즐
                {
                    InputManager.instance.RestoreInputsFromSnapshot(); // 인풋 복구
                }
                break;

            case GameFocusState.InGameMenu:
                bool isToggleMenuEnabled = PlayerManager.Instance.playerInput.actions["ToggleMenu"].enabled; // ESC 메뉴 토글 액션 켜져있는지 여부 저장
                if (oldState == GameFocusState.Puzzle) // 퍼즐 -> 인게임 메뉴
                {
                    InputManager.instance.SaveAndDisableAllInputs(); // 저장 & 모든 인풋 비활성화
                }
                else
                {
                    InputManager.instance.DisableAllInputs(); // 모든 인풋 비활성화
                }
                PlayerManager.Instance.playerInput.actions["ToggleInGameMenu"].Enable(); // 인게임 메뉴 토글 액션 활성화
                // ESC 메뉴 토글 액션이 켜져있었으면 활성화
                if (isToggleMenuEnabled)
                    PlayerManager.Instance.playerInput.actions["ToggleMenu"].Enable();
                break;

            case GameFocusState.ESCMenu:
                InputManager.instance.DisableAllInputs(); // 모든 인풋 비활성화
                PlayerManager.Instance.playerInput.actions["ToggleMenu"].Enable(); // ESC 메뉴 토글 액션 활성화
                PlayerManager.Instance.playerInput.actions["ToggleDebug"].Enable(); // 디버그 토글 액션 활성화 (나중에 제거 필요)
                break;

            case GameFocusState.UIScene:
                // PlayerInput을 가진 플레이어가 없으므로 인풋 비활성화 필요 X
                break;
        }
    }

    /// <summary>
    /// 가운데 UI(조준점, 상호작용 감지 텍스트) 요소 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">지우기 여부</param>
    private void SetCenterUIActive(bool isActive)
    {
        if (isActive)
        {
            PlayerManager.Instance.playerInteractor.SetActiveAimUI(true); // 조준점 켜기
        }
        else
        {
            PlayerManager.Instance.playerInteractor.SetActiveAimUI(false); // 조준점 끄기
            PlayerManager.Instance.playerInteractor.ClearDetectionText(); // 상호작용 감지 텍스트 클리어
        }
    }

    /// <summary>
    /// 현재 퍼즐 컨트롤러 갱신
    /// </summary>
    /// <param name="puzzleController">퍼즐 컨트롤러</param>
    public void SetCurrentPuzzleController(PuzzleController puzzleController)
    {
        CurrentPuzzleController = puzzleController;
    }
}