using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WireTile : MonoBehaviour
{
    [SerializeField] private WireData wireData;
    [Header("Directional Images")]
    [SerializeField] private GameObject wireTop;
    [SerializeField] private GameObject wireBottom;
    [SerializeField] private GameObject wireLeft;
    [SerializeField] private GameObject wireRight;
    [SerializeField] private GameObject wireCenter;

    public WireData Data => wireData;

    // 이미지 컴포넌트 캐싱
    private Image imgTop, imgBottom, imgLeft, imgRight, imgCenter;

    // 현재 이 타일을 지나가는 모든 전선 색상을 저장
    private List<WireColor> activeColors = new List<WireColor>();
    public List<WireColor> ActiveColors => activeColors;

    // Key: 방향, Value: 해당 방향 전선에 칠해진 색상
    private Dictionary<Vector2Int, List<WireColor>> directionColors = new Dictionary<Vector2Int, List<WireColor>>();

    private void Awake()
    {
        CacheImages();
    }

    private void CacheImages()
    {
        if (wireTop) imgTop = wireTop.GetComponent<Image>();
        if (wireBottom) imgBottom = wireBottom.GetComponent<Image>();
        if (wireLeft) imgLeft = wireLeft.GetComponent<Image>();
        if (wireRight) imgRight = wireRight.GetComponent<Image>();
        if (wireCenter) imgCenter = wireCenter.GetComponent<Image>();
    }

    /// <summary>
    /// 초기 설정 시 호출
    /// </summary>
    public void ApplyData()
    {
        if (wireData.IsFixed) return;

        wireTop?.SetActive(wireData.OpenDirections.Contains(Vector2Int.up));
        wireBottom?.SetActive(wireData.OpenDirections.Contains(Vector2Int.down));
        wireLeft?.SetActive(wireData.OpenDirections.Contains(Vector2Int.left));
        wireRight?.SetActive(wireData.OpenDirections.Contains(Vector2Int.right));
        wireCenter?.SetActive(wireData.OpenDirections.Count > 0);

        ResetToDefaultVisuals();
    }

    /// <summary>
    /// 특정 방향의 전선 색상만 변경하는 함수
    /// </summary>
    public void SetDirectionColor(Vector2Int direction, WireColor color)
    {
        if (wireData.IsFixed) return;

        // 색상 기록 업데이트
        if (activeColors.Contains(color)) activeColors.Remove(color);
        activeColors.Add(color);

        // 방향별 다중 색상 관리
        if (!directionColors.ContainsKey(direction))
        {
            directionColors[direction] = new List<WireColor>();
        }

        if (!directionColors[direction].Contains(color))
        {
            directionColors[direction].Add(color);
        }

        // 시각적 업데이트
        UpdateVisuals();
    }

    /// <summary>
    /// 특정 색상만 제거하고 시각 효과 갱신
    /// </summary>
    public void RemoveColor(WireColor color)
    {
        if (wireData.IsFixed && wireData.WireColor == color) return;
        if (activeColors.Contains(color))
        {
            activeColors.Remove(color);
        }
        // 모든 방향 리스트에서 해당 색상만 제거
        foreach (var list in directionColors.Values)
        {
            if (list.Contains(color)) list.Remove(color);
        }

        UpdateVisuals();
    }

    public void RemoveDirectionColor(Vector2Int direction, WireColor color)
    {
        // 1. 해당 방향의 색상 데이터 삭제
        if (directionColors.ContainsKey(direction) && directionColors[direction].Contains(color))
        {
            directionColors[direction].Remove(color);

            // 만약 해당 방향에 더 이상 남은 색상이 없다면 리스트 삭제
            if (directionColors[direction].Count == 0)
                directionColors.Remove(direction);
        }

        // 2. 만약 타일의 모든 방향에서 이 색상이 사라졌다면 activeColors에서도 제거
        bool isColorStillPresent = false;
        foreach (var list in directionColors.Values)
        {
            if (list.Contains(color))
            {
                isColorStillPresent = true;
                break;
            }
        }

        if (!isColorStillPresent)
        {
            activeColors.Remove(color);
        }

        // 3. 비주얼 갱신
        UpdateVisuals();
    }

    /// <summary>
    /// 현재 남은 색상들 중 가장 최근 것이나 우선순위에 따라 다시 그리기
    /// </summary>
    public void UpdateVisuals()
    {
        // 모든 조각을 일단 기본색(검정)으로 리셋
        ResetToDefaultVisuals();

        // 남아있는 모든 색상 기록을 바탕으로 다시 칠함
        foreach (var pair in directionColors)
        {
            List<WireColor> colorsInThisDir = pair.Value;
            if (colorsInThisDir.Count > 0)
            {
                WireColor topColor = colorsInThisDir[colorsInThisDir.Count - 1];
                ApplyColorToDirection(pair.Key, GetActualColor(topColor));
            }
        }

        // 중앙점은 현재 타일에 색상이 하나라도 있다면 가장 마지막 색상으로 칠함
        // 하나도 없다면 기본색으로
        if (activeColors != null && activeColors.Count > 0 && imgCenter != null)
        {
            WireColor latestColor = activeColors[activeColors.Count - 1];
            imgCenter.color = GetActualColor(latestColor);
        }
        else if (imgCenter != null)
        {
            imgCenter.color = Color.black;
        }
    }

    /// <summary>
    /// 모든 조각을 기본색(검정)으로 리셋
    /// </summary>
    private void ResetToDefaultVisuals()
    {
        if (imgTop) imgTop.color = Color.black;
        if (imgBottom) imgBottom.color = Color.black;
        if (imgLeft) imgLeft.color = Color.black;
        if (imgRight) imgRight.color = Color.black;
        if (imgCenter) imgCenter.color = Color.black;
    }

    /// <summary>
    /// 실제 이미지의 색상을 변경하는 핵심 로직
    /// </summary>
    private void ApplyColorToDirection(Vector2Int direction, Color color)
    {
        if (direction == Vector2Int.up && imgTop) imgTop.color = color;
        else if (direction == Vector2Int.down && imgBottom) imgBottom.color = color;
        else if (direction == Vector2Int.left && imgLeft) imgLeft.color = color;
        else if (direction == Vector2Int.right && imgRight) imgRight.color = color;
    }

    /// <summary>
    /// wireColor 종류에 맞춰 색상 값 지정
    /// </summary>
    private Color GetActualColor(WireColor color)
    {
        switch (color)
        {
            case WireColor.Purple: return new Color(0.65f, 0.3f, 0.95f, 1f); // 밝은 보라
            case WireColor.Green: return new Color(0.2f, 0.85f, 0.2f, 1f); // 선명한 초록
            case WireColor.Red: return new Color(0.9f, 0.1f, 0.1f, 1f);  // 강렬한 빨강
            case WireColor.Pink: return new Color(1f, 0.4f, 0.7f, 1f);    // 진한 분홍
            case WireColor.Orange: return new Color(1f, 0.6f, 0.1f, 1f);    // 선명한 주황
            case WireColor.Blue: return new Color(0.1f, 0.5f, 1f, 1f);    // 맑은 파랑
            default: return Color.black; // 기본은 검정색
        }
    }
}
