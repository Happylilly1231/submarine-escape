using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 서랍을 열고 닫을 수 있도록 하는 Interactable 컴포넌트
/// </summary>
public class Drawer : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform drawerBody; // 실제로 움직이는 서랍 부분
    [SerializeField] private float openDistance = -0.004f; // 열릴 때의 이동 거리
    [SerializeField] private float moveDuration = 0.5f; // 이동 시간

    private bool _isOpen = false; // 열린/닫힌 상태
    private bool _isMoving = false; // 이동 여부
    private Vector3 _closedPos; // 닫힌 위치
    private Vector3 _openPos; // 열린 위치

    void Start()
    {
        _closedPos = drawerBody.localPosition;
        _openPos = _closedPos + new Vector3(0, openDistance, 0);
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (_isOpen)
            return LocalizationHelper.GetLocalizedInteractText("Interact/Close", "E");
        else
            return LocalizationHelper.GetLocalizedInteractText("Interact/Open", "E");
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 서랍을 열거나 닫음
    /// </summary>
    public void Interact()
    {
        if (_isMoving) return;

        if (_isOpen)
            StartCoroutine(MoveDrawer(_openPos, _closedPos));
        else
            StartCoroutine(MoveDrawer(_closedPos, _openPos));

        _isOpen = !_isOpen;
    }

    /// <summary>
    /// 서랍을 부드럽게 이동시키는 코루틴
    /// </summary>
    private IEnumerator MoveDrawer(Vector3 startPos, Vector3 endPos)
    {
        _isMoving = true;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            drawerBody.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        drawerBody.localPosition = endPos;
        _isMoving = false;
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
}
