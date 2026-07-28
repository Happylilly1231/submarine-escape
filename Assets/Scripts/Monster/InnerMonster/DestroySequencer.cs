using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using InnerMonsterStates;
using UnityEngine;

public class DestroySequencer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip explosionSound; // 폭발 소리
    [SerializeField] private AudioClip glassShatterSound; // 유리 깨지는 소리
    [SerializeField] private AudioClip beepSound; // 삐 소리
    [SerializeField] private AudioClip turnOffLightSound; // 불 꺼지는 소리

    [Header("Galley")]
    [SerializeField] private GameObject explosionParticleParent; // 폭발 파티클들 부모
    // [SerializeField] private GameObject oilBarrel; // 기름통


    [Header("Storage01")]
    [SerializeField] private GameObject[] samples; // 깨질 샘플


    public AlarmControlPanel alarmControlPanel;

    private Action _currentOnComplete = null;

    InnerMonsterController monster;

    private void Start()
    {
        alarmControlPanel = FindAnyObjectByType<AlarmControlPanel>();
    }

    public void DestroySequence(InnerMonsterController monster, Action onComplete = null)
    {
        this.monster = monster;
        monster.IsDestroySequencing = true;

        _currentOnComplete = onComplete;
        InnerMonsterController.OnAnimationRageDestroyAttackTouched += PlaySequence;

        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence);

        // 카메라를 현재 연출 카메라 위치로 즉시 이동 및 회전
        Camera.main.transform.SetPositionAndRotation(SubmarineInGameManager.instance.CurrentSequenceCameraPos.position, SubmarineInGameManager.instance.CurrentSequenceCameraPos.rotation);
    }

    public void PlaySequence(InnerMonsterController monster)
    {
        InnerMonsterController.OnAnimationRageDestroyAttackTouched -= PlaySequence;
        Sequence seq = null;

        // 파괴 모션과 함께 실행됨 (파괴 모션 실행 후 아님)
        switch (monster.currentDestroyObjType)
        {
            case EDestroyObjType.Equipment: // 현재 파괴될 장비를 파괴한 경우
                AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생
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

        // 파괴 연출이 완료되었을 때(실제로 파괴가 된 시점) 로직
        if (seq != null)
        {
            seq.OnComplete(() =>
            {
                // 탈출실과 기계실이 아닐 때만 -> 경보 패널에서 버튼 선택 불가능으로 변경
                if (SubmarineInGameManager.instance.CurrentAlertArea != AlertArea.EscapeRoom
                    && SubmarineInGameManager.instance.CurrentAlertArea != AlertArea.MachinerySpace)
                    alarmControlPanel.DeactivateAreaButton(SubmarineInGameManager.instance.CurrentAlertArea);

                _currentOnComplete?.Invoke();
                _currentOnComplete = null;
            });
        }
    }

    /// <summary>
    /// 파괴 연출 - 식당(Galley)
    /// </summary>
    /// <returns></returns>
    private Sequence DestroySequence_Galley()
    {
        // 플레이어 카메라가 괴물을 비추는 카메라로 전환되고, 괴물이 식당의 가스레인지를 부술 때 기름이 쏟아지며 폭발하는 모습만 딱 연출로 보여주면서 게임 오버

        Sequence seq = DOTween.Sequence();

        // 1.5초 동안 폭발 발생
        seq.AppendCallback(() =>
        {
            AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생

            // 폭발 발생 (폭발 파티클 재생 + 소리 재생)
            explosionParticleParent.SetActive(true);
            AudioManager.Instance.PlayGlobalOneShot(explosionSound);

        });
        seq.AppendInterval(1.5f); // 1.5초 동안 보여주기

        // 이후 게임 종료는 자동 처리

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

        // 1초 동안 샘플 깨지는 거 보여줌
        seq.AppendCallback(() =>
        {
            AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생

            AudioManager.Instance.PlayGlobalOneShot(glassShatterSound); // 유리 깨지는 소리 재생
            // 샘플 오브젝트들 비활성화
            foreach (var sample in samples)
            {
                if (sample != null)
                {
                    sample.SetActive(false);
                }
            }
        });
        seq.AppendInterval(1.5f);

        // 이후 파괴 로직 자동 처리

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

        // 2초 동안 엔진 고장난 거 보여줌
        seq.AppendCallback(() =>
        {
            AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생

            monster.currentDestroyObj.GetComponent<DieselEngine>().BrokeWithJumpscare(); // 디젤 엔진 컴포넌트의 점프스퀘어로 인한 고장 함수 호출 (영구 고장)
            AudioManager.Instance.PlayGlobalOneShot(turnOffLightSound); // 불 꺼지는 소리 재생
        });
        seq.AppendInterval(1.5f);

        // 이후 파괴 로직 자동 처리

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

                // 2초 동안 통신 장비 고장나는 거 보여줌
                seq.AppendCallback(() =>
                {
                    AudioManager.Instance.PlayGlobalOneShot(monster.destroyCompleteSound); // 파괴 완료 소리 재생

                    monster.currentDestroyObj.GetComponent<TelegraphKey>().BrokeWithJumpscare(); // 통신 장치 컴포넌트의 점프스퀘어로 인한 고장 함수 호출
                    AudioManager.Instance.PlayGlobalOneShot(beepSound); // 삐 소리 한번 재생
                });
                seq.AppendInterval(1.5f);

                break;
        }
        return seq;
    }
}
