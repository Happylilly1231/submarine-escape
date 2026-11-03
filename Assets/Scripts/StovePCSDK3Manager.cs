using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Stove.PCSDK.Base;
using static Stove.PCSDK.GameSupport;

public class StovePCSDK3Manager : MonoBehaviour
{
    // 초기화 여부를 저장하기 위한 변수
    private bool _isInitialized;

    // 코루틴 실행 주기를 저장하기 위한 변수
    private float _runCallbackInterval = 1.0f;

    // RunCallbackLoop 코루틴을 저장하기 위한 변수
    private Coroutine _runCallbackCoroutine;

    // 오브젝트를 Singleton 형태로 사용하기 위한 정적 변수
    private static StovePCSDK3Manager _instance;
    private static object _lockObject = new object();

    private OnStatFinished _onStatOnFinished;
    private OnModifyStatFinished _onModifyStatFinished;

    public static StovePCSDK3Manager Instance
    {
        get
        {
            lock (_lockObject)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StovePCSDK3Manager>();

                    if (_instance == null)
                    {
                        _instance = new GameObject().AddComponent<StovePCSDK3Manager>();
                        _instance.name = "StovePCSDK3Manager";
                    }
                }
            }

            return _instance;
        }
    }

    #region

    // DontDestroyOnLoad 처리를 진행
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    // OnDestroy 에서 UnInitialize 호출
    // 초기화 여부에 따라 SDK 모듈을 자동 정리
    private void OnDestroy()
    {
        if (_isInitialized)
        {
            UnInitialize();
        }
    }

    private void Start()
    {
        _onStatOnFinished = (
            CallbackResult callbackResult,
            StovePCStat stat) =>
        {
            if (callbackResult.result.IsSuccessful())
            {
                Debug.Log($"Stat finished successfully. statId: {stat.ToString()}");
            }
            else
            {
                Debug.Log($"Stat finished with error.");
            }
        };

        _onModifyStatFinished = (
            CallbackResult callbackResult,
            StovePCModifyStatValue stat) =>
        {
            if (callbackResult.result.IsSuccessful())
            {
                Debug.Log($"Modify stat finished successfully. statId: {stat.ToString()}");
            }
            else
            {
                Debug.Log($"Modify stat finished with error.");
            }
        };
    }

    #endregion        

    #region Coroutine

    /// <summary>
    /// RunCallback을 처리하기 위한 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator RunCallbackCoroutine()
    {
        var wfs = new WaitForSeconds(_runCallbackInterval);

        while (true)
        {
            Base_RunCallback();

            yield return wfs;
        }
    }

    #endregion

    #region STOVEPCSDK3Manager public methods

    /// <summary>
    /// Result 구조체 출력 메서드
    /// </summary>
    public void PrintResult(Result r)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Result");
        sb.AppendLine($" - Result.sdkName : {r.sdkName}");
        sb.AppendLine($" - Result.methodCode : {r.methodCode}");
        sb.AppendLine($" - Result.resultCode : {r.resultCode}");
        sb.AppendLine($" - Result.exceptionMessage : {r.exceptionMessage}");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// CallbackResult 구조체 출력 메서드
    /// </summary>
    public void PrintCallbackResult(CallbackResult cr)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# CallbackResult");
        sb.AppendLine($" - CallbackResult.Result.sdkName : {cr.result.sdkName}");
        sb.AppendLine($" - CallbackResult.Result.methodCode : {cr.result.methodCode}");
        sb.AppendLine($" - CallbackResult.Result.resultCode : {cr.result.resultCode}");
        sb.AppendLine($" - CallbackResult.Result.exceptionMessage : {cr.result.exceptionMessage}");
        sb.AppendLine($" - CallbackResult.externalError : {cr.externalError}");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 모듈 통합 초기화를 위한 Initialize 메소드
    /// <para> - Base SDK 연동 및 초기화 후 GameSupport SDK 초기화 </para>
    /// </summary>
    public void Initialize()
    {
        StartRunCallbackLoop();

        var initParam = new StovePCInitializeParamEx2
        {
            environment = "LIVE",
            gameId = "GM-2A61-6A50E4C8_IND",
            applicationKey = "38bf1f7f50e8087a616d425587d1a582ec03416384eb2216d40490e4f2e29dc3",
            waitTimeMillisec = 60000,
            launchLauncher = true
        };

        // 런처 확인
        Base_RestartAppIfNecessaryAsyncEx2(initParam,
            (CallbackResult callbackResult, bool restartAppIfNecessary) =>
        {
            if (restartAppIfNecessary)
            {
                // 런처를 통해 게임을 실행하지 않았므로 게임종료 처리
                Debug.Log("Please run the game through the stove launcher.");
                Application.Quit();
                return;
            }

            // 캐시된 파라미터로 Base SDK 초기화
            Base_InitializeEx((CallbackResult cbResult) =>
            {
                // Print CallbackResult
                PrintCallbackResult(cbResult);

                if (cbResult.result.IsSuccessful()) // Base SDK 초기화 성공
                {
                    Result result = GameSupport_Initialize();
                    PrintResult(result);

                    _isInitialized = true;
                }
                else // Base SDK 초기화 실패
                {
                    Debug.Log("Fail to initialize Base SDK");
                }
            });
        });
    }

    /// <summary>
    /// 모듈 통합 정리를 위한 UnInitialize 메소드
    /// </summary>
    public void UnInitialize()
    {
        Result result;

        this.StopRunCallbackLoop();

        result = Base_UnInitialize();
        PrintResult(result);

        result = GameSupport_UnInitialize();
        PrintResult(result);

        _isInitialized = false;
    }

    /// <summary>
    /// RunCallback을 주기적으로 호출하기 위한 메소드
    /// </summary>
    public void StartRunCallbackLoop()
    {
        if (_runCallbackCoroutine == null)
        {
            Debug.Log("Start RunCallbackLoop");

            _runCallbackCoroutine = StartCoroutine(RunCallbackCoroutine());
        }
    }

    /// <summary>
    /// Coroutine을 중지하기 위한 메소드
    /// </summary>
    public void StopRunCallbackLoop()
    {
        if (_runCallbackCoroutine != null)
        {
            Debug.Log("Stop RunCallbackLoop");

            StopCoroutine(_runCallbackCoroutine);
            _runCallbackCoroutine = null;
        }
    }

    /// <summary>
    /// 스탯 정보 조회
    /// </summary>
    /// <param name="gameStatId"></param>
    public void GetStat(string gameStatId)
    {
        GameSupport_Stat(gameStatId, _onStatOnFinished);
    }

    /// <summary>
    /// 스탯 정보 수정
    /// </summary>
    /// <param name="gameStatId"></param>
    /// <param name="value"></param>
    public void ModifyStat(string gameStatId, int value)
    {
        GameSupport_ModifyStat(gameStatId, value, _onModifyStatFinished);
    }

    #endregion
}
