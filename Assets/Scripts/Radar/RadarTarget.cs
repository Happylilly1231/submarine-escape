using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TargetType { DeepSeaMonster, Submarine2 };

[System.Serializable]
public class RadarTarget
{
    // UI
    public RectTransform dotRectTransform;
    public Image dotImg;
    public TextMeshProUGUI posText;

    public bool IsCurrentActive { get; set; } = true; // 현재 활성화 여부

    private RadarController _radarController; // 레이더 컨트롤러
    private TargetType _type; // 타입 

    private Vector3 _normPos; // 정규화 위치
    private Vector3 _currentPos; // 현재 위치
    public Vector3 CurrentPos => _currentPos;

    // 수치
    public float[] MonsterPeriods { get; private set; } = { 600f, 1020f, 1020f, 1020f }; // 심해 괴물이 다가오기까지 걸리는 시간: 10분, 17분, 17분, 17분
    private float _submarine2Period = 540f / 0.7712f; // 다른 잠수함 한 바퀴 주기: 9분 (괴물과 주기를 맞추기 위해 0.7712f로 똑같이 나누어줌)
    private float _maxR = 12f; // 최대 R 수치(정규화할 때 필요)
    private float _maxZ = 4f; // 최대 Z 수치(정규화할 때 필요)

    public float CurrentMonsterPeriod { get; set; } // 0.771f로 나눈 것 (잠수함과의 거리가 5f 되는 시점이 전체 시간의 0.771f 정도이기 때문에 해당 시점을 원하는 시간으로 맞추기 위해 0.7712f로 원하는 시간으로 나누어준다.(약간의 널널함을 주기 위해 0.0002f 더함))

    public RadarTarget(TargetType type)
    {
        _type = type;
        if (type == TargetType.DeepSeaMonster)
        {
            CurrentMonsterPeriod = MonsterPeriods[0] / 0.7712f;
        }
    }

    public void SetRadarController(RadarController radarController)
    {
        _radarController = radarController;
    }

    // 매 프레임 위치 계산 로직 (공식)
    public void UpdatePosition(float timer)
    {
        // 시간, 필요 수식
        float t, R, Z, angle;
        if (_type == TargetType.DeepSeaMonster)
        {
            t = (timer % CurrentMonsterPeriod) / CurrentMonsterPeriod * 10f; // t가 0 ~ 10이므로 그에 맞춤
            R = 10f - t + 2f * Mathf.Sin(3f * t);
            Z = 2f * Mathf.Sin(4f * t) * (1f - t / 10f);
            angle = _radarController.MonsterStartAngle + 5f * t;
        }
        else
        {
            t = (timer % _submarine2Period) / _submarine2Period * 10f; // t가 0 ~ 10이므로 그에 맞춤
            R = 7f + Mathf.Cos(12f * Mathf.PI * (t / 10f));
            angle = 2f * Mathf.PI * (t / 10f);
            Z = -3f + Mathf.Sin(angle);
        }

        // R, z 정규화 (-1 ~ 1)
        float normalized = Mathf.Clamp(R / _maxR, -1f, 1f);
        float zNormalized = Mathf.Clamp(Z / _maxZ, -1f, 1f);

        // 정규화된 위치
        _normPos.x = normalized * Mathf.Cos(angle);
        _normPos.y = normalized * Mathf.Sin(angle);
        _normPos.z = zNormalized;

        // 실제 위치
        _currentPos.x = _normPos.x * _radarController.MaxPlanarDistance;
        _currentPos.y = _normPos.y * _radarController.MaxPlanarDistance;
        _currentPos.z = _normPos.z * _radarController.MaxHeight;
    }

    /// <summary>
    /// UI에서 위치 갱신
    /// </summary>
    public void UpdatePosUI()
    {
        // UI 위치(x, y)
        Vector2 uiPos = new Vector2(_normPos.x, _normPos.y) * _radarController.RadarRadius;

        // 점 위치, 색상, 크기 갱신
        dotRectTransform.anchoredPosition = uiPos;
        dotImg.color = new Color(0, 1, 0, 0.7f + _normPos.z * 0.3f); // 높이가 올라가면 진해짐(높이 0 기준 0.7f)
        dotRectTransform.localScale = Vector3.one * (1f + _normPos.z * 0.3f); // 높이가 올라가면 커짐(높이 0 기준 1f)

        // 좌표 텍스트 갱신
        posText.text = $"({CurrentPos.x:F0}, {CurrentPos.y:F0}, {CurrentPos.z:F0})";
    }

    /// <summary>
    /// 위치 텍스트가 레이더 화면 안에서 보이도록 보정
    /// </summary>
    public void ClampPosTextsInside()
    {
        Rect textRect = posText.rectTransform.rect;
        Vector2 dir = Vector2.down;
        float offset = 0.02f;

        bool hitRight = dotRectTransform.anchoredPosition.x + textRect.width * 0.5f > _radarController.RadarRadius;
        bool hitLeft = dotRectTransform.anchoredPosition.x - textRect.width * 0.5f < -_radarController.RadarRadius;
        bool hitTop = dotRectTransform.anchoredPosition.y + textRect.height * 0.5f > _radarController.RadarRadius;
        bool hitBottom = dotRectTransform.anchoredPosition.y - textRect.height * 0.5f - offset < -_radarController.RadarRadius;

        if (hitRight) dir.x = -2f;
        if (hitLeft) dir.x = 2f;
        if (hitTop) dir.y = -1f;
        if (hitBottom) dir.y = 1f;

        posText.rectTransform.anchoredPosition = dir * offset;
    }
}
