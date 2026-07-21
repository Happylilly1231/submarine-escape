using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 다각형 이미지 버튼의 경우, 딱 그 영역만 클릭되게 하고 나머지 투명 부분은 클릭을 막는 버튼 스크립트
/// <para>- 주의: 이미지의 Read/Write 옵션을 꼭 켜야 함! (Advanced - Read/Write 체크)</para>
/// </summary>
public class PolygonButton : MonoBehaviour
{
    void Start()
    {
        Image image = GetComponent<Image>();
        if (image != null)
        {
            // 투명도가 0.1(약 10%) 이하인 곳은 클릭 무시
            image.alphaHitTestMinimumThreshold = 0.1f;
        }
    }
}
