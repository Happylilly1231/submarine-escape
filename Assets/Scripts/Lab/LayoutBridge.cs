using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode]
public class LayoutBridge : MonoBehaviour
{
    public TextMeshProUGUI targetTMP;
    public float paddingValue = 10f; // TMP 높이에 더해질 패딩값

    void Update()
    {
        if (targetTMP == null) return;

        // 1. 자식 TMP가 계산한 "자기 자신의 적정 높이"
        float childPreferredHeight = LayoutUtility.GetPreferredHeight(targetTMP.rectTransform);

        // 2. 현재 부모(나)의 실제 높이
        float myCurrentHeight = GetComponent<RectTransform>().rect.height;

        // 3. 부모가 가진 Layout Element의 설정값
        LayoutElement le = GetComponent<LayoutElement>();
        float lePreferred = le != null ? le.preferredHeight : -1f;

        // Debug.Log($"[UI 디버그] 자식(TMP) 희망높이: {childPreferredHeight} | 부모 실제높이: {myCurrentHeight} | LayoutElement 설정값: {lePreferred}");

        // 4. 부모 높이를 자식의 희망높이 + 패딩값으로 설정
        float newHeight = childPreferredHeight + paddingValue;

        // 5. 만약 부모의 Layout Element 설정값과 다르다면 업데이트 (무한 루프 방지)
        if (!Mathf.Approximately(lePreferred, newHeight))
        {
            // LayoutElement가 있다면 preferredHeight를 조정, 없다면 RectTransform의 높이를 직접 조정
            if (le != null)
            {
                le.preferredHeight = newHeight;
            }
            else
            {
                RectTransform rt = GetComponent<RectTransform>();
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
            }
        }
    }
}