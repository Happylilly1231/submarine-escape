using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockwaveTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem shockwaveParticle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    private bool _isTriggered = false;
    private Coroutine _moveCoroutine = null;
    private Vector3 originOffset = new Vector3(0f, 0f, 3.5f);

    private Action _onAttackSuccess;
    private Action _onAttackMiss;

    /// <summary>
    /// 발사
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="rotation"></param>
    /// <param name="duration"></param>
    /// <param name="onHit"></param>
    /// <param name="onMiss"></param>
    public void Fire(Vector3 targetPos, Quaternion rotation, float duration, Action onHit = null, Action onMiss = null)
    {
        _isTriggered = false;
        _onAttackSuccess = onHit;
        _onAttackMiss = onMiss;

        transform.localPosition = originOffset;
        transform.localRotation = rotation;
        gameObject.SetActive(true);

        // 파티클 duration 설정 및 재생
        var particleMain = shockwaveParticle.main;
        particleMain.duration = duration;
        shockwaveParticle.Play();
        audioSource.Play();

        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(FireRoutine(targetPos, duration));
    }

    /// <summary>
    /// 발사 코루틴
    /// </summary>
    /// <param name="distance">거리</param>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator FireRoutine(Vector3 targetPos, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < duration)
        {
            while (GameManager.instance.IsPausing) yield return null;

            if (_isTriggered) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        HandleMiss();
    }

    /// <summary>
    /// 트리거 함수
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;

        if (other.CompareTag("Player"))
        {
            _isTriggered = true;
            AudioManager.Instance.PlayGlobalOneShot(hitSound);
            _onAttackSuccess?.Invoke();
            Deactivate();
        }
    }

    /// <summary>
    /// 놓쳤을 때
    /// </summary>
    private void HandleMiss()
    {
        if (_isTriggered) return;
        _isTriggered = true;

        _onAttackMiss?.Invoke();
        Deactivate();
    }

    /// <summary>
    /// 비활성화
    /// </summary>
    private void Deactivate()
    {
        // 이동 코루틴 실행 중이었으면 종료
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        transform.localPosition = originOffset; // 시작 위치로 초기화
        gameObject.SetActive(false); // 오브젝트 비활성화
        shockwaveParticle.Stop(); // 파티클 정지
        audioSource.Stop();
    }

    private void OnDisable()
    {
        audioSource.Stop();
    }
}
