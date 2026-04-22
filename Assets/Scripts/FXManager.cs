using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Volume volume; // 볼륨
    private Bloom _bloom; // 발광
    private Vignette _vignette; // 비네트
    public Vignette Vignette => _vignette;
    private ChromaticAberration _chromatic;
    public ChromaticAberration Chromatic => _chromatic;
    private LensDistortion _distortion;
    public LensDistortion Distortion => _distortion;
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
}
