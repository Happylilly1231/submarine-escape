using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingUIManager : MonoBehaviour
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private TextMeshProUGUI endingTitleText; // 엔딩 제목 텍스트


    void Awake()
    {
        restartButton.onClick.AddListener(GameManager.instance.StartGame);
        returnToTitleButton.onClick.AddListener(GameManager.instance.ReturnToTitle);
        quitButton.onClick.AddListener(GameManager.instance.QuitGame);

        if (GameManager.instance.IsClear)
        {
            endingTitleText.color = Color.green;
            endingTitleText.text = "Game Clear";
        }
        else
        {
            endingTitleText.color = Color.red;
            endingTitleText.text = "Game Over...";
        }
    }
}
