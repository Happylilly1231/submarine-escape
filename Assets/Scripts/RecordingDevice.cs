using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 나중에 변경할 것임!!!
public class RecordingDevice : PuzzleController, IInteractable
{
    [SerializeField] private GameObject recordingUI;
    [SerializeField] private TextMeshProUGUI recordingText;
    [SerializeField] private Button skipButton;

    private const string RECORDING_TEXT = "z좌표 -25 ~ 25에서 지금까지 본 적 없는 비정상적 심해 생물 반응을 포획 가능한 사정거리 내에서 포착했습니다. 어뢰 1발을 적중하였으나 5분 뒤 다시 나타났습니다. 현재 탑재된 포획 장비로는 감당하기 어려울 수 있으니, 포획 허가 승인 및 추가 포획 장비 지원 잠수함을 요청합니다.";
    private bool _isTypingCompleted = false; // 한 번이라도 타이핑이 완료되었는지 여부

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

    #region IInteractable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        return "Play Recording [E]";
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
        StartCoroutine(PlayRecording());
        if (_isTypingCompleted)
            skipButton.gameObject.SetActive(true);
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        recordingUI.SetActive(false);
        _audioSource.Stop();
        StopAllCoroutines();
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
        float defaultTime = 0.1f;
        bool fast = false;
        float t = defaultTime;
        for (int i = 0; i < RECORDING_TEXT.Length; i++)
        {
            if (RECORDING_TEXT[i] == '*')
            {
                fast = true;
                t = 0.01f;
            }
            else
            {
                if (!fast)
                {
                    if (RECORDING_TEXT[i] == '.')
                        t = defaultTime + 0.2f;
                    else
                        t = defaultTime;
                }

                recordingText.text += RECORDING_TEXT[i].ToString();
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
        StopAllCoroutines();
        recordingText.text = RECORDING_TEXT;
        _audioSource.Stop();
    }
    #endregion
}
