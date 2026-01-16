using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PowerSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] private ElectricalBox electricalBox;
    [SerializeField] private Transform switchHandle;
    [SerializeField] private MeshRenderer lightRenderer;
    [SerializeField] private Material greenMaterial;

    public int IsCompleteDieselCnt = 0;
    private bool _isPowerRestored = false; // 이미 전력을 복구했는지 체크
    public bool IsPowerRestored => _isPowerRestored;

    void Update()
    {
        if (_isPowerRestored) return; // 이미 켜졌다면 체크 중단

        if (IsAllConditionsMet())
        {
            lightRenderer.material = greenMaterial;
        }
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (_isPowerRestored) return ""; // 이미 복구됨

        if (IsAllConditionsMet())
        {
            // 모든 조건 충족 시
            return "Restore Power [E]";
        }
        else
        {
            // 조건 미충족 시
            return "Power Restoration Required";
        }
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 키패드 패널 확대
    /// </summary>
    public void Interact()
    {
        // 이미 켰거나, 조건이 안 맞으면 작동 안 함
        if (_isPowerRestored || !IsAllConditionsMet()) return;

        _isPowerRestored = true;

        // 손잡이 내려감
        switchHandle.DOLocalRotate(new Vector3(180, 0, 0), 1.5f)
        .SetEase(Ease.InCubic) // 처음엔 뻑뻑하게 시작해서 뒤로 갈수록 가속
        .OnComplete(() =>
        {
            // 도착했을 때 살짝 튕기는 느낌(진동)을 주면 고정되는 느낌
            switchHandle.DOShakeRotation(0.5f, new Vector3(10, 0, 0), 10, 50);

            // 잠수함 내 불 켜기
            if (LightingManager.instance != null)
            {
                LightingManager.instance.LightToggle(true);
                Debug.Log("Power Restored: Lights On!");
            }
        });
    }

    /// <summary>
    /// 선택된 아이템으로 상호작용 가능한지 여부
    /// </summary>
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    /// <summary>
    /// 모든 조건(디젤 엔진 + 전기 박스)이 충족되었는지 확인
    /// </summary>
    private bool IsAllConditionsMet()
    {
        if (IsCompleteDieselCnt != 3) return false;

        // 전기 박스 체크 (퍼즐 완성 여부)
        if (electricalBox == null || !electricalBox.IsComplete) return false;

        return true;
    }
}
