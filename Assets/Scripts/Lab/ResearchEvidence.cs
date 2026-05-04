using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchEvidence : PuzzleController, IInteractable
{
    [SerializeField] private GameObject evidenceUI; // 증거 UI
    [SerializeField] private Button[] evidenceButtons; // 증거 버튼 배열
    [SerializeField] private Button[] hideButtons; // 숨기기 버튼 배열
    [SerializeField] private GameObject[] evidences; // 증거 배열

    protected override bool IsHoverRequired => false;

    protected override bool IsMouseRequiredAtFirst => true;

    private void Awake()
    {
        evidenceUI.SetActive(false);

        // 버튼 배열과 증거 배열의 개수가 다를 경우를 대비한 방어 코드
        int count = Mathf.Min(evidenceButtons.Length, evidences.Length);

        for (int i = 0; i < count; i++)
        {
            int index = i; // 클로저(Closure) 문제를 피하기 위해 로컬 변수에 복사
            evidenceButtons[i].onClick.AddListener(() => OnEvidenceButtonClick(index));
        }

        foreach (var hideButton in hideButtons)
        {
            hideButton.onClick.AddListener(HideEvidence);
        }
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        return "Find Evidence [E]";
    }

    public void Interact()
    {
        ActivatePuzzle();
    }

    #region PuzzleController
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        evidenceUI.SetActive(true);
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        evidenceUI.SetActive(false);
    }
    #endregion

    /// <summary>
    /// 특정 인덱스의 증거만 활성화하고 나머지는 비활성화
    /// </summary>
    public void OnEvidenceButtonClick(int index)
    {
        for (int i = 0; i < evidences.Length; i++)
        {
            // 인덱스가 일치하면 true(활성), 다르면 false(비활성)
            evidences[i].SetActive(i == index);
        }
    }

    public void HideEvidence()
    {
        for (int i = 0; i < evidences.Length; i++)
        {
            evidences[i].SetActive(false);
        }
    }
}
