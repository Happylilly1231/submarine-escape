using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageUIController : MonoBehaviour
{
    [SerializeField] private GameObject messageUI;
    [SerializeField] private TextMeshProUGUI messageText;

    public static MessageUIController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        messageUI.SetActive(false);
    }

    public void ShowMessage(string text)
    {
        messageUI.SetActive(true);
        messageText.text = text;
    }

    public void HideMessage()
    {
        messageText.text = "";
        messageUI.SetActive(false);
    }

    // public void ShowMessageAndHide(string text)
    // {
    //     StopAllCoroutines();
    //     StartCoroutine(ShowMessageAndHideCoroutine(text));
    // }

    // IEnumerator ShowMessageAndHideCoroutine(string text, float time = 1f)
    // {
    //     ShowMessage(text);
    //     yield return new WaitForSeconds(time);
    //     HideMessage();
    // }
}
