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
    [SerializeField] private int torpedoTubeNum; // 1 ~ 4번
    public int TorpedoTubeNum => torpedoTubeNum;

    public bool IsOpened { get; private set; } // 문 열린 여부
    public bool IsUnlocked { get; private set; } // 잠금 해제 여부
    public bool IsNormal { get; set; } // 정상 여부

    private Material _panelButtonMaterial;
    private bool _isRotating = false; // 문 회전 중 여부

    public event Action OnUnlocked; // 잠금 해제 이벤트
    public static event Action OnOpened; // 열림 이벤트

    private void Start()
    {
        _panelButtonMaterial = panelButton.GetComponent<Renderer>().material;
        IsOpened = false;
        IsUnlocked = false;
        IsNormal = false;
    }

    /// <summary>
    /// 문 열기 전 테스트
    /// </summary>
    public void TestBeforeOpen()
    {

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
                if (torpedoTubeNum == 4) // 4번 발사관 -> 문이 약간 비틀어짐(비정상)
                {
                    door.transform.DOLocalRotate(new Vector3(0f, -3f, 0f), 1f, RotateMode.LocalAxisAdd)
                    .OnComplete(() =>
                    {
                        // 비틀어지는 것까지 보여준 다음에 exit되게 함
                        IsUnlocked = true;
                        OnUnlocked?.Invoke();
                    });
                }
                else
                {
                    IsUnlocked = true;
                    OnUnlocked?.Invoke();
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

        _isRotating = true;

        float rotValue;
        if (isOpen)
            rotValue = -90f;
        else
            rotValue = 90f;

        door.transform.DOLocalRotate(new Vector3(0f, rotValue, 0f), 2f, RotateMode.WorldAxisAdd)
        .OnComplete(() =>
        {
            if (isOpen)
            {
                _panelButtonMaterial.color = Color.green;
                _panelButtonMaterial.SetColor("_EmissionColor", Color.green);

                OnOpened?.Invoke();
            }
            else
            {
                _panelButtonMaterial.color = Color.red;
                _panelButtonMaterial.SetColor("_EmissionColor", Color.red);
            }
            IsOpened = isOpen;
            _isRotating = false;
        });
    }

}
