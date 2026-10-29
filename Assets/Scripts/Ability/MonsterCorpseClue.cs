using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterCorpseClue : MonoBehaviour, IAbilityInteractable
{
    public bool CanDestroy => false;

    [SerializeField] private InnerMonsterController innerMonsterController;
    [SerializeField] private GameObject clueUI;
    [SerializeField] private Button foldedNoteButton;
    [SerializeField] private GameObject openedNote;
    [SerializeField] private Button closeUIButton;

    private Collider _collider;
    public Collider CorpseCollider => _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.enabled = false;
    }

    private void Start()
    {
        clueUI.SetActive(false);

        foldedNoteButton.onClick.AddListener(OpenNote);
        closeUIButton.onClick.AddListener(CloseUI);

        // UI 활성화 여부 기본 설정
        closeUIButton.gameObject.SetActive(false); // 닫기 버튼 비활성화
        foldedNoteButton.gameObject.SetActive(true);
        openedNote.SetActive(false);
        clueUI.SetActive(false);
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!innerMonsterController.enabled) return "시체 조사 [E]";
        return "";
    }

    public void Interact()
    {
        if (!innerMonsterController.enabled)
        {
            InteractWithAbility();
            return;
        }
    }

    public void InteractWithAbility()
    {
        // 조사
        FocusManager.Instance.PushFocusState(GameFocusState.Diary);
        clueUI.SetActive(true);
    }

    /// <summary>
    /// 메모 펼치기
    /// </summary>
    public void OpenNote()
    {
        foldedNoteButton.gameObject.SetActive(false);
        openedNote.SetActive(true);
        closeUIButton.gameObject.SetActive(true); // 닫기 버튼 활성화
    }

    /// <summary>
    /// UI 닫기
    /// </summary>
    public void CloseUI()
    {
        closeUIButton.gameObject.SetActive(false); // 닫기 버튼 비활성화
        foldedNoteButton.gameObject.SetActive(true);
        openedNote.SetActive(false);
        clueUI.SetActive(false);
        FocusManager.Instance.PopFocusState();
    }
}
