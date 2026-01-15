using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 게임 전반의 플레이와 관련된 변수, 함수 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    // 정지
    private bool _isPausing = false; // 정지 중 여부
    public bool IsPausing { get => _isPausing; set => _isPausing = value; }

    private bool _haveToShowCursor = false; // 커서가 현재 보여야 하는지 여부(true일 때는 Resume(재시작)을 해도 커서를 숨기지 않음)
    public bool HaveToShowCursor { get => _haveToShowCursor; set => _haveToShowCursor = value; }

    private bool _isClear = false;
    public bool IsClear => _isClear;



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
        InitGame();
    }

    /// <summary>
    /// 게임 초기 설정
    /// </summary>
    private void InitGame()
    {
        _isPausing = false; // 정지 해제
        SetCursorVisible(false);
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        InitGame(); // 게임 초기 설정
        SceneManager.LoadScene("SubmarineScene");
    }

    /// <summary>
    /// 커서 보이거나 숨기기
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
        SetCursorVisible(true); // 커서 보이기
        Time.timeScale = 0f; // 시간 정지
        AudioListener.pause = true;
    }

    /// <summary>
    /// 게임 정지 해제
    /// </summary>
    public void Resume()
    {
        Debug.Log("재시작");
        _isPausing = false; // 정지 중 아님으로 설정
        if (!_haveToShowCursor) // 현재 커서가 보여야 하는 게 아니면
            SetCursorVisible(false); // 커서 숨기기
        Time.timeScale = 1.0f; // 시간 정지 해제
        AudioListener.pause = false;
    }

    /// <summary>
    /// 게임 클리어
    /// </summary>
    public void GameClear()
    {
        Debug.Log("게임 클리어!");
        Pause();
        _isClear = true;
        ShowEnding(); // 엔딩 보여주기
    }

    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        // 임시 - 후에 수정 예정
        Debug.Log("게임 오버!");
        Pause();
        _isClear = false;
        ShowEnding(); // 엔딩 보여주기
    }

    /// <summary>
    /// 엔딩 보여주기
    /// </summary>
    private void ShowEnding()
    {
        _isPausing = false; // 정지 중 아님으로 설정
        Cursor.visible = true; // 마우스 커서 보이게 함
        Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
        Time.timeScale = 1.0f; // 시간 정지 해제

        SceneManager.LoadScene("EndingScene"); // 엔딩 씬으로 이동
    }

    /// <summary>
    /// 타이틀 씬으로 돌아가기
    /// </summary>
    public void ReturnToTitle()
    {
        SceneManager.LoadScene("TitleScene"); // 추후 씬 이름 수정 예정
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
