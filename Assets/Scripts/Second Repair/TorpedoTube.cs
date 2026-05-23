using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TorpedoTube : MonoBehaviour
{
    [SerializeField] private GameObject door; // 문
    [SerializeField] private GameObject panelButton; // 패널의 문 버튼
    [SerializeField] private Transform[] lockingDogPivots; // 4방향 잠금장치 피봇 트랜스폼 배열
    [SerializeField] private Outline[] lockingDogOutlines; // 잠금장치 아웃라인 배열
    [SerializeField] private int torpedoTubeNum; // 1 ~ 4번
    public int TorpedoTubeNum => torpedoTubeNum;

    public bool IsOpened { get; private set; } // 문 열린 여부
    public bool IsUnlocked { get; private set; } // 잠금 해제 여부
    public bool IsCloseAvailable { get; set; } // 문 닫기 가능 여부

    private bool _isRotating = false; // 문 회전 중 여부

    public event Action OnUnlocked; // 잠금 해제 이벤트
    public static event Action OnOpened; // 문 열림 이벤트
    public static event Action OnClosed; // 문 닫힘 이벤트
    public event Action<bool> OnDoorOpenStateChanged; // 문 열린 상태 변경 이벤트

    private void Start()
    {
        IsOpened = false;
        IsUnlocked = false;
        IsCloseAvailable = true;

        if (torpedoTubeNum == 3) // 3번 발사관은 잠금해제 된 상태임
        {
            Unlock();
        }
        else
            IsUnlocked = false;
    }

    public void SetLockingDogOutlinesShow(bool isShow)
    {
        foreach (var lockingDogOutline in lockingDogOutlines)
        {
            lockingDogOutline.enabled = isShow;
        }
    }

    /// <summary>
    /// 문 잠금 해제
    /// </summary>
    public void Unlock()
    {
        foreach (Transform lockingDogPivot in lockingDogPivots)
        {
            lockingDogPivot.DOLocalRotate(new Vector3(-90f, 0f, 0f), 2f, RotateMode.LocalAxisAdd)
            .OnComplete(() =>
            {
                switch (torpedoTubeNum)
                {
                    case 1: // 1번 발사관 -> 문 진동하다가 폭발 사망 엔딩
                        door.transform.DOShakeRotation(2f, new Vector3(2f, 2f, 2f), 10)
                        .OnComplete(() =>
                        {
                            // 폭발 사망 엔딩
                            GameManager.instance.GameOver(EEndingType.KeypadExplosion); // 일단 키패드 폭발 엔딩으로 함
                            return;
                        });
                        break;
                    case 4: // 4번 발사관 -> 문이 약간 비틀어짐(비정상)
                        door.transform.DOLocalRotate(new Vector3(0f, -3f, 0f), 1f, RotateMode.LocalAxisAdd)
                        .OnComplete(() =>
                        {
                            // 비틀어지는 것까지 보여준 다음에 exit되게 함
                            IsUnlocked = true;
                            OnUnlocked?.Invoke();
                        });
                        break;
                    default: // 나머지 경우
                        IsUnlocked = true;
                        OnUnlocked?.Invoke();
                        break;
                }
            });
        }
    }

    /// <summary>
    /// 문 열기/닫기
    /// </summary>
    /// <param name="isOpen">여는 여부</param>
    public void SetDoorOpenState(bool isOpen)
    {
        if (_isRotating) return;
        if (IsOpened == isOpen) return;
        if (!isOpen && !IsCloseAvailable) return; // 닫기 불가인데 닫으려는 경우

        _isRotating = true;

        float rotValue;
        if (isOpen)
            rotValue = -90f;
        else
            rotValue = 90f;

        door.transform.DOLocalRotate(new Vector3(0f, rotValue, 0f), 2f, RotateMode.WorldAxisAdd)
        .OnComplete(() =>
        {
            OnDoorOpenStateChanged?.Invoke(isOpen);
            if (isOpen)
            {
                OnOpened?.Invoke();
            }
            else
                OnClosed?.Invoke();

            IsOpened = isOpen;
            _isRotating = false;
        });
    }
}
