using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;


    void Awake()
    {
        startButton.onClick.AddListener(GameManager.instance.StartGame);
        quitButton.onClick.AddListener(GameManager.instance.QuitGame);
    }
}

