using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReportButton : MonoBehaviour
{
    public ReportData reportData; // 이 버튼이 들고 있을 데이터
    private Image _buttonImage;
    private DatabaseMonitorDisplay _display;

    private void Awake()
    {
        _buttonImage = GetComponent<Image>();
        _display = FindObjectOfType<DatabaseMonitorDisplay>();
    }

    /// <summary>
    /// 버튼이 클릭되었을 때 실행될 함수
    /// </summary>
    public void OnClick()
    {
        _display.OnReportButtonClicked(this);
    }

    /// <summary>
    /// 이미지를 변경하는 헬퍼 함수
    /// </summary>
    public void SetHighlight(bool isSelected, Sprite selectedSprite, Sprite defaultSprite)
    {
        if (_buttonImage != null)
        {
            _buttonImage.sprite = isSelected ? selectedSprite : defaultSprite;
        }
    }
}
