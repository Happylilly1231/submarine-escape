using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI를 페이드 인/아웃
/// <para> - 감지된 아이템의 이름과 조작 키 표시를 부드럽게 나타내기 위한 UI 페이드 기능 </para>
/// </summary>
public class FadeUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    public CanvasGroup CanvasGroup => _canvasGroup;

    private Coroutine _fadeCoroutine; // 현재 진행 중인 페이드 코루틴

    [SerializeField] private float fadeDuration = 0.7f; // UI 페이드 지속 시간

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void FadeIn()
    {
        StartFade(1f);
    }

    public void FadeOut()
    {
        StartFade(0f);
    }

    private void StartFade(float targetAlpha)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha));
    }

    private IEnumerator FadeCoroutine(float targetAlpha)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        _fadeCoroutine = null;
    }
}