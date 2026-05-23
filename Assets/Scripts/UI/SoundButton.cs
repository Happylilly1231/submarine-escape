using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SoundButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("연출 설정")]
    private float _hoverScale = 1.1f; // 커질 배율
    private float _hoverDuration = 0.2f;   // 연출 시간 (초)
    private Ease _easeType = Ease.OutQuad; // 부드러운 가속/감속 타입

    private Vector3 _originalScale;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 기존에 돌고 있던 트윈(애니메이션)이 있다면 즉시 중단하고
        // 2. 새로운 목표 크기로 부드럽게 이동합니다.
        transform.DOKill();
        transform.DOScale(_originalScale * _hoverScale, _hoverDuration).SetEase(_easeType);

        AudioManager.Instance.PlayButtonHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스를 떼면 즉시 원래 크기로 돌아가는 연출 시작
        transform.DOKill();
        transform.DOScale(_originalScale, _hoverDuration).SetEase(_easeType);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 시 약간 눌리는 듯한 느낌(Punch)을 추가할 수도 있습니다.
        transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.1f);

        AudioManager.Instance.PlayButtonClickSound();
    }

    // 씬이 바뀌거나 오브젝트가 파괴될 때 혹시 모를 메모리 찌꺼기 정리
    void OnDestroy()
    {
        transform.DOKill();
    }

    void OnDisable()
    {
        transform.DOKill();
        transform.localScale = _originalScale;
    }
}