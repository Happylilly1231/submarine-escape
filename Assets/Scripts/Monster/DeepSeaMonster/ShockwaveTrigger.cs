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
    private Transform _originParent; // 원래 부모(괴물) 저장용

    private Action _onAttackSuccess;
    private Action _onAttackMiss;

    private void Awake()
    {
        // 시작 시 원래 부모 트랜스폼 기억해 두기
        _originParent = transform.parent;
    }

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
        // 혹시 부모가 분리된 상태였다면 원래 부모 밑으로 복구
        if (_originParent != null && transform.parent != _originParent)
        {
            transform.SetParent(_originParent);
        }

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
    /// 충격파 반사
    /// </summary>
    public void ReflectShockwave(Transform target, float duration = 1f, Action onReflectHit = null)
    {
        Debug.Log("충격파 반사!");

        if (target == null) return;

        // 1. 기존 플레이어 피격/빗나감 판정 비활성화
        _isTriggered = true;

        // 2. 기존 이동 코루틴 정지
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        // 3. 부모(괴물)가 움직여도 궤적이 흔들리지 않도록 월드 공간으로 잠시 분리
        if (_originParent == null) _originParent = transform.parent;
        transform.SetParent(null);

        // 4. 파티클은 그대로 유지하되, 반사 비행시간 동안 꺼지지 않도록 재생 보장
        if (!shockwaveParticle.isPlaying)
        {
            shockwaveParticle.Play();
        }

        // 5. 소리 처음부터 다시 재생
        audioSource.Stop();
        audioSource.Play();

        // 6. 반사 이동 코루틴 시작
        _moveCoroutine = StartCoroutine(ReflectRoutine(target, duration, onReflectHit));
    }

    /// <summary>
    /// 반사 이동 코루틴
    /// </summary>
    private IEnumerator ReflectRoutine(Transform target, float duration, Action onReflectHit)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            while (GameManager.instance.IsPausing) yield return null;

            if (target == null) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 currentTargetPos = target.position;
            transform.position = Vector3.Lerp(startPos, currentTargetPos, t);

            // 타겟 방향으로 회전 정렬
            Vector3 dir = currentTargetPos - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            yield return null;
        }

        // 타겟에 도달(명중) 처리
        AudioManager.Instance.PlayGlobalOneShot(hitSound);
        onReflectHit?.Invoke();
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

        // 분리되었던 부모(괴물) 밑으로 다시 복귀시킨 뒤 로컬 위치 초기화
        if (_originParent != null && transform.parent != _originParent)
        {
            transform.SetParent(_originParent);
        }

        transform.localPosition = originOffset; // 시작 위치로 초기화
        shockwaveParticle.Stop(); // 파티클 정지
        audioSource.Stop();
        gameObject.SetActive(false); // 오브젝트 비활성화
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
    }
}
