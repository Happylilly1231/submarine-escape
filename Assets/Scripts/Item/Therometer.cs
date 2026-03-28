using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class Therometer : MonoBehaviour
{
    [SerializeField] private GameObject screen; // 화면
    [SerializeField] private TextMeshProUGUI temperatureText; // 온도 텍스트
    private PlayerMutation _playerMutation; // 플레이어 괴물화 컴포넌트
    private bool _isTurnOn = false; // 화면 켜진 여부

    private void Awake()
    {
        _playerMutation = FindObjectOfType<PlayerMutation>();
    }
    public void Use()
    {
        // 현재 이 transform에 실행 중인 트윈이 있다면 리턴(함수 종료)
        if (DOTween.IsTweening(transform)) return;

        _isTurnOn = !screen.activeSelf;

        if (_isTurnOn)
        {
            float temperature = _playerMutation.GetCurrentTemperature();
            temperatureText.text = temperature.ToString("F1");
            gameObject.transform.DOLocalRotate(new Vector3(-0.8f, -90f, 80f), 0.5f)
            .OnComplete(() =>
            {
                screen.SetActive(true);
                transform.DOLocalRotate(new Vector3(-0.8f, -90f, 5f), 0.5f)
                    .SetDelay(0.5f);
            });
        }
        else
        {
            screen.SetActive(false);
        }
    }
}
