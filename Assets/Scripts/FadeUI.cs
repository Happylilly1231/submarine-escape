using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI를 페이드 인/아웃
/// <para> - 감지된 아이템의 이름과 조작 키 표시를 부드럽게 나타내기 위한 UI 페이드 기능 </para>
/// </summary>
public class FadeUI : MonoBehaviour
{
    public CanvasGroup mCanvasGroup;
    private Coroutine mFadeCoroutine; // 현재 진행 중인 페이드 코루틴

    [SerializeField] private float mFadeDuration = 0.7f; // UI 페이드 지속 시간

    void Awake()
    {
        mCanvasGroup = GetComponent<CanvasGroup>();
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
        if (mFadeCoroutine != null)
        {
            StopCoroutine(mFadeCoroutine);
        }
        mFadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha));
    }

    private IEnumerator FadeCoroutine(float targetAlpha)
    {
        float startAlpha = mCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < mFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            mCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / mFadeDuration);
            yield return null;
        }

        mCanvasGroup.alpha = targetAlpha;
        mFadeCoroutine = null;
    }
}