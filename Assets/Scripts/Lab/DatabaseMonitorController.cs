using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DatabaseMonitorController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    [SerializeField] private GameObject actionText;

    // 로그인
    public DatabaseMonitorState CurrentState { get; private set; } = DatabaseMonitorState.Login; // 현재 모니터 상태
    private const string CORRECT_ID = "ADMIN"; // 정답 아이디
    private const string CORRECT_PASSWORD = "123"; // 정답 비밀번호

    private DatabaseMonitorDisplay _monitorDisplay;

    private void Awake()
    {
        _monitorDisplay = GetComponent<DatabaseMonitorDisplay>();
    }

    public override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        if (CurrentState == DatabaseMonitorState.Login)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                // Tab 키 입력 시 [아이디 입력 필드]에서 [비밀번호 입력 필드]로 포커스 전환
                if (_monitorDisplay.idText.isFocused) _monitorDisplay.pwText.ActivateInputField();
            }
        }
    }

    /// <summary>
    /// Tab(지도) 활성화 여부를 결정
    /// <para> - 모니터 상호작용 중에는 Permanent 비활성화 </para>
    /// <para> - 로그인 화면: tab키로 지도 활성화 불가 </para>
    /// <para> - 메인 화면: tab키로 지도 활성화 가능 </para>
    /// </summary>
    private void SetToggleMapEnabled(bool isEnable)
    {
        SubmarineInGameManager.instance.playerInput.actions.FindActionMap("Permanent")?.Disable();
        var toggleMapAction = SubmarineInGameManager.instance.playerInput.actions.FindAction("ToggleMap");
        if (toggleMapAction != null)
        {
            if (isEnable) toggleMapAction.Enable();
            else toggleMapAction.Disable();
        }
    }

    #region 퍼즐 시작/종료
    public override void ActivatePuzzle()
    {
        inventoryManager.CloseInventory();
        itemEquipController.UnequipItem();
        actionText.SetActive(false);

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        // 퍼즐 시작 시 현재 상태에 맞는 패널 표시
        _monitorDisplay.ShowPanel(CurrentState);

        if (CurrentState == DatabaseMonitorState.Login)
        {
            SetToggleMapEnabled(false);
            Debug.Log("로그인 화면");
        }
        else
        {
            Debug.Log("이미 로그인된 상태로 데이터베이스 화면");
        }
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        inventoryManager.OpenInventory();

        if (CurrentState == DatabaseMonitorState.Login)
        {
            _monitorDisplay.ResetLoginFields(); // 입력 필드 초기화
        }

        _monitorDisplay.ShowPanel(DatabaseMonitorState.None); // 현재 상태에 맞는 패널 표시
    }
    #endregion

    #region 로그인 처리
    /// <summary>
    /// 로그인 처리
    /// <para> - 로그인 성공 시: 메인 패널로 전환</para>
    /// <para> - 로그인 실패 시: 로그인 실패 텍스트 활성화</para>
    /// </summary>
    /// <returns>로그인 성공 여부</returns>
    public bool TryLogin()
    {
        SetInputLock(true); // 입력 잠금

        // 로그인 성공 여부 확인
        if (CurrentState == DatabaseMonitorState.Login)
        {
            if (_monitorDisplay.idText.text == CORRECT_ID && _monitorDisplay.pwText.text == CORRECT_PASSWORD)
            {
                Debug.Log("로그인 성공");
                CurrentState = DatabaseMonitorState.Main;

                _monitorDisplay.ResetLoginFields(); // 입력 필드 초기화
                _monitorDisplay.ShowPanel(CurrentState); // 메인 패널로 전환

                SetToggleMapEnabled(true); // Tab으로 맵 열기 가능
                SetInputLock(false); // 입력 잠금 해제
                return true;
            }
            else
            {
                Debug.Log("로그인 실패");

                _monitorDisplay.idText.text = ""; // 입력 필드 초기화
                _monitorDisplay.pwText.text = "";

                SetInputLock(false); // 입력 잠금 해제
                return false;
            }
        }
        SetInputLock(false); // 입력 잠금 해제
        return false;
    }
    #endregion

    #region 로그아웃 처리
    /// <summary>
    /// 로그아웃 처리
    /// <para> - 로그인 패널로 전환 </para>
    /// <para> - 로그인 화면에서는 지도 비활성화 </para>
    /// </summary>
    public void Logout()
    {
        CurrentState = DatabaseMonitorState.Login;

        _monitorDisplay.ResetLoginFields();
        _monitorDisplay.ShowPanel(CurrentState);

        SetToggleMapEnabled(false);
    }
    #endregion
}
