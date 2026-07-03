using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class ScenePortal : Door
{
    [SerializeField] private string sceneName; // 이동할 씬 이름
    [SerializeField] private Transform viewPoint; // 카메라 이동 위치

    public override void OpenDoor(float angle)
    {
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 연출 포커스 상태로 전환

        base.OpenDoor(angle);

        Sequence portalSeq = DOTween.Sequence();

        portalSeq.Append(Camera.main.transform.DOMove(viewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));

        portalSeq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        portalSeq.AppendInterval(0.5f);

        portalSeq.OnComplete(() =>
        {
            SceneManager.LoadScene(sceneName); // 씬 전환
        });
    }
}
