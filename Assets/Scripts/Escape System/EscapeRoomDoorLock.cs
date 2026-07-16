using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class EscapeRoomDoorLock : InteractableBase
{
    [SerializeField] private Item escapeRoomDoorKeyItem; // 탈출실 문 열쇠 아이템
    [SerializeField] private Transform lockPartTransform;
    [SerializeField] private GameObject lockedObj;
    [SerializeField] private Door escapeRoomDoor;
    [SerializeField] private Transform viewPoint;

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public override string GetInteractText()
    {
        return IsRequiredItemSelected() ? "Unlock [E]" : "Locked (Need Key)";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 캐비넷을 열거나 닫음
    /// </summary>
    public override void Interact()
    {
        if (IsRequiredItemSelected())
        {
            Unlock(); // 잠금 해제
        }
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return item == escapeRoomDoorKeyItem;
    }

    private void Unlock()
    {
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 포커스
        SubmarineInGameManager.instance.InteractorUI.SetActive(false); // 상호작용 UI 비활성화

        Sequence seq = DOTween.Sequence();

        // 카메라 이동
        seq.Append(Camera.main.transform.DOMove(viewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));
        seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        // 자물쇠 풀림
        seq.Append(lockPartTransform.DOLocalMoveY(250f, 1.5f).SetRelative()
        .SetEase(Ease.InBounce));
        seq.OnComplete(() =>
            {
                escapeRoomDoor.isLocked = false;
                lockedObj.SetActive(false);
                SubmarineInGameManager.instance.InteractorUI.SetActive(true); // 상호작용 UI 활성화
                FocusManager.Instance.PopFocusState(); // 포커스 해제
            });
    }
}
