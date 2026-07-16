using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DestroySequencer : MonoBehaviour
{
    [SerializeField] private GameObject[] samples; // 깨질 샘플
    [SerializeField] private GameObject explosionParticle; // 폭발 파티클
    private AlarmControlPanel _alarmControlPanel;

    InnerMonsterController monster;

    private void Start()
    {
        _alarmControlPanel = FindAnyObjectByType<AlarmControlPanel>();
    }

    public void DestroySequence(InnerMonsterController monster, Action onComplete = null)
    {
        this.monster = monster;
        monster.IsDestroySequencing = true;

        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence);

        // 카메라를 현재 연출 카메라 위치로 즉시 이동 및 회전
        Camera.main.transform.SetPositionAndRotation(SubmarineInGameManager.instance.CurrentSequenceCameraPos.position, SubmarineInGameManager.instance.CurrentSequenceCameraPos.rotation);

        Sequence seq = null;

        switch (monster.currentDestroyObjType)
        {
            case EDestroyObjType.EscapeRoomDoor: // 탈출실 문을 파괴한 경우
                seq = DestroySequence_EscapeRoomDoor();
                break;

            case EDestroyObjType.MachinerySpaceDoor: // 기계실 문을 파괴한 경우
                seq = DestroySequence_MachinerySpaceDoor();
                break;

            case EDestroyObjType.Equipment: // 현재 파괴될 장비를 파괴한 경우
                switch (SubmarineInGameManager.instance.CurrentAlertArea)
                {
                    // 장치 이미 부순 경우도 처리 잘하기!!!
                    case AlertArea.Galley:
                        seq = DestroySequence_Galley();
                        break;
                    case AlertArea.Storage01:
                        seq = DestroySequence_Storage01();
                        break;
                    case AlertArea.EngineRoom:
                        seq = DestroySequence_EngineRoom();
                        break;
                    case AlertArea.ControlRoom:
                        seq = DestroySequence_ControlRoom();
                        break;
                }
                break;
        }

        if (seq != null)
        {
            seq.OnComplete(() =>
            {
                _alarmControlPanel.DeactivateAreaButton(SubmarineInGameManager.instance.CurrentAlertArea);
                onComplete?.Invoke();
            });
        }

    }

    /// <summary>
    /// 파괴 연출 - 탈출실 문(EscapeRoomDoor)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_EscapeRoomDoor()
    {
        // 탈출실 문 파괴하고 괴물이 들어가서 포효하고 끝나는 연출

        Sequence seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            monster.Nav.enabled = false;
            monster.transform.DOMove(SubmarineInGameManager.instance.CurrentDestroyPos.position, 1f); // 탈출실 안으로 이동
            monster.Nav.enabled = true;
        });
        seq.AppendInterval(1f);
        seq.AppendCallback(() =>
        {
            // 회전?
            // RageStart 애니메이션 즉시 실행 
            monster.Animator.Play("Rage StateMachine.RageStart");
        });
        seq.AppendInterval(1f); // 1초 보여주고 게임 오버 엔딩
        return seq;
    }

    /// <summary>
    /// 파괴 연출 - 기계실 문(MachinerySpaceDoor)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_MachinerySpaceDoor()
    {
        // 괴물을 바라보고 괴물에게 공격당해 죽는 연출

        Sequence seq = DOTween.Sequence();

        return seq;
    }

    /// <summary>
    /// 파괴 연출 - 식당(Galley)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_Galley()
    {
        // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 식당의 가스레인지를 부술 때 기름이 쏟아지며 폭발하는 모습만 딱 연출로 보여주면서 게임 오버

        Sequence seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            Debug.Log("폭발!!!");
            // 폭발 파티클 재생 + 소리 재생
        });
        seq.AppendInterval(3f); // 3초 동안 보여주기
        return seq;
    }

    /// <summary>
    /// 파괴 연출 - 창고01(Sotrage01)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_Storage01()
    {
        // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 창고01의 샘플병을 부수며 깨지는 소리나는 모습만 딱 연출로 잠깐 보여주고 다시 플레이어 카메라로 돌아옴

        Sequence seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            // 샘플 몇 개 비활성화
            // 깨지는 소리 재생
        });
        seq.AppendInterval(3f);
        return seq;
    }

    /// <summary>
    /// 파괴 연출 - 엔진실(EngineRoom)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_EngineRoom()
    {
        // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 엔진실의 엔진을 부수면서 파지직 소리와 함께 불이 다 꺼지는 걸 연출로 보여주고 다시 플레이어 카메라로 돌아옴

        Sequence seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            // 엔진 고장 상태로 변경
            // 불 끄기
            // 복구 안되게 처리
        });
        return seq;
    }

    /// <summary>
    /// 파괴 연출 - 조종실(ControlRoom)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_ControlRoom()
    {
        Sequence seq = DOTween.Sequence();
        switch (SubmarineInGameManager.instance.CurrentDestroyEquipmentIndex)
        {
            case 3: // 통신 장치
                    // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 조종실의 통신 장비를 부수면서 삐삐삐— 소리와 함께 망가지고 통신기가 검정색으로 되는 모습을 연출로 보여주고 다시 플레이어 카메라로 돌아옴
                    // 이미 부순 경우 부수는 모션만!!!
                seq.AppendCallback(() =>
                {
                    // 통신 장비 고장 처리
                    // 삐삐삐 소리 한 번 재생
                    // 통신기 material 검정색으로 변경?
                });
                break;
        }
        return seq;
    }
}
