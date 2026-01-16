using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairEngineQTE : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject qteSlider;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform pointer;
    [Header("Movement Settings")]
    [SerializeField] private float speed = 600f;
    private float _minX = -288f;
    private float _maxX = 288f;
    private bool _movingRight = true;
    private bool _isActive = false;
    private int _successCnt = 0;

    void Update()
    {
        if (!_isActive) return;
        MovePointer();
    }

    public void StartQTE(int count)
    {
        qteSlider.SetActive(true);
        _successCnt = count;
        pointer.anchoredPosition = new Vector2(0, pointer.anchoredPosition.y);
        SetupSuccessZone();

        _isActive = true;
        _movingRight = true;
    }

    public void HideQTE()
    {
        _isActive = false;
        qteSlider.SetActive(false);
    }

    private void SetupSuccessZone()
    {
        float targetWidth = 0f;
        float randomRange = 0f;

        // 단계별 설정 (1회 성공 시마다 다음 단계로)
        switch (_successCnt)
        {
            case 0: // 첫 번째 시도
                // targetWidth = 200f;
                // randomRange = 207f;
                targetWidth = 150f;
                randomRange = 232f;
                break;
            case 1: // 두 번째 시도
                targetWidth = 100f;
                randomRange = 258f;
                break;
            case 2: // 세 번째 시도
                targetWidth = 70f;
                randomRange = 272f;
                break;
        }

        successZone.sizeDelta = new Vector2(targetWidth, successZone.sizeDelta.y);

        float randomX = Random.Range(-randomRange, randomRange);
        successZone.anchoredPosition = new Vector2(randomX, successZone.anchoredPosition.y);
    }

    private void MovePointer()
    {
        Vector2 pos = pointer.anchoredPosition;

        if (_movingRight)
        {
            pos.x += speed * Time.deltaTime;
            if (pos.x >= _maxX)
            {
                pos.x = _maxX;
                _movingRight = false;
            }
        }
        else
        {
            pos.x -= speed * Time.deltaTime;
            if (pos.x <= _minX)
            {
                pos.x = _minX;
                _movingRight = true;
            }
        }

        pointer.anchoredPosition = pos;
    }

    public bool ExecuteHit()
    {
        if (!_isActive) return false;
        _isActive = false;

        float pointerX = pointer.anchoredPosition.x;

        float zoneHalfWidth = successZone.rect.width / 2f;
        float zoneX = successZone.anchoredPosition.x;
        float minX = zoneX - zoneHalfWidth;
        float maxX = zoneX + zoneHalfWidth;

        if (pointerX >= minX && pointerX <= maxX)
        {
            return true;
        }
        return false;
    }

}
