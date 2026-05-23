// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public enum FocusPriority
// {
//     None = 0, // 일반
//     Trigger = 20, // 트리거 연출
//     Special = -10, // 특별 연출 (괴물화 가시 생성 등)
// }

// public class FocusManager : MonoBehaviour
// {
//     private Action _bestAction;
//     private FocusPriority _highestPriority = FocusPriority.None;
//     private bool _isProcessing = false; // 현재 연출 중인지 체크
//     private bool _isCurrentRequestPuzzleSubFocus = false; // 현재 요청이 퍼즐 부속 포커스인지 여부
//     private PuzzleController _currentRequestPuzzleController; // 현재 요청을 보낸 퍼즐 컨트롤러

//     public PuzzleController CurrentPuzzleController { get; private set; } = null;

//     private PlayerInteractor _playerInteractor;
//     private PlayerCameraController _playerCameraController;
//     private PlayerMove _playerMove;

//     // 요청 데이터를 담는 간단한 구조체
//     private class FocusRequest
//     {
//         public Action Action;
//         public FocusPriority Priority;
//         public PuzzleController RequestPuzzleController;
//         public bool IsPuzzleSubFocus;
//     }

//     // 요청들을 담아둘 리스트 (우선순위 큐 역할)
//     private List<FocusRequest> _requestQueue = new List<FocusRequest>();
//     private bool _isBusy = false;

//     public static FocusManager Instance { get; private set; }

//     private void Awake() => Instance = this;

//     void Start()
//     {
//         _playerInteractor = SubmarineInGameManager.instance.player.GetComponent<PlayerInteractor>(); // 플레이어 인터랙터 컴포넌트 가져오기
//         _playerMove = SubmarineInGameManager.instance.player.GetComponent<PlayerMove>(); // 플레이어 이동 컴포넌트 가져오기
//     }

//     /// <summary>
//     /// 포커스 요청 - 들어온 요청 중 가장 높은 우선순위 요청 선택
//     /// </summary>
//     /// <param name="action">포커스 후 실행할 액션(포커스 해제 함수를 마지막에 반드시 포함!)</param>
//     /// <param name="puzzleController">퍼즐 컨트롤러</param>
//     /// <param name="isPuzzleSubFocus">퍼즐 부속 포커스 여부 - 퍼즐 실행 중에 퍼즐 안에서 실행되는 포커스인지 여부</param>
//     /// <param name="priority">우선순위</param>
//     public void RequsetFocus(Action action, PuzzleController puzzleController = null, FocusPriority priority = FocusPriority.None, bool isPuzzleSubFocus = false)
//     {
//         _requestQueue.Add(new FocusRequest
//         {
//             Action = action,
//             RequestPuzzleController = puzzleController,
//             Priority = priority,
//             IsPuzzleSubFocus = isPuzzleSubFocus
//         });

//         // 우선순위 높은 순서대로 정렬 (점수가 같으면 먼저 들어온 게 위로 감)
//         _requestQueue.Sort((a, b) => b.Priority.CompareTo(a.Priority));

//         // // 가장 우선순위가 높은 액션으로 갱신
//         // if (priority > _highestPriority)
//         // {
//         //     _highestPriority = priority;
//         //     _bestAction = action;
//         //     _currentRequestPuzzleController = puzzleController; // 현재 요청 보낸 퍼즐 컨트롤러 갱신
//         //     _isCurrentRequestPuzzleSubFocus = isPuzzleSubFocus;
//         // }
//     }

//     /// <summary>
//     /// 다른 스크립트의 Update가 전부 실행된 후 실행 (모든 요청을 받아보고 그 중 가장 우선순위가 높은 요청을 선택해야 하므로)
//     /// </summary>
//     private void LateUpdate()
//     {
//         // if (_bestAction != null) // 우선순위 가장 높은 액션이 있으면
//         // {
//         //     if (_currentRequestPuzzleController != null) // 퍼즐 포커스의 경우
//         //         SetPuzzleFocus(true, _currentRequestPuzzleController); // 퍼즐 포커스 실행
//         //     else // 일반 포커스의 경우
//         //         SetFocus(true, _isCurrentRequestPuzzleSubFocus); // 포커스 실행

//         //     _bestAction.Invoke();
//         //     _bestAction = null;
//         //     _currentRequestPuzzleController = null; // 요청 보낸 퍼즐 컨트롤러도 초기화
//         //     _highestPriority = FocusPriority.None;
//         // }
//         // 연출 중이 아니고, 대기 중인 요청이 있다면?
//         if (!_isBusy && _requestQueue.Count > 0)
//         {
//             ExecuteNext();
//         }
//     }

//     private void ExecuteNext()
//     {
//         _isBusy = true;

//         // 가장 앞에 있는(우선순위 높은) 놈을 꺼내서 실행
//         var next = _requestQueue[0];
//         _requestQueue.RemoveAt(0);

//         if (next.RequestPuzzleController != null) // 퍼즐 포커스의 경우
//             SetPuzzleFocus(true, next.RequestPuzzleController); // 퍼즐 포커스 실행
//         else // 일반 포커스의 경우
//             SetFocus(true, next.IsPuzzleSubFocus); // 포커스 실행

//         next.Action.Invoke();
//     }

//     /// <summary>
//     /// 퍼즐 외 포커스 여부 설정(괴물화 가시 생성 보여줄 때나 심해 괴물로 인한 카메라 흔들림 등에 사용)
//     /// </summary>
//     /// <param name="isFocus"></param>
//     public void SetFocus(bool isFocus, bool isPuzzleSubFocus = false)
//     {
//         GameTime.Instance.SetPause(isFocus); // 포커스 -> 게임 시간 정지

//         // 현재 퍼즐 진행 중이었다면 & 퍼즐 부속 포커스가 아니라면 -> 그 퍼즐 종료
//         if (CurrentPuzzleController != null && !isPuzzleSubFocus)
//         {
//             CurrentPuzzleController.ExitPuzzle();
//             CurrentPuzzleController = null;
//         }

//         // 해제는 포커스와 반대로 작동
//         SubmarineInGameManager.instance.SetCameraControllerEnable(!isFocus); // 포커스 -> 카메라 조작 불가
//         _playerMove.SetMoveable(!isFocus); // 포커스 -> 플레이어 이동 불가능

//         if (isFocus) // 포커스
//         {
//             // 상호작용 감지 텍스트 클리어
//             _playerInteractor.ClearDetectionText();

//             SubmarineInGameManager.instance.playerInput.DeactivateInput(); // 모든 액션 비활성화
//         }
//         else
//         {
//             SubmarineInGameManager.instance.playerInput.ActivateInput(); // 모든 액션 활성화
//         }
//     }

//     /// <summary>
//     /// 퍼즐용 포커스 여부 설정
//     /// </summary>
//     /// <param name="isFocus">포커스 여부</param>
//     public void SetPuzzleFocus(bool isFocus, PuzzleController puzzleController = null)
//     {
//         if (isFocus)
//             CurrentPuzzleController = puzzleController;
//         else
//             CurrentPuzzleController = null;

//         // 해제는 포커스와 반대로 작동
//         _playerInteractor.IsPuzzleActive = isFocus; // interactor의 퍼즐 상호작용 여부는 포커스 여부와 동일하게 설정
//         _playerCameraController.enabled = !isFocus; // 포커스 -> 카메라 조작 불가
//         _playerMove.SetMoveable(!isFocus); // 포커스 -> 플레이어 이동 불가능

//         if (isFocus) // 포커스
//         {
//             // 상호작용 감지 텍스트 클리어
//             _playerInteractor.ClearDetectionText();
//         }
//         else // 포커스 해제
//         {
//             // 커서 보여야 한다고 되어 있었으면 -> 커서 보이지 않아도 됨으로 설정(퍼즐에서 플레이로 돌아가니까), 커서 숨기기
//             if (GameManager.instance.HaveToShowCursor)
//             {
//                 GameManager.instance.SetHaveToShowCursor(false);
//                 GameManager.instance.SetCursorVisible(false);
//             }
//         }
//     }
// }