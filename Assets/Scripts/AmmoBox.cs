using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform ammoBoxDoor; // 실제로 움직이는 탄약 상자 뚜껑
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float rotateDuration = 0.5f;

    private bool _isOpen = false; // 열린/닫힌 상태
    private bool _isMoving = false; // 이동 여부
    private Vector3 _closedPos; // 닫힌 위치
    private Vector3 _openPos; // 열린 위치

    void Start()
    {
        _closedPos = ammoBoxDoor.localEulerAngles;
        _openPos = _closedPos + new Vector3(openAngle, 0, 0);
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    /// <returns></returns>
    public string GetInteractText()
    {
        return _isOpen ? "close [E]" : "open [E]";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 탄약 상자 뚜껑을 열거나 닫음
    /// </summary>
    public void Interact()
    {
        if (_isMoving) return;

        if (_isOpen)
            StartCoroutine(RotateAmmoBox(_openPos, _closedPos));
        else
            StartCoroutine(RotateAmmoBox(_closedPos, _openPos));

        _isOpen = !_isOpen;
    }

    /// <summary>
    /// 탄약 상자 뚜껑을 부드럽게 회전시키는 코루틴
    /// </summary>
    private IEnumerator RotateAmmoBox(Vector3 startRot, Vector3 endRot)
    {
        _isMoving = true;

        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);
            ammoBoxDoor.localEulerAngles = Vector3.Lerp(startRot, endRot, t);
            yield return null;
        }

        ammoBoxDoor.localEulerAngles = endRot;
        _isMoving = false;
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
}
