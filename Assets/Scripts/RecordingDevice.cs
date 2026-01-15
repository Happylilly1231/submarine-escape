using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordingDevice : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject recordingUI;
    [SerializeField] private TextMeshProUGUI recordingText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button exitButton;

    private const string RECORDING_TEXT = "z좌표 -25 ~ -34에서 순회하고 있는 다른 잠수함 발견.\n비정상적인 패턴을 가진 점 발견. 순회하면서 접근 중이다. 통신 실패...\n근처 잠수함에 해당 타겟에 대한 정보를 요청... ... ...\n*(기록이 비정상적으로 종료되었습니다.)";
    private bool _isTypingCompleted = false; // 한 번이라도 타이핑이 완료되었는지 여부

    private PlayerInteractor _playerInteractor;

    private void Start()
    {
        _playerInteractor = FindObjectOfType<PlayerInteractor>();

        // 버튼 클릭 이벤트 함수 할당
        skipButton.onClick.AddListener(SkipTyping);
        exitButton.onClick.AddListener(ExitRecordingDevice);

        // 건너뛰기 버튼 처음엔 비활성화
        skipButton.gameObject.SetActive(false);
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        return "Play Recording[E]";
    }

    public void Interact()
    {
        _playerInteractor.IsPuzzleActive = true;
        _playerInteractor.ClearDetectionText();
        SubmarineInGameManager.instance.SetFocusUI(true);

        recordingUI.SetActive(true);
        StartCoroutine(PlayRecording());
        if (_isTypingCompleted)
            skipButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 녹음 파일 재생(타이핑)
    /// </summary>
    private IEnumerator PlayRecording()
    {
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
    }

    /// <summary>
    /// 타이핑 건너뛰기
    /// </summary>
    public void SkipTyping()
    {
        StopAllCoroutines();
        recordingText.text = RECORDING_TEXT;
    }

    /// <summary>
    /// 종료
    /// </summary>
    public void ExitRecordingDevice()
    {
        _playerInteractor.IsPuzzleActive = false;
        SubmarineInGameManager.instance.SetFocusUI(false);

        recordingUI.SetActive(false);
        StopAllCoroutines();
    }
}
