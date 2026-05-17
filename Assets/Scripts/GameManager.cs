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
    [SerializeField] private GameObject menuUI;
    public GameObject MenuUI => menuUI;

    private bool _isClear = false; // 클리어 여부 변수
    public bool IsClear => _isClear;

    private float _mouseSensitivity; // 마우스 감도
    public float MouseSensitivity => _mouseSensitivity;
    private bool _haveToShowCursor = true;
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
            if (!_haveToShowCursor)
            {
                Cursor.visible = false; // 마우스 커서 숨김
                Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
            }
        }
    }

    /// <summary>
    /// 커서 보여줘야하는지 여부 설정
    /// </summary>
    /// <param name="isShow"></param>
    public void SetHaveToShowCursor(bool isShow)
    {
        _haveToShowCursor = isShow;
    }

    public void ToggleMenu()
    {
        menuUI.SetActive(!menuUI.activeSelf);
        if (menuUI.activeSelf)
        {
            MenuUIController.instance.OpenTab(0);
        }
    }

    public void ExitMenu()
    {
        menuUI.SetActive(false);
        // 인게임 매니저가 존재한다면(플레이 중)
        if (SubmarineInGameManager.instance != null)
        {
            if (SubmarineInGameManager.instance.IsPausing) // 정지 중이면
            {
                SubmarineInGameManager.instance.Resume(); // 정지 해제(플레이)
            }
            else // 플레이 중이면
            {
                SubmarineInGameManager.instance.Pause(); // 정지
            }
        }
    }


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
        if (menuUI.activeSelf)
        {
            menuUI.SetActive(false);
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
        SaveEnding(EEndingType.EscapeSuccess);
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
        SaveEnding(type); // 엔딩 데이터 저장
        SetCursorVisible(true); // 커서 보이기
        Time.timeScale = 0f; // 시간 정지
        _isClear = false;
        ShowEnding(); // 엔딩 보여주기
    }

    /// <summary>
    /// 특정 엔딩을 저장
    /// </summary>
    /// <param name="ending">달성한 엔딩 종류</param>
    public void SaveEnding(EEndingType ending)
    {
        // PlayerPrefs를 사용하여 저장 (Key: Ending_타입명, Value: 1(해금됨))
        string key = "Ending_" + ending.ToString();
        string firstTimeKey = key + "_FirstTime";
        string bestTimeKey = key + "_BestTime";
        float currentTime = GameTime.Instance.TimeSinceStart;

        if (PlayerPrefs.GetInt(key, 0) == 0) // 처음 해금하는 경우
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.SetFloat(firstTimeKey, currentTime); // 최초 기록 저장
            PlayerPrefs.SetFloat(bestTimeKey, currentTime);  // 최초 기록이 곧 베스트
        }
        else // 이미 해금된 경우 - 최단 기록 갱신 확인
        {
            float existingBest = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);
            if (currentTime < existingBest)
            {
                PlayerPrefs.SetFloat(bestTimeKey, currentTime);
            }
        }
        PlayerPrefs.Save(); // 디스크에 즉시 저장
        Debug.Log($"{ending} 엔딩이 수집되었습니다!");
    }

    /// <summary>
    /// 엔딩 보여주기
    /// </summary>
    private void ShowEnding()
    {
        // Cursor.visible = true; // 마우스 커서 보이게 함
        // Cursor.lockState = CursorLockMode.None; // 마우스 고정 해제
        Time.timeScale = 1.0f; // 시간 정지 해제

        if (menuUI.activeSelf)
        {
            menuUI.SetActive(false);
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }
        SceneManager.LoadScene("EndingScene"); // 엔딩 씬으로 이동
    }

    /// <summary>
    /// 타이틀 씬으로 돌아가기
    /// </summary>
    public void ReturnToTitle()
    {
        if (menuUI.activeSelf)
        {
            menuUI.SetActive(false);
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }
        SceneManager.LoadScene("TitleScene"); // 추후 씬 이름 수정 예정
    }

    public void EndingGallery()
    {
        if (menuUI.activeSelf)
        {
            menuUI.SetActive(false);
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
