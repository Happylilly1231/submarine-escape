using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMutation : MonoBehaviour
{
    [SerializeField] private GameObject[] spikes;
    [SerializeField] private Transform spawnSpikeViewPoint; // 가시 생성될 때 3인칭으로 보는 위치
    [SerializeField] private Transform hitViewPoint; // 가시가 바닥을 칠 때 바닥 바라보도록 하는 위치
    [SerializeField] private Image vignetteImg;
    [SerializeField] private Transform monsterHandsTransform; // 괴물 양손 트랜스폼
    [SerializeField] private GameObject itemOverlayCamera; // 아이템 든 거 보여주는 카메라

    private float _mutationTimer = 0f;
    private float _mutationInterval = 600f; // 10분 = 600초
    private int _currentStage = 1; // 1 ~ 5단계 (게임 시작 시 1단계)
    private int _maxStage = 5;

    private float[] _vignetteTimes = { 1f, 2f, 5f, 10f };
    private float[] _temperatures = { 37f, 38f, 40f, 42f, 44f };

    public static event Action<Vector3> OnSpikeHitFloor; // 가시가 바닥을 쳤을 때 이벤트 
    public static event Action OnMutationCompleted; // 괴물화 완료 이벤트

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioSource mutationAudioSource;
    [SerializeField] private AudioClip spikeHitSound; // 가시로 바닥을 쾅 치는 소리
    [SerializeField] private AudioClip tinnitusSound; // 이명 소리

    private void OnEnable()
    {
        LightingManager.instance.OnLightTurnedOn += ReactLightOrSound;
        SubmarineInGameManager.instance.OnAlertStarted += ReactLightOrSound;
    }

    private void Start()
    {
        // 가시들 비활성화
        foreach (var spike in spikes)
        {
            spike.SetActive(false);
        }

        // 비네트 이미지 비활성화
        vignetteImg.gameObject.SetActive(false);

        // 괴물 손 비활성화
        monsterHandsTransform.gameObject.SetActive(false);

        GameTime.Instance.ReserveEvent(_mutationInterval, SpawnSpike, true);
    }

    // private void Update()
    // {
    //     // 정지 중 -> 아무것도 X
    //     if (SubmarineInGameManager.instance.IsPausing)
    //         return;

    //     // 타이머 증가
    //     _mutationTimer += Time.deltaTime;

    //     // 10분마다 단계 증가
    //     if (_mutationTimer >= _mutationInterval && _currentStage < _maxStage)
    //     {
    //         _currentStage++; // 단계 증가
    //         _mutationTimer = 0f; // 타이머 초기화

    //         // 가시 생성
    //         SpawnSpike();
    //     }
    // }

    /// <summary>
    /// 완전 괴물화
    /// </summary>
    private void CompleteMutation()
    {
        // 완전 괴물화 이벤트 알림 -> 괴물이 더 이상 플레이어를 추적 & 공격 대상으로 여기지 않도록 함
        OnMutationCompleted?.Invoke();

        // 포효 소리 추가 예정!!!

        Sequence seq = DOTween.Sequence();

        // 플레이어 포효 (마구 움직임)
        seq.Append(SubmarineInGameManager.instance.player.transform
            .DORotate(new Vector3(0, 15f, 0), 0.1f)
            .SetRelative()
            .SetLoops(20, LoopType.Yoyo)
            .SetEase(Ease.InOutFlash));

        seq.AppendCallback(() =>
        {
            // 1인칭 시점으로 바뀌고 아래 보도록 클로즈업
            Camera.main.transform.position = hitViewPoint.position;
            Camera.main.transform.rotation = hitViewPoint.rotation;

            // 괴물 손 활성화
            monsterHandsTransform.gameObject.SetActive(true);
        });

        // 손 시야 밖에서 서서히 가운데로 나옴
        seq.Append(monsterHandsTransform.DOLocalMoveZ(0f, 3f)).SetEase(Ease.InSine);

        // 완전 암전 & 이명 일정 시간 지속
        seq.AppendCallback(() =>
        {
            FXManager.instance.FadeOut(Color.black, 3f, () =>
            {
                mutationAudioSource.Stop();

                // 게임 오버
                GameManager.instance.GameOver(EEndingType.MonsterDeath); // 괴물화 엔딩인데 아직 없어서 일단 괴물에게 죽음으로 함
            });
            AudioManager.Instance.PlaySoundSafe(mutationAudioSource, tinnitusSound); // 이명 소리 재생
        });
    }

    /// <summary>
    /// 가시 생성
    /// </summary>
    private void SpawnSpike()
    {
        _currentStage++; // 단계 증가

        // 현재 가시
        GameObject spike = spikes[_currentStage - 2];

        // 포커스
        SubmarineInGameManager.instance.SetFocus(true);

        // 3인칭 시점으로 즉시 바뀌고 가시가 생성되는 등을 비춤
        Camera.main.transform.position = spawnSpikeViewPoint.position;
        Camera.main.transform.rotation = spawnSpikeViewPoint.rotation;

        itemOverlayCamera.SetActive(false); // 아이템 든 거 비추는 카메라 비활성화 (3인칭에서는 우하단에 아이템이 보이지 않으므로)

        // 1초 기다리기
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(1f);

        // 가시 생성(나중에 쑥 나오는 식으로 바꿀 예정, 지금은 그냥 딱 활성화됨) & 가시 마구 움직임 
        seq.AppendCallback(() =>
        {
            spike.SetActive(true);
            spike.GetComponentInChildren<Animator>().SetTrigger("Rage");
        });

        // 2초 기다리기 (등 가시 움직이는 거 보여주는 중)
        seq.AppendInterval(2f);

        seq.OnComplete(() =>
        {
            if (_currentStage == _maxStage) // 최종 단계 -> 바닥 치지 않고 그냥 나감
                CompleteMutation();
            else
                HitFloor(spike); // 최종 단계 아님 -> 바닥 치기
        });
    }

    /// <summary>
    /// 바닥 치기
    /// </summary>
    /// <param name="spike">가시</param>
    private void HitFloor(GameObject spike)
    {
        // 바닥 치기 애니메이션 재생
        spike.GetComponentInChildren<Animator>().SetTrigger("Hit");

        // 1인칭 시점으로 바뀌고 가시가 칠 바닥 즉시 클로즈업
        Camera.main.transform.position = hitViewPoint.position;
        Camera.main.transform.rotation = hitViewPoint.rotation;

        itemOverlayCamera.SetActive(true); // 아이템 든 거 비추는 카메라 활성화 (1인칭으로 다시 돌아왔으므로)

        OnSpikeHitFloor?.Invoke(transform.position + Vector3.forward * 0.5f); // 이벤트 알림

        AudioManager.Instance.PlayGlobalOneShot(spikeHitSound); // 쾅 소리 재생

        Sequence seq = DOTween.Sequence();

        // 카메라 흔들림
        seq.Append(Camera.main.transform.DOShakePosition(1f, 0.3f, 10, 90f));

        // 완료되면 포커스 종료 
        seq.OnComplete(() =>
        {
            SubmarineInGameManager.instance.SetFocus(false);
        });
    }

    /// <summary>
    /// 빛이나 소리에 대한 반응
    /// </summary>
    private void ReactLightOrSound()
    {
        if (_currentStage == 0)
            return;

        // 초기 설정
        vignetteImg.gameObject.SetActive(true);
        float scale = _maxStage - _currentStage;
        vignetteImg.rectTransform.localScale = new Vector3(scale, scale, 1f);

        // 이명 소리 재생
        mutationAudioSource.volume = 0f; // 볼륨 0으로 설정
        AudioManager.Instance.PlaySoundSafe(mutationAudioSource, tinnitusSound);

        Sequence seq = DOTween.Sequence();

        // 1초 페이드 인 - 비네트 알파 & 이명 볼륨
        seq.Append(vignetteImg.DOFade(1f, 1f)).SetEase(Ease.InQuad);
        seq.Join(mutationAudioSource.DOFade(1f, 1f));

        // 지속
        seq.AppendInterval(_vignetteTimes[_currentStage - 1]);

        // 1초 페이드 아웃 - 비네트 알파 & 이명 볼륨
        seq.Append(vignetteImg.DOFade(0f, 1f)).SetEase(Ease.InQuad);
        seq.Join(mutationAudioSource.DOFade(0f, 1f));

        // 종료 처리
        seq.OnComplete(() =>
        {
            mutationAudioSource.Stop(); // 이명 소리 정지
            vignetteImg.gameObject.SetActive(false); // 비네트 이미지 비활성화
            mutationAudioSource.volume = 1f; // 볼륨 다시 1로 초기화
        });
    }

    /// <summary>
    /// 현재 체온 반환
    /// </summary>
    /// <returns>현재 체온</returns>
    public float GetCurrentTemperature()
    {
        if (_currentStage == _maxStage) return 44.0f;

        return Mathf.Floor(Mathf.Lerp(_temperatures[_currentStage - 1], _temperatures[_currentStage], _mutationTimer / _mutationInterval) * 10f) / 10f;
    }

    private void OnDisable()
    {
        LightingManager.instance.OnLightTurnedOn -= ReactLightOrSound;
        SubmarineInGameManager.instance.OnAlertStarted -= ReactLightOrSound;
    }
}

