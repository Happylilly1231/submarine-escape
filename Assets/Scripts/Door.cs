using System;
using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    public Vector3 frontPos; // 문 앞(앞으로 열리는 쪽, 현재는 아닐 수도 있음!!!) 위치
    public Vector3 backPos; // 문 뒤(뒤로 열리는 쪽, 현재는 아닐 수도 있음!!!) 위치
    public bool isOpened = false; // 열려있는지 변수
    private Transform _doorAxis;
    private float _openAngle = -90f; // 목표 회전각
    private float _openDuration = 1f; // 여는 시간

    public static event Action OnDoorOpenStateChanged; // 문이 열리고 닫힐 때 이벤트
    public static event Action<Door> OnDoorClosed; // 문이 닫힐 때 이벤트(문을 인자로 넘겨줌)

    void Start()
    {
        frontPos = transform.GetChild(0).position;
        backPos = transform.GetChild(1).position;
        _doorAxis = transform.parent; // 문 회전 축
    }

    public void ToggleDoor()
    {
        StopAllCoroutines();
        StartCoroutine(ToggleDoorCoroutine());
    }

    IEnumerator ToggleDoorCoroutine()
    {
        float time = 0f;
        Quaternion startRot = _doorAxis.localRotation; // 시작 각도(현재 각도)
        float targetRotY = isOpened ? 0f : _openAngle; // 열려있는지 여부에 따라 닫거나 열기
        Quaternion endRot = Quaternion.Euler(0, targetRotY, 0); // 끝 각도(목표 각도)

        // 부드럽게 회전
        while (time < _openDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / _openDuration);
            _doorAxis.localRotation = Quaternion.Slerp(startRot, endRot, t); // 부드럽게 회전
            yield return null;
        }
        _doorAxis.localRotation = endRot;

        isOpened = !isOpened;
        OnDoorOpenStateChanged?.Invoke();

        if (!isOpened)
            OnDoorClosed?.Invoke(this);
    }
}
