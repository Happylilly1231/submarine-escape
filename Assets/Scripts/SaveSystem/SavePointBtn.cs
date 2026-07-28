using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SavePointBtn : MonoBehaviour
{
    [SerializeField] private List<Button> saveBtns;

    /// <summary>
    /// 세이브 데이터 UI 업데이트
    /// </summary>
    public void UpdateSavePointUI()
    {
        List<SavePointData> allSavePoints = SavePointManager.Instance.LoadAllSavePoints();

        for (int i = 0; i < saveBtns.Count; i++)
        {
            if (i < allSavePoints.Count)
            {
                SavePointData data = allSavePoints[i];

                if (!data.isUnlocked)
                {
                    saveBtns[i].gameObject.SetActive(false);
                    continue;
                }

                saveBtns[i].gameObject.SetActive(true);

                if (saveBtns[i].transform.childCount >= 2)
                {
                    Transform savePointName = saveBtns[i].transform.GetChild(0);
                    Transform playTime = saveBtns[i].transform.GetChild(1);
                    Transform saveDate = saveBtns[i].transform.GetChild(2);

                    TextMeshProUGUI nameText = savePointName.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI playTimeText = playTime.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI saveDateText = saveDate.GetComponent<TextMeshProUGUI>();

                    nameText.text = GetLocalizedContent(data.savePointType);
                    playTimeText.text = FormatTime(data.playTime);
                    saveDateText.text = data.saveDate;
                }

                // 버튼 클릭 이벤트 연결
                saveBtns[i].onClick.RemoveAllListeners();
                ESavePointType targetType = data.savePointType;

                // 버튼을 눌렀을 때 해당 기점 타입을 넘겨줌
                saveBtns[i].onClick.AddListener(() => OnSavePointButtonClicked(targetType));
            }
            else
            {
                saveBtns[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 세이브 기점 버튼 클릭 시 호출
    /// </summary>
    /// <param name="type"></param>
    private void OnSavePointButtonClicked(ESavePointType type)
    {
        Debug.Log($"{type} 기점으로 게임을 시작합니다.");

        SavePointManager.Instance.IsLoadGameMode = true;
        SavePointManager.Instance.QueryLoadFromTitle(type);
        GameManager.instance.StartGame();
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public string GetLocalizedContent(ESavePointType type)
    {
        return LocalizationHelper.GetLocalizedInteractText($"SavePoint/Name/{type}");
    }
}

