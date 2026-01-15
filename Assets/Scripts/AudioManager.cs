using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역적인 오디오 재생과 공통으로 사용할 오디오 함수를 관리한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    /// <summary>
    /// 한 번만 재생되는 효과음 재생(오디오 매니저의 sfxSource에서 재생됨)
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip); // 한 번만 재생
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>
    /// 해당 오디오 소스에서 안전하게 재생(매 프레임 반복되는 곳에서 호출 시 한 번만 Play(Loop)되도록 함)
    /// </summary>
    /// <param name="audioSource">소리를 재생할 오디오 소스(Loop)</param>
    /// <param name="audioClip">오디오 클립</param>
    /// <param name="pitch">피치 - 기본: 1</param>
    public void PlaySoundSafe(AudioSource audioSource, AudioClip audioClip, float pitch = 1f)
    {
        // 현재 클립 재생 중 -> 중복으로 재생하지 않고 종료
        if (audioSource.clip == audioClip && audioSource.isPlaying)
            return;

        audioSource.clip = audioClip;
        audioSource.pitch = pitch; // 피치 설정 가능
        audioSource.Play();
    }
}
