using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject tabButtonRoot;
    [SerializeField] private GameObject tabPanelRoot;
    private int _currentIndex = -1;
    private GameObject[] tabButtons;
    private GameObject[] tabPanels;
    private Color _highLightColor = new Color(190f / 255f, 163f / 255f, 58f / 255f);

    public static MenuUIController instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        tabButtons = new GameObject[tabButtonRoot.transform.childCount];
        for (int i = 0; i < tabButtonRoot.transform.childCount; i++)
        {
            tabButtons[i] = tabButtonRoot.transform.GetChild(i).gameObject;
            int idx = i;
            tabButtons[i].GetComponent<Button>().onClick.AddListener(() => OpenTab(idx));
        }

        tabPanels = new GameObject[tabPanelRoot.transform.childCount];
        for (int i = 0; i < tabPanelRoot.transform.childCount; i++)
        {
            tabPanels[i] = tabPanelRoot.transform.GetChild(i).gameObject;
        }
    }

    private void OnEnable()
    {
        OpenTab(0);
    }

    public void OpenTab(int index)
    {
        if (_currentIndex == index) return;

        if (_currentIndex >= 0)
        {
            tabPanels[_currentIndex].SetActive(false);
            tabButtons[_currentIndex].transform.GetChild(0).GetComponent<Image>().color = Color.white;
            tabButtons[_currentIndex].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.white;
        }

        tabPanels[index].SetActive(true);
        _currentIndex = index;
        tabButtons[_currentIndex].transform.GetChild(0).GetComponent<Image>().color = _highLightColor;
        tabButtons[_currentIndex].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = _highLightColor;
    }
}
