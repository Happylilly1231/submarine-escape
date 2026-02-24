using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 퍼즐 컨트롤러(추상 클래스, 내용 바꾸고 싶으면 override)
/// <para>- 퍼즐 공통 로직 관리(퍼즐 시작, 퍼즐 종료, ESC 키 입력 시 퍼즐 종료, 마우스 좌표 변경 시 호버 검사(호버 필요한 퍼즐만), 입력 잠금 설정, 호버 검사 함수)</para>
/// <para>- 퍼즐 액션맵의 액션 변수 관리</para>
/// </summary>
public abstract class PuzzleController : MonoBehaviour
{
    public Transform viewPoint; // 카메라 위치

    // 액션
    private PlayerInput playerInput;
    private InputAction _click; // 클릭
    public InputAction Click => _click;
    private InputAction _point; // 마우스 좌표
    public InputAction Point => _point;
    private InputAction _exit; // 종료(ESC)
    public InputAction Exit => _exit;
    private InputAction _space; // 스페이스
    public InputAction Space => _space;
    protected InputAction KeyA { get; private set; }
    protected InputAction KeyD { get; private set; }

    public bool IsPuzzleStarted { get; private set; } // 현재 퍼즐 시작되었는지(활성화되는 시점 X, StartPuzzle이 실행되는 시점 O) 여부
    private bool isInputLocked = false; // 현재 입력 잠금 여부

    // 호버
    protected abstract bool IsHoverRequired { get; } // 호버 필요한지 여부
    protected abstract bool IsMouseRequired { get; } // 마우스 필요한지 여부
    private LayerMask hoverableLayerMask; // 호버 가능 레이어 마스크(Hoverable 레이어)
    private HoverInteractable _currentHover; // 현재 호버
    public HoverInteractable CurrentHover => _currentHover;

    // 이벤트
    public event Action OnPuzzleStarted; // 퍼즐 시작 이벤트(활성화 시점 X)
    public event Action OnPuzzleExited; // 퍼즐 종료 이벤트

    /// <summary>
    /// Awake - 퍼즐 액션맵의 액션 변수, Hoverable 레이어 마스크 가져오기
    /// </summary>
    public virtual void Start()
    {
        IsPuzzleStarted = false;

        playerInput = SubmarineInGameManager.instance.playerInput;

        _click = playerInput.actions["Click"];
        _point = playerInput.actions["Point"];
        _exit = playerInput.actions["Exit"];
        _space = playerInput.actions["Space"];
        KeyA = playerInput.actions["KeyA"];
        KeyD = playerInput.actions["KeyD"];

        hoverableLayerMask = LayerMask.GetMask("Hoverable");

        // if (IsHoverRequired)
        //     SetHoverObjsPuzzleController();
    }

    // /// <summary>
    // /// 호버 오브젝트들의 퍼즐 컨트롤러 할당 (호버 필요 없으면 비워두기)
    // /// </summary>
    // public abstract void SetHoverObjsPuzzleController();

    /// <summary>
    /// 퍼즐 활성화 - 퍼즐 포커스, 플레이어 외형 안 보이게, 뷰 포인트로 카메라 이동 후 퍼즐 시작
    /// </summary>
    /// <param name="viewPoint"></param>
    public virtual void ActivatePuzzle()
    {
        SubmarineInGameManager.instance.SetPuzzleFocus(true, IsMouseRequired);
        SubmarineInGameManager.instance.SetPlayerGeoActive(false);

        Sequence seq = DOTween.Sequence();
        seq.Append(Camera.main.transform.DOMove(viewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            StartPuzzle(); // 퍼즐 시작
        });
    }

    /// <summary>
    /// 퍼즐 시작 - 퍼즐 액션맵으로 전환, 입력 이벤트 구독(기본: Exit(ESC 키)), 마우스 좌표 이벤트 구독
    /// </summary>
    public virtual void StartPuzzle()
    {
        IsPuzzleStarted = true;
        playerInput.SwitchCurrentActionMap("Puzzle");
        _exit.performed += OnExit;
        if (IsHoverRequired) _point.performed += OnPoint; // 호버 필요할 때만 미리 구독

        OnPuzzleStarted?.Invoke();
    }

    /// <summary>
    /// 퍼즐 종료 - Player(기본) 액션맵으로 전환, 입력 이벤트 구독 해제(기본: Exit(ESC 키)), 마우스 좌표 이벤트 구독 해제, 퍼즐 포커스 해제, 플레이어 외형 보이게
    /// </summary>
    public virtual void ExitPuzzle()
    {
        IsPuzzleStarted = false;
        playerInput.SwitchCurrentActionMap("Player");
        _exit.performed -= OnExit;
        if (IsHoverRequired) _point.performed -= OnPoint; // 호버 필요할 때만 미리 구독해두었던 것 해제

        // 호버된 게 있다면 해제
        if (_currentHover != null)
        {
            _currentHover.OnHoverExit();
            _currentHover = null;
        }

        SubmarineInGameManager.instance.SetPuzzleFocus(false);
        SubmarineInGameManager.instance.SetPlayerGeoActive(true);

        OnPuzzleExited?.Invoke();
    }

    /// <summary>
    /// 종료(ESC) 키 누를 때 호출되는 함수 (기본 동작: 퍼즐 종료)
    /// </summary>
    /// <param name="context"></param>
    public virtual void OnExit(InputAction.CallbackContext context)
    {
        ExitPuzzle();
    }

    /// <summary>
    /// 마우스 좌표 변경 이벤트 함수
    /// </summary>
    /// <param name="context"></param>
    public virtual void OnPoint(InputAction.CallbackContext context)
    {
        if (!IsPuzzleStarted) return;

        Vector2 pointerPos = context.ReadValue<Vector2>();

        if (IsHoverRequired)
            CheckHover(pointerPos);
    }

    /// <summary>
    /// 입력 잠금 설정
    /// </summary>
    /// <param name="isLock">잠금 여부</param>
    public void SetInputLock(bool isLock)
    {
        if (isLock)
        {
            isInputLocked = true;
            playerInput.DeactivateInput(); // 모든 액션 비활성화
        }
        else
        {
            isInputLocked = false;
            playerInput.ActivateInput(); // 모든 액션 활성화
        }
    }

    /// <summary>
    /// 호버 검사(호버 가능한 오브젝트에 Hoverable 레이어 지정 필수!)
    /// </summary>
    public virtual void CheckHover(Vector2 mousePos)
    {
        // 입력 잠겨있으면 -> 호버 X
        if (isInputLocked)
            return;

        // 현재 마우스 좌표에서 레이를 쏴서 HoverInteractable 컴포넌트 가진 오브젝트 검사
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hoverableLayerMask)) // 기본적으로 Hoverable 레이어만 검사함
        {
            if (hit.collider.TryGetComponent<HoverInteractable>(out var hoverInteractable)) // 컴포넌트 있으면
            {
                // 현재 호버가 변경됐으면 -> 이전 호버 종료, 현재 호버 시작
                if (hoverInteractable != _currentHover)
                {
                    _currentHover?.OnHoverExit();
                    _currentHover = hoverInteractable;
                    _currentHover.OnHoverEnter();
                }
            }
            else // 컴포넌트 없으면 -> 호버 클리어
            {
                ClearHover();
            }
        }
        else // 현재 호버가 없으면 -> 호버 클리어
        {
            ClearHover();
        }
    }

    /// <summary>
    /// 호버 클리어 - 현재 호버 종료, null로 초기화
    /// </summary>
    private void ClearHover()
    {
        if (_currentHover != null)
        {
            _currentHover.OnHoverExit();
            _currentHover = null;
        }
    }
}