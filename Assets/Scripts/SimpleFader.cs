using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleFader : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private bool fadeInOnStart = true; // 씬 시작 시 자동으로 밝아질지 여부

    private void Start()
    {
        if (fadeInOnStart && fadeImage != null)
        {
            // 검은 상태에서 시작해서 부드럽게 밝아짐 (Fade In)
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;

            fadeImage.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                fadeImage.gameObject.SetActive(false);
            });
        }
    }

    /// <summary>
    /// 화면이 어두워진 후 지정한 씬으로 이동 (Fade Out -> LoadScene)
    /// </summary>
    public void FadeAndLoadScene(string sceneName)
    {
        if (fadeImage == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        fadeImage.gameObject.SetActive(true);
        fadeImage.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            SceneManager.LoadScene(sceneName);
        });
    }
}
