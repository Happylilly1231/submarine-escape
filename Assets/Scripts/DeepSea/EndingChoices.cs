using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndingChoices : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HelicopterRescueCutscene rescueCutscene;
    [SerializeField] private AudioSource rotorAudioSource;

    [Header("UI")]
    [SerializeField] private Button[] choiceBtns;
    [SerializeField] private GameObject[] choiceTexts;
    [SerializeField] private TextMeshProUGUI HQBaseText;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;   // 기본 이미지
    [SerializeField] private Sprite selectedSprite; // 선택된 이미지
    [SerializeField] private Sprite lockedSprite;   // 잠금 상태 이미지

    [Header("Unlock States")]
    [SerializeField] private bool[] isChoiceUnlocked = new bool[4] { true, false, false, false }; // 선택지 잠금 상태

    private int _currentSelectedIdx = -1;
    private bool _isChoiceConfirmed = false; // 중복 선택 방지 플래그

    public event Action<int> OnChoiceSelected;

    private void Awake()
    {
        for (int i = 0; i < choiceBtns.Length; i++)
        {
            int index = i;
            choiceBtns[i]?.onClick.AddListener(() => SelectChoice(index));
        }
    }

    private void OnEnable()
    {
        _currentSelectedIdx = -1;

        UpdateChoiceUnlockStates();
        RefreshAllSlotVisuals();
    }

    /// <summary>
    /// 슬롯 키(1~4) 입력 시 선택지 선택
    /// </summary>
    /// <param name="context"></param>
    public void HandleSlotKeyPress(InputAction.CallbackContext context)
    {
        if (_isChoiceConfirmed) return;

        int slotIdx = context.control.name[0] - '1';

        if (slotIdx >= 0 && slotIdx < choiceBtns.Length)
        {
            SelectChoice(slotIdx);
        }
    }

    /// <summary>
    /// 선택지 선택 처리
    /// </summary>
    /// <param name="index"></param>
    private void SelectChoice(int index)
    {
        if (index < 0 || index >= choiceBtns.Length) return;
        if (!isChoiceUnlocked[index]) return;

        _isChoiceConfirmed = true;
        _currentSelectedIdx = index;
        RefreshAllSlotVisuals();

        OnChoiceSelected?.Invoke(index);

        StartCoroutine(ExecuteEndingSequence(index));
    }

    /// <summary>
    /// 선택지 잠금 상태 설정
    /// </summary>
    private void UpdateChoiceUnlockStates()
    {
        if (DeepSeaBridge.Instance?.PlayerItemStates != null)
        {
            var itemStates = DeepSeaBridge.Instance.PlayerItemStates;

            isChoiceUnlocked[0] = true; // 무조건 활성화
            isChoiceUnlocked[1] = itemStates.HasMonsterGasBomb; // 괴물화 가스 폭탄 보유 여부에 따라
            isChoiceUnlocked[2] = itemStates.HasSleepingGas;    // 수면 가스 보유 여부에 따라
            isChoiceUnlocked[3] = GameManager.instance.IsAbilityActive; // 능력 게임일 때
        }
    }

    /// <summary>
    /// 선택지 버튼 상태 업데이트
    /// </summary>
    private void RefreshAllSlotVisuals()
    {
        for (int i = 0; i < choiceBtns.Length; i++)
        {
            Button btn = choiceBtns[i];
            if (btn == null) continue;

            btn.interactable = isChoiceUnlocked[i];
            if (isChoiceUnlocked[i])
            {
                btn.image.sprite = (i == _currentSelectedIdx) ? selectedSprite : normalSprite;
            }
            else
            {
                btn.image.sprite = lockedSprite;
                choiceTexts[i].SetActive(false); // 잠금 상태일 때 텍스트 비활성화
            }
        }
    }

    private void SetChoicesVisible(bool visible)
    {
        for (int i = 0; i < choiceBtns.Length; i++)
        {
            choiceBtns[i].gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 선택지에 따른 시퀀스
    /// </summary>
    /// <param name="idx">선택지 번호</param>
    /// <returns></returns>
    private IEnumerator ExecuteEndingSequence(int idx)
    {
        yield return new WaitForSeconds(0.4f);

        // UI 패널 숨기기
        SetChoicesVisible(false);

        switch (idx)
        {
            case 0: // 아무것도 하지 않는다
            case 1: // 타이밍을 기다린다
                if (idx == 0) Debug.Log("선택지 - 아무것도 하지 않는다.");
                else Debug.Log("선택지 - 타이밍을 기다린다.");
                HQBaseBridge.Instance?.SetArrivalChoice(idx);
                yield return StartCoroutine(FadeRotorAndLoadHQBaseScene());
                break;
            case 2: // 구조 요원을 기절시킨다
                Debug.Log("선택지 - 구조 요원을 기절시킨다.");
                break;
            case 3: // 구조 요원을 죽인다
                Debug.Log("선택지 - 구조 요원을 죽인다.");
                break;
        }
    }

    /// <summary>
    /// 헬기 로터 사운드 감쇠 후 페이드 아웃 -> HQBaseScene 로드
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeRotorAndLoadHQBaseScene()
    {
        float rotorFadeDuration = 2f;

        if (rotorAudioSource != null && rotorAudioSource.isPlaying)
        {
            float startVolume = rotorAudioSource.volume;
            float elapsed = 0f;

            while (elapsed < rotorFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rotorFadeDuration);
                float smoothT = t * t;
                rotorAudioSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);
                yield return null;
            }

            rotorAudioSource.volume = 0f;
            rotorAudioSource.Stop();
        }

        yield return StartCoroutine(rescueCutscene.Fade(1f));
        HQBaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);
        rescueCutscene.Fade(0f);
        SceneManager.LoadScene("HQBaseScene");
    }
}