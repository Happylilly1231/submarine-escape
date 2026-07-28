using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// 나중에 변경할 것임!!!
public class RecordingDevice : PuzzleController, IInteractable
{
    [SerializeField] private GameObject recordingUI;
    [SerializeField] private TextMeshProUGUI recordingText;
    [SerializeField] private Button skipButton;

    // Localization Table Key
    private const string RECORDING_KEY = "Puzzle/RecordingDevice/RecordingText";

    // private const string RECORDING_TEXT = "z좌표 -25 ~ 25에서 지금까지 본 적 없는 비정상적 심해 생물 반응을 포획 가능한 사정거리 내에서 포착했습니다. 어뢰 1발을 적중하였으나 5분 뒤 다시 나타났습니다. 현재 탑재된 포획 장비로는 감당하기 어려울 수 있으니, 포획 허가 승인 및 추가 포획 장비 지원 잠수함을 요청합니다.";
    private bool _isTypingCompleted = false; // 한 번이라도 타이핑이 완료되었는지 여부
    private Coroutine _typingCoroutine;      // 실행 중인 코루틴 참조용

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip glitchSound;
    private AudioSource _audioSource;

    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public override void Start()
    {
        base.Start();

        // 버튼 클릭 이벤트 함수 할당
        skipButton.onClick.AddListener(SkipTyping);

        // 건너뛰기 버튼 처음엔 비활성화
        skipButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 언어 변경 이벤트 구독
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // 언어 변경 이벤트 해제
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    #region Localization
    /// <summary>
    /// 퍼즐 연출 도중 언어가 변경되었을 때 호출되는 콜백
    /// </summary>
    private void OnLanguageChanged(Locale newLocale)
    {
        // 퍼즐 UI가 열려있는 상태에서 언어가 바뀐 경우
        if (recordingUI != null && recordingUI.activeSelf)
        {
            RestartTypingWithCurrentLanguage();
        }
    }

    /// <summary>
    /// 현재 언어 텍스트를 다시 가져와 연출을 처음부터 재시작
    /// </summary>
    private void RestartTypingWithCurrentLanguage()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _audioSource.Stop();
        _typingCoroutine = StartCoroutine(PlayRecording());
    }

    /// <summary>
    /// 현재 언어 설정에 맞는 번역 텍스트 반환
    /// </summary>
    private string GetCurrentLocalizedText()
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", RECORDING_KEY);
    }
    #endregion

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        return LocalizationHelper.GetLocalizedInteractText("Interact/PlayRecording", "E");
    }

    public void Interact()
    {
        ActivatePuzzle();
    }
    #endregion

    #region PuzzleController
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        recordingUI.SetActive(true);

        // 연출 시작
        RestartTypingWithCurrentLanguage();

        if (_isTypingCompleted)
            skipButton.gameObject.SetActive(true);
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        recordingUI.SetActive(false);
        _audioSource.Stop();

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 녹음 파일 재생(타이핑)
    /// </summary>
    private IEnumerator PlayRecording()
    {
        _audioSource.clip = glitchSound;
        _audioSource.Play();

        recordingText.text = "";

        // 현재 언어 텍스트 읽어오기
        string currentText = GetCurrentLocalizedText();

        float defaultTime = 0.1f;
        bool fast = false;
        float t = defaultTime;
        for (int i = 0; i < currentText.Length; i++)
        {
            if (currentText[i] == '*')
            {
                fast = true;
                t = 0.01f;
            }
            else
            {
                if (!fast)
                {
                    if (currentText[i] == '.')
                        t = defaultTime + 0.2f;
                    else
                        t = defaultTime;
                }

                recordingText.text += currentText[i].ToString();
            }

            yield return new WaitForSeconds(t);
        }
        _isTypingCompleted = true;
        _audioSource.Stop();
    }

    /// <summary>
    /// 타이핑 건너뛰기
    /// </summary>
    public void SkipTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        // 전체 텍스트 한 번에 표시
        recordingText.text = GetCurrentLocalizedText();
        _audioSource.Stop();
    }
    #endregion
}