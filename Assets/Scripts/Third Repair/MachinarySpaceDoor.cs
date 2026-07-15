// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class MachinerySpaceDoor : PuzzleController
// {
//     protected override bool IsHoverRequired => false;

//     protected override bool IsMouseRequiredAtFirst => true;

//     public bool IsRepaired { get; private set; } = false; // 수리 되었는지 여부

//     #region PuzzleController
//     public override void ActivatePuzzle()
//     {
//         SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화
//         itemEquipController.UnequipItem(); // 아이템 장착 해제
//         SubmarineInGameManager.instance.InteractorUI.SetActive(true); // 상호작용 UI 활성화

//         base.ActivatePuzzle();
//     }

//     public override void StartPuzzle()
//     {
//         base.StartPuzzle();

//         Click.performed += OnClickPerformed; // 클릭 performed 사용
//     }

//     public override void ExitPuzzle()
//     {
//         base.ExitPuzzle();

//         Click.performed -= OnClickPerformed;

//         SubmarineInGameManager.instance.SetActiveInGameUI(true); // 인게임 UI 활성화
//     }
//     #endregion

//     #region 입력 이벤트 함수
//     private void OnClickPerformed(InputAction.CallbackContext context)
//     {
//         Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
//         if (Physics.Raycast(ray, out RaycastHit hit))
//         {
//             for (int i = 0; i < areaImgs.Length; i++)
//             {
//                 if (areaImgs[i] == hit.collider.gameObject)
//                 {
//                     // 해당 구역 경보 발생
//                     AlertArea alertArea = (AlertArea)(i + 1);
//                     if (alertArea == AlertArea.ControlRoom)
//                     {
//                         SubmarineInGameManager.instance.AlertOn(alertArea, 3);
//                     }
//                     else
//                     {
//                         SubmarineInGameManager.instance.AlertOn(alertArea);
//                     }

//                 }
//             }
//         }
//     }
//     #endregion
// }
