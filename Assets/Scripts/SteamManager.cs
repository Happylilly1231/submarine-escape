using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

/// <summary>
/// 스팀 도전 과제 API 이름 목록 (API 이름과 똑같이 씀. Enum을 그대로 문자열로 변경해서 사용)
/// </summary>
public enum SteamAchievementID
{
    ACH_ESCAPE_CREW_ROOM,
}

/// <summary>
/// 스팀 매니저 (도전 과제 관리 등)
/// </summary>
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    private uint appId = 5496220; // 앱 아이디

    public bool IsInitialized { get; private set; } // 초기화(연결)된 여부

    private void Awake()
    {
        // 싱글톤 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 넘어가도 유지

        // 초기화 - 스팀 클라이언트 연결
        InitializeSteam();

        // ResetAchievement(SteamAchievementID.ACH_ESCAPE_CREW_ROOM); // 선원실 탈출 도전 과제 초기화 (테스트용)
    }

    /// <summary>
    /// 스팀 클라이언트 연결
    /// </summary>
    private void InitializeSteam()
    {
        // try-catch로 감싸서 스팀 클라이언트가 꺼져있거나 에러가 날 경우에 로그만 남기고 그대로 게임 실행 가능
        try
        {
            SteamClient.Init(appId, true); // 스팀 클라이언트와 연결 (두번째 인자: 스팀 서버로부터 오는 응답을 어떻게 받아서 처리할지 결정하는 옵션. ture면 RunCallbacks를 자동으로 백그라운드에서 주기적으로 돌려줌. 따라서 Update에서 직접 호출할 필요가 없으므로 편함)
            IsInitialized = true; // 초기화됨으로 설정
            Debug.Log($"[Steam] 초기화 성공 - 유저: {SteamClient.Name} ({SteamClient.SteamId})");
        }
        catch (System.Exception e) // 에러 발생 시
        {
            IsInitialized = false; // 초기화되지 않음으로 설정
            Debug.LogWarning($"[Steam] 초기화 실패 (스팀 클라이언트가 꺼져 있거나 App ID 확인 필요): {e.Message}");
        }
    }

    /// <summary>
    /// 앱 종료 시
    /// </summary>
    private void OnApplicationQuit()
    {
        ShutdownSteam();
    }

    /// <summary>
    /// 오브젝트 파괴 시 (에디터에서 OnApplicationQuit이 실행되지 않는 경우 발생할 수 있으므로 방어)
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            ShutdownSteam();
        }
    }

    /// <summary>
    /// 스팀 연결 종료
    /// </summary>
    private void ShutdownSteam()
    {
        if (!IsInitialized) return;

        SteamClient.Shutdown(); // 스팀 연결 종료
        IsInitialized = false; // 초기화되지 않음으로 설정
        Debug.Log("[Steam] 종료 완료");
    }

    /// <summary>
    /// 도전 과제 달성
    /// </summary>
    /// <param name="achievementId">도전 과제 API 이름 (Enum)</param>
    public void UnlockAchievement(SteamAchievementID achievementId)
    {
        // 스팀 연결된 경우에만 진행
        if (!IsInitialized) return;

        // API 이름 - Enum 값을 그대로 문자열로 변경
        string apiName = achievementId.ToString();

        // 해당 API 이름의 도전 과제 객체 가져오기
        var ach = new Achievement(apiName);
        if (!ach.State) // 해당 도전 과제를 달성하지 않았으면 (중복 달성 방지)
        {
            ach.Trigger(); // 도전 과제 달성 처리 & 스팀 서버에 전송
            Debug.Log($"[Steam] 도전과제 달성: {apiName}");
        }
    }

    /// <summary>
    /// 특정 도전과제 초기화 (테스트용)
    /// </summary>
    public void ResetAchievement(SteamAchievementID achievementId)
    {
        // 스팀 연결된 경우에만 진행
        if (!IsInitialized) return;

        // API 이름 - Enum 값을 그대로 문자열로 변경
        string apiName = achievementId.ToString();

        var ach = new Achievement(apiName);
        if (ach.State) // 해당 도전 과제를 달성했으면
        {
            ach.Clear(); // 달성 여부 초기화
            Debug.Log($"[Steam] 도전과제 초기화: {apiName}");
        }
    }
}