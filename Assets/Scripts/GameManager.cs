using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Difficulty { Easy, Hard }

/// <summary>
/// 게임 전반의 플레이와 관련된 변수, 함수 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    // 정지
    private bool _isPausing = false; // 정지 중 여부
    public bool IsPausing { get => _isPausing; set => _isPausing = value; }

    private bool _isClear = false; // 클리어 여부 변수
    public bool IsClear => _isClear;

    private float _mouseSensitivity; // 마우스 감도
    public float MouseSensitivity => _mouseSensitivity;
    private bool _haveToShowCursor = false;
    public bool HaveToShowCursor => _haveToShowCursor;

    // 현재 난이도
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Easy;

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

    /// <summary>
    /// 마우스 감도 설정
    /// </summary>
    public void SetMouseSensitivity(float value)
    {
        _mouseSensitivity = value;
    }

    /// <summary>
    /// 커서 보이거나 숨기기 (커서 보여야 하는 중에는 숨기지 않음)
    /// </summary>
    /// <param name="visible">보이기 여부</param>
    public void SetCursorVisible(bool visible)
    {
        if (visible)
        {
            Cursor.visible = true; // 마우스 커서 보이게 함
            Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
        }
        else
        {
            Cursor.visible = false; // 마우스 커서 숨김
            Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
        }
    }

    /// <summary>
    /// 게임 정지
    /// </summary>
    public void Pause()
    {
        Debug.Log("정지");
        _isPausing = true; // 정지 중으로 설정

        Time.timeScale = 0f; // 시간 정지
        AudioListener.pause = true; // 오디오 듣기 정지

        FocusManager.Instance.PushFocusState(GameFocusState.ESCMenu); // 퍼즐 포커스 상태로 변경
    }

    /// <summary>
    /// 게임 정지 해제
    /// </summary>
    public void Resume()
    {
        Debug.Log("재시작");
        _isPausing = false; // 정지 중 아님으로 설정

        Time.timeScale = 1.0f; // 시간 정지 해제
        AudioListener.pause = false; // 오디오 듣기 정지 해제

        FocusManager.Instance.PopFocusState();
    }

    // public void ExitMenu()
    // {
    //     menuUI.SetActive(false);
    //     if (IsPausing) // 정지 중이면
    //     {
    //         Resume(); // 정지 해제(플레이)
    //     }
    //     else // 플레이 중이면
    //     {
    //         Pause(); // 정지
    //     }
    // }

    // 난이도 설정
    public void SetDifficulty(Difficulty difficulty)
    {
        CurrentDifficulty = difficulty;
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (MenuUIController.instance != null && MenuUIController.instance.MenuUI.activeSelf)
        {
            MenuUIController.instance.ToggleMenu();
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }
        SceneManager.LoadScene("SubmarineScene");
    }

    /// <summary>
    /// 게임 클리어
    /// </summary>
    public void GameClear()
    {
        Debug.Log("게임 클리어!");
        EndingSaveManager.Instance.SaveEnding(EEndingType.EscapeSuccess);
        SetCursorVisible(true); // 커서 보이기
        Time.timeScale = 0f; // 시간 정지
        _isClear = true;
        ShowEnding(); // 엔딩 보여주기
    }

    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver(EEndingType type)
    {
        // 임시 - 후에 수정 예정
        Debug.Log($"게임 오버!: {type}");
        EndingSaveManager.Instance.SaveEnding(type); // 엔딩 데이터 저장
        SetCursorVisible(true); // 커서 보이기
        Time.timeScale = 0f; // 시간 정지
        _isClear = false;
        ShowEnding(); // 엔딩 보여주기
    }

    /// <summary>
    /// 엔딩 보여주기
    /// </summary>
    private void ShowEnding()
    {
        // Cursor.visible = true; // 마우스 커서 보이게 함
        // Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
        Time.timeScale = 1.0f; // 시간 정지 해제

        if (MenuUIController.instance.MenuUI.activeSelf)
        {
            MenuUIController.instance.ToggleMenu();
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }
        SceneManager.LoadScene("EndingScene"); // 엔딩 씬으로 이동
    }

    /// <summary>
    /// 타이틀 씬으로 돌아가기
    /// </summary>
    public void ReturnToTitle()
    {
        //CurrentPanelType = EPanelType.GameMenu;
        if (MenuUIController.instance && MenuUIController.instance.MenuUI.activeSelf)
        {
            MenuUIController.instance.ToggleMenu();
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }

        SceneManager.LoadScene("Title3DScene");
    }

    public void EndingGallery()
    {
        if (MenuUIController.instance.MenuUI.activeSelf)
        {
            MenuUIController.instance.ToggleMenu();
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }
        SceneManager.LoadScene("EndingFrameScene");
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR // 에디터의 경우
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 플레이 모드 종료
#else // 실제 플레이 경우
    Application.Quit(); // 실제 게임 종료
#endif
    }
}
