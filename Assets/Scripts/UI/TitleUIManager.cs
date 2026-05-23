using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button endingFrameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button menuButton;
    void Awake()
    {
        startButton.onClick.AddListener(GameManager.instance.StartGame);
        endingFrameButton.onClick.AddListener(GameManager.instance.EndingGallery);
        quitButton.onClick.AddListener(GameManager.instance.QuitGame);
        menuButton.onClick.AddListener(GameManager.instance.ToggleMenu);
    }

    void Start()
    {
        // 타이틀 화면이 시작되면 커서를 보이게 설정
        if (GameManager.instance != null)
        {
            GameManager.instance.SetCursorVisible(true);
        }
    }
}

