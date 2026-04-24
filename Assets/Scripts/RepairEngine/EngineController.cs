using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class EngineController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => false;

    private DieselEngine _targetEngine; // 수리할 엔진

    [SerializeField] private Transform itemViewRoot; // 아이템 뷰 위치

    [Header("수리에 필요한 아이템")]
    public Item hammerItem; // 망치 아이템

    [Header("UI")]
    [SerializeField] private GameObject inventoryUI; // 인벤토리 UI

    public int currentRepairCount { get; private set; } = 0; // 현재 수리 횟수

    private RepairEngineQTE _repairEngineQTE; // 수리 QTE 참조
    private bool _isActionProcessing = false;

    public override void Start()
    {
        base.Start();
        _repairEngineQTE = FindObjectOfType<RepairEngineQTE>();
    }

    /// <summary>
    /// 수리할 엔진 등록 및 뷰포인트 설정
    /// </summary>
    public void SetTargetEngine(DieselEngine engine)
    {
        _targetEngine = engine;
        this.viewPoint = engine.viewPoint; // 뷰포인트 설정
    }

    #region 퍼즐 시작/종료

    /// <summary>
    /// 퍼즐 활성화 - 엔진 시각 효과 설정, 인벤토리 UI 숨김, 퍼즐 시작
    /// </summary>
    public override void ActivatePuzzle()
    {
        _targetEngine.SetVisuals(true); // 엔진 시각 효과 설정
        inventoryUI.SetActive(false);

        base.ActivatePuzzle();
    }
    /// <summary>
    /// 퍼즐 시작 - QTE 시작
    /// </summary>
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        Space.performed += OnSpace; // 스페이스 사용

        _repairEngineQTE.StartQTE(0); // QTE 시작
    }
    /// <summary>
    /// 퍼즐 종료 - 엔진 시각 효과 설정, QTE 숨김, 퍼즐 종료
    /// <para> - 수리가 완전히 안 된 경우 수리 단계 초기화 </para>
    /// <para> - 퍼즐 종료 후 인벤토리 UI 다시 보여주기 </para>
    ///
    /// </summary>
    public override void ExitPuzzle()
    {
        if (_isActionProcessing)
        {
            Debug.Log("모션 진행 중에는 퍼즐을 나갈 수 없습니다.");
            return;
        }

        if (!_targetEngine.IsComplete) _targetEngine.currentRepairStep = 0; // 현재 수리 단계 초기화
        _targetEngine.SetVisuals(false); // 엔진 시각 효과 설정
        _repairEngineQTE.HideQTE(); // QTE 숨김

        base.ExitPuzzle();
        Space.performed -= OnSpace; // 스페이스 사용 해제

        inventoryUI.SetActive(true);
    }
    #endregion

    #region 입력 이벤트
    /// <summary>
    /// 스페이스 입력 이벤트 - QTE 성공 시 엔진 수리 단계 증가 및 망치 휘두르는 애니메이션 재생, QTE 실패 시 수리 실패 처리
    /// </summary>
    /// <param name="context"></param>
    private void OnSpace(InputAction.CallbackContext context)
    {
        if (!IsPuzzleStarted || _targetEngine == null || _isActionProcessing) return;

        _isActionProcessing = true;
        if (_repairEngineQTE.ExecuteHit()) // QTE 성공 시
        {
            _targetEngine.currentRepairStep++; // 엔진 수리 단계 증가
            PlayHammerSwing(); // 망치 휘두르는 애니메이션 재생

            if (_targetEngine.IsComplete) // 엔진 수리가 완전히 된 경우
            {
                Debug.Log("엔진 수리 완료!");
                currentRepairCount++; // 수리된 엔진 수 증가
                Invoke(nameof(ExitPuzzle), 1.0f); // 퍼즐 종료
            }
            else // 아직 수리가 완전히 안 된 경우 다음 QTE 시작
            {
                Invoke(nameof(NextQTE), 1.0f);
            }
        }
        else // QTE 실패 시
        {
            Debug.Log("QTE 실패!");
            HandleRepairFailure();
        }
    }
    #endregion

    /// <summary>
    /// 다음 QTE 시작 - 현재 수리 단계에 맞는 QTE 설정으로 시작
    /// </summary>
    private void NextQTE() => _repairEngineQTE.StartQTE(_targetEngine.currentRepairStep);

    #region 애니메이션
    /// <summary>
    /// 망치 휘두르는 애니메이션 - 3회 타격으로 구성, 타격마다 카메라 흔들림 효과 포함, 마지막 타격 시 엔진 연기 업데이트 호출
    /// </summary>
    private void PlayHammerSwing()
    {
        if (itemViewRoot == null) return;

        // 초기 상태 저장
        Vector3 originalRot = itemViewRoot.localEulerAngles;
        Vector3 originalPos = itemViewRoot.localPosition;

        Sequence swingSeq = DOTween.Sequence();

        // --- 1회차 타격 (톡!) ---
        swingSeq.Append(itemViewRoot.DOLocalRotate(new Vector3(-15, 0, 0), 0.1f).SetRelative()) // 살짝 들기
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(20, 0, 0), 0.05f).SetRelative().SetEase(Ease.InQuad)) // 툭!
                      .AppendCallback(() => Camera.main.transform.DOShakePosition(0.05f, 0.03f)) // 미세 흔들림
                      .Append(itemViewRoot.DOLocalRotate(originalRot, 0.1f)); // 복구

        // --- 2회차 타격 (톡!) ---
        swingSeq.AppendInterval(0.05f) // 타격 사이 아주 짧은 간격
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(-15, 0, 0), 0.1f).SetRelative())
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(20, 0, 0), 0.05f).SetRelative().SetEase(Ease.InQuad)) // 툭!
                      .AppendCallback(() => Camera.main.transform.DOShakePosition(0.05f, 0.03f))
                      .Append(itemViewRoot.DOLocalRotate(originalRot, 0.1f));

        // --- 3회차 타격 (콰앙!) ---
        swingSeq.AppendInterval(0.1f) // 마지막 타격 전 약간의 딜레이로 강조
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(-50, 0, 0), 0.15f).SetRelative().SetEase(Ease.OutQuad)) // 크게 들기
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(75, 0, 0), 0.05f).SetRelative().SetEase(Ease.InExpo)) // 콰앙!
                      .Join(itemViewRoot.DOLocalMoveZ(0.25f, 0.05f).SetRelative()) // 앞으로 깊게 찌르기
                      .OnComplete(() =>
                      {
                          // 강한 흔들림 및 최종 복구
                          Camera.main.transform.DOShakePosition(0.15f, 0.1f);
                          itemViewRoot.DOLocalRotate(originalRot, 0.2f);
                          itemViewRoot.DOLocalMove(originalPos, 0.2f);

                          _targetEngine.UpdateSmoke(); // 연기 업데이트

                          _isActionProcessing = false;
                      });

    }

    /// <summary>
    /// 수리 실패 처리 - 카메라 흔들림, 실패 효과 재생, 퍼즐 종료
    /// </summary>
    private void HandleRepairFailure()
    {
        Camera.main.transform.DOShakePosition(0.4f, 0.2f, 20); // 실패 시 카메라 흔들림

        _targetEngine.PlayFailureEffect();

        _isActionProcessing = false;

        Invoke(nameof(ExitPuzzle), 0.8f); // 퍼즐 종료
    }
    #endregion
}
