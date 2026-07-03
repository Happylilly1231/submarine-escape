using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Difficulty { Easy, Hard }

[System.Serializable]
public class PlayerRecord
{
    public string playerName;   // 플레이어 이름
    public EEndingType endingType; // 달성한 엔딩 종류
    public float clearTime; // 클리어 기록 (초 단위)
    public string playDate; // 플레이 날짜
}

[System.Serializable]
public class RankingsData
{
    // 플레이엑스포 기간 동안 달성한 모든 사람의 엔딩 기록 리스트
    public List<PlayerRecord> playerRecords = new List<PlayerRecord>();
}

[System.Serializable]
public class SavePointData
{
    public ESavePointType savePointType; // 세이브 기점
    public bool isUnlocked;              // 해당 기점 해금 여부
    public string saveDate;              // 갱신된 날짜
    public float playTime;              // 플레이 타임

    [Header("[ Player States ]")]
    public SerializableVector3 playerPosition;              // 플레이어 위치
    public PlayerStatsData playerStats;                     // 플레이어 스탯
    public List<PlayerInventory> playerInventory = new List<PlayerInventory>();   // 인벤토리
    // public bool hasRegisteredMap;                           // 지도 아이템 등록 여부
    public int mutationStage;                               // 괴물화 진행 단계
    public bool isCureInjected;                             // 치료제 투여 여부

    [Header("[ Monster States ]")]
    public SerializableVector3 insideMonsterPosition;  // 내부 괴물 위치
    public SerializableVector3 outsideMonsterPosition; // 외부 괴물 위치
    public bool isInsideMonsterBerserk;                // 내부 괴물 폭주 여부

    public List<WorldItemSaveData> worldItems = new List<WorldItemSaveData>(); // 아이템 오브젝트들
}

[System.Serializable]
public class PlayerSaveContainer
{
    public string playerName;
    public List<SavePointData> savePoints = new List<SavePointData>();
}

[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public class PlayerStatsData
{
    public float hp;
    public float stamina;
}

[System.Serializable]
public class PlayerInventory
{
    public Item item;
    public int itemcnt;
    public int itemInstanceNum;
}

[System.Serializable]
public class WorldItemSaveData
{
    public Item item;
    public string uniqueID;
    public bool isDropped;
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 scale;
}

public enum ESavePointType
{
    CrewKeyPad, // 선원실 탈출
    PowerRestoration, // 전력 복구
    TorpedoLoaded, // 어뢰관 장전
    TorpedoFirstLaunch, // 어뢰 1차 발사
    TorpedoSecondLaunch, // 어뢰 2차 발사
    TorpedoThirdLaunch, // 어뢰 3차 발사
    CureInjected // 치료제 투여
}

/// <summary>
/// 게임 전반의 플레이와 관련된 변수, 함수 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    // [SerializeField] private GameObject menuUI;
    // public GameObject MenuUI => menuUI;

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

    [SerializeField] private string playerName;
    public string PlayerName { get => playerName; private set => playerName = value; } // 플레이어 이름
    public EPanelType CurrentPanelType = EPanelType.NameSetting;

    private int _cursorRequestCount = 0;

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

            playerName = PlayerPrefs.GetString("LastPlayerName", "");
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
            // if (!_haveToShowCursor)
            // {
            //     Cursor.visible = false; // 마우스 커서 숨김
            //     Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
            // }
        }
    }

    // /// <summary>
    // /// 커서 보여줘야하는지 여부 설정
    // /// </summary>
    // /// <param name="isShow"></param>
    // public void SetHaveToShowCursor(bool isShow)
    // {
    //     _haveToShowCursor = isShow;
    // }

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
    /// 플레이어 이름 설정
    /// </summary>
    /// <param name="name"></param>
    public void SetPlayerName(string name)
    {
        PlayerName = name;
        PlayerPrefs.SetString("LastPlayerName", name);
        PlayerPrefs.Save();
        Debug.Log($"플레이어 이름이 {name}(으)로 설정되었습니다.");
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (MenuUIController.instance.MenuUI.activeSelf)
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

    // 한 명의 플레이어가 단독으로 플레이할 때 '엔딩별 최초/최단 기록'을 관리
    // /// <summary>
    // /// 특정 엔딩을 저장
    // /// </summary>
    // /// <param name="ending">달성한 엔딩 종류</param>
    // public void SaveEnding(EEndingType ending)
    // {
    //     // PlayerPrefs를 사용하여 저장 (Key: Ending_타입명, Value: 1(해금됨))
    //     string key = "Ending_" + ending.ToString();
    //     string firstTimeKey = key + "_FirstTime";
    //     string bestTimeKey = key + "_BestTime";
    //     float currentTime = GameTime.Instance.TimeSinceStart;

    //     if (PlayerPrefs.GetInt(key, 0) == 0) // 처음 해금하는 경우
    //     {
    //         PlayerPrefs.SetInt(key, 1);
    //         PlayerPrefs.SetFloat(firstTimeKey, currentTime); // 최초 기록 저장
    //         PlayerPrefs.SetFloat(bestTimeKey, currentTime);  // 최초 기록이 곧 베스트
    //     }
    //     else // 이미 해금된 경우 - 최단 기록 갱신 확인
    //     {
    //         float existingBest = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);
    //         if (currentTime < existingBest)
    //         {
    //             PlayerPrefs.SetFloat(bestTimeKey, currentTime);
    //         }
    //     }
    //     PlayerPrefs.Save(); // 디스크에 즉시 저장
    //     Debug.Log($"{ending} 엔딩이 수집되었습니다!");
    // }

    /// <summary>
    /// 플레이엑스포 전용: 특정 엔딩을 달성했을 때 플레이어 이름과 기록을 JSON 파일로 저장하여 랭킹에 활용
    /// </summary>
    /// <param name="ending">달성한 엔딩 종류</param>
    public void SaveEnding(EEndingType ending)
    {
        float currentTime = GameTime.Instance.TimeSinceStart;

        // 기존 랭킹 데이터 불러오기
        string jsonString = PlayerPrefs.GetString("RankingsData", "");
        RankingsData rankingsData = JsonUtility.FromJson<RankingsData>(jsonString);

        if (rankingsData == null)
        {
            rankingsData = new RankingsData();
        }

        // 새로운 기록 추가
        PlayerRecord newRecord = new PlayerRecord
        {
            playerName = PlayerName,
            endingType = ending,
            clearTime = currentTime,
            playDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        rankingsData.playerRecords.Add(newRecord);

        // 랭킹 데이터를 JSON으로 변환하여 저장
        string newJsonString = JsonUtility.ToJson(rankingsData);
        PlayerPrefs.SetString("RankingsData", newJsonString);
        PlayerPrefs.Save();

        Debug.Log($"[플레이 엑스포 기록 등록] 엔딩: {ending} | 이름: {PlayerName} | 기록: {currentTime:F2}초");
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
        CurrentPanelType = EPanelType.GameMenu;
        if (MenuUIController.instance && MenuUIController.instance.MenuUI.activeSelf)
        {
            MenuUIController.instance.ToggleMenu();
            MenuUIController.instance.SetActiveDebuggingUI(false); // 디버깅 UI 비활성화
        }
        SceneManager.LoadScene("Title3DScene"); // 추후 씬 이름 수정 예정
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
