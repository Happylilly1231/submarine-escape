using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BackroomEntity : MonoBehaviour
{
    /// <summary>
    /// 플레이어 죽이기
    /// </summary>
    protected void KillPlayer()
    {
        BackroomManager.Instance.MoveToStartPos(); // 백룸 시작 위치로 이동
        Sequence seq = DOTween.Sequence();
        FocusManager.Instance.PopFocusState(); // 이전 포커스 복구
        // SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제
        seq.Append(FXManager.instance.fadeImage.DOFade(0f, 2f)); // 페이드 아웃
        // seq.OnComplete(() =>
        // {
        //     SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제
        // });
    }
}