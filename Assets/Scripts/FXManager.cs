using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 효과 관리
/// <para>- 발광, 비네트 등</para>
/// </summary>
public class FXManager : MonoBehaviour
{
    [Header("기본")]
    [SerializeField] private Volume volume; // 볼륨
    private Bloom _bloom; // 발광
    private Vignette _vignette; // 비네트
    public Vignette Vignette => _vignette;
    private ChromaticAberration _chromatic;
    public ChromaticAberration Chromatic => _chromatic;
    private LensDistortion _distortion;
    public LensDistortion Distortion => _distortion;

    [Header("플레이어 폭주")]
    [SerializeField] private Image playerRageImage; // 플레이어 폭주 이미지
    private Coroutine _heartbeatCoroutine;

    [Header("투여 효과")]
    [SerializeField] private Volume injectionVolume;
    private Vignette _injectionVignette;
    [SerializeField] private AudioClip cureSuccessSound; // 치료 성공 소리
    [SerializeField] private AudioClip cureFailSound; // 치료 실패 소리(심장 박동)

    [Header("암전/비네트")]
    public Image fadeImage;

    // 싱글톤 변수
    public static FXManager instance;

    /// <summary>
    /// 싱글톤 구현
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 볼륨에서 각 효과 가져오기
        volume.profile.TryGet(out _bloom);
        volume.profile.TryGet(out _vignette);
        volume.profile.TryGet(out _chromatic);
        volume.profile.TryGet(out _distortion);

        // 투여 효과 볼륨 관련 초기화
        injectionVolume.weight = 0f;
        if (injectionVolume.profile.TryGet<Vignette>(out var vignette))
        {
            _injectionVignette = vignette;
        }

        // 초기 설정
        VignetteOff(); // 비네트 효과 끄기
    }

    /// <summary>
    /// 발광 효과 활성화
    /// </summary>
    /// <param name="intensity">발광 강도</param>
    public void BloomOn(float intensity)
    {
        _bloom.intensity.value = intensity;
    }

    /// <summary>
    /// 발광 효과 비활성화
    /// </summary>
    public void BloomOff()
    {
        _bloom.intensity.value = 0f;
    }

    /// <summary>
    /// 비네트 효과 활성화
    /// </summary>
    /// <param name="color">비네트 색</param>
    public void VignetteOn(Color color)
    {
        _vignette.color.value = color;
        _vignette.intensity.value = 0.5f;
        _vignette.smoothness.value = 0.5f;
    }

    /// <summary>
    /// 비네트 효과 비활성화
    /// </summary>
    public void VignetteOff()
    {
        _vignette.intensity.value = 0f;
    }

    /// <summary>
    /// 암전
    /// </summary>
    public void FadeOut(Color color, float duration, System.Action onComplete = null)
    {
        StartCoroutine(FadeOutCoroutine(color, duration, onComplete));
    }

    private IEnumerator FadeOutCoroutine(Color color, float duration, System.Action onComplete)
    {
        _vignette.color.value = color;
        _vignette.smoothness.value = 1f;

        float value = 0f;
        float speed = 1f / duration;
        while (value < 1f)
        {
            _vignette.intensity.value = value;
            value += speed * Time.deltaTime;
            yield return null;
        }
        _vignette.intensity.value = 1f;

        onComplete?.Invoke();
    }

    #region 심장 박동

    /// <summary>
    /// UI 심장 박동 연출 시작
    /// </summary>
    /// <param name="duration">연출이 지속될 시간 (0 이하이면 Stop할 때까지 무한 반복)</param>
    public void StartHeartbeatUI(float duration = -1f, float pulseSpeed = 5f, float pulseIntensity = 0.2f)
    {
        StopHeartbeatUI();
        playerRageImage.gameObject.SetActive(true);
        _heartbeatCoroutine = StartCoroutine(HeartbeatRoutine(duration, pulseSpeed, pulseIntensity));
    }

    /// <summary>
    /// UI 심장 박동 연출 강제 종료
    /// </summary>
    public void StopHeartbeatUI()
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }

        playerRageImage.rectTransform.localScale = Vector3.one; // 크기 원상복구
        playerRageImage.gameObject.SetActive(false);
    }

    private IEnumerator HeartbeatRoutine(float duration, float pulseSpeed = 5f, float pulseIntensity = 0.2f)
    {
        float elapsed = 0f;
        bool isInfinite = duration <= 0f;

        while (isInfinite || elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float scaleOffset = Mathf.Abs(Mathf.Sin(elapsed * pulseSpeed) * pulseIntensity);
            playerRageImage.rectTransform.localScale = new Vector3(1f + scaleOffset, 1f + scaleOffset, 1f);

            yield return null;
        }

        StopHeartbeatUI();
    }

    #endregion

    #region 투여 효과

    /// <summary>
    /// 투여 결과 효과 보여주기
    /// </summary>
    public void ShowInjectionResultEffect(bool isSuccess, System.Action onComplete = null)
    {
        if (injectionVolume == null || _injectionVignette == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 결과에 맞는 소리 재생
        AudioClip sound = isSuccess ? cureSuccessSound : cureFailSound;
        if (sound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobalOneShot(sound);
        }

        // 결과에 따른 색상 설정
        _injectionVignette.color.value = isSuccess ? Color.green : Color.red;

        Sequence seq = DOTween.Sequence();
        seq.Append(DOTween.To(() => injectionVolume.weight, x => injectionVolume.weight = x, 1f, 0.2f));
        seq.AppendInterval(1f); // 1초 동안 유지
        seq.Append(DOTween.To(() => injectionVolume.weight, x => injectionVolume.weight = x, 0f, 1f));

        // 콜백 실행
        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    #endregion
}
