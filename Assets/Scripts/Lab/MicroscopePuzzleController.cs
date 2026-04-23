using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MicroscopePuzzleController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => false;

    [SerializeField] private GameObject statUI;
    [SerializeField] private GameObject interactorUI;
    [SerializeField] private GameObject microscopeUIRoot;
    [SerializeField] private Image microscopeDisplayImage; // 현미경 결과를 보여줄 UI Image

    [Header("Result Sprites")]
    [SerializeField] private Sprite successSprite;  // 성공
    [SerializeField] private Sprite failSprite;     // 실패

    [Header("Blink Animation")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;
    [SerializeField] private float blinkDuration = 1f;

    private PetriDish currentPetriDish;

    public void SetTargetTube(PetriDish petriDish)
    {
        currentPetriDish = petriDish;
        UpdateMicroscopeDisplay();
    }

    public override void ActivatePuzzle()
    {
        if (currentPetriDish == null) return;

        inventoryManager.CloseInventory();
        statUI.SetActive(false);
        interactorUI.SetActive(false);

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        StartCoroutine(BlinkSequence());
        base.StartPuzzle();
    }

    public override void ExitPuzzle()
    {
        StartCoroutine(ExitSequence());

        base.ExitPuzzle();
    }

    private void UpdateMicroscopeDisplay()
    {
        if (currentPetriDish.IsH2S1)
        {
            microscopeDisplayImage.sprite = successSprite;
        }
        else
        {
            microscopeDisplayImage.sprite = failSprite;
        }
    }

    private IEnumerator BlinkSequence()
    {
        // 눈 감기 (Fade Out)
        yield return StartCoroutine(MoveLids(270, blinkDuration));

        microscopeUIRoot.SetActive(true); // 현미경 화면 켜기

        // 잠시 대기
        yield return new WaitForSeconds(0.2f);

        // 눈 뜨기 (Fade In)
        yield return StartCoroutine(MoveLids(810, blinkDuration));

    }

    private IEnumerator ExitSequence()
    {
        yield return StartCoroutine(MoveLids(270, blinkDuration));

        microscopeUIRoot.SetActive(false);

        yield return StartCoroutine(MoveLids(810, blinkDuration));

        inventoryManager.OpenInventory();
        statUI.SetActive(true);
        interactorUI.SetActive(true);
    }

    private IEnumerator MoveLids(float targetY, float duration)
    {
        float elapsed = 0;
        float startTopY = topLid.anchoredPosition.y;
        float startBottomY = bottomLid.anchoredPosition.y;

        // TopLid는 위쪽으로(+), BottomLid는 아래쪽으로(-) 이동
        float targetTopY = targetY;
        float targetBottomY = -targetY;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 부드러운 움직임을 위해 SmoothStep 적용
            t = Mathf.SmoothStep(0, 1, t);

            topLid.anchoredPosition = new Vector2(0, Mathf.Lerp(startTopY, targetTopY, t));
            bottomLid.anchoredPosition = new Vector2(0, Mathf.Lerp(startBottomY, targetBottomY, t));
            yield return null;
        }
    }
}
