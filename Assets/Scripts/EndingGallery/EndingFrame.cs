using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class EndingFrame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI titleText;

    void Awake()
    {
        if (titleText != null) titleText.gameObject.SetActive(false);
    }

    public void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (titleText != null) titleText.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (titleText != null) titleText.gameObject.SetActive(false);
    }
}
