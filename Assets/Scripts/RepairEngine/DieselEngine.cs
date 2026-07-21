using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class DieselEngine : InteractableBase
{
    [Header("엔진")]
    public Transform viewPoint; // 뷰포인트
    [SerializeField] private GameObject flashlight; // 빛
    [SerializeField] private GameObject repairPatch; // 수리 패치

    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem originalParticle; // 원래 파티클
    [SerializeField] private ParticleSystem blockedParticle; // 막힌 파티클
    [SerializeField] private ParticleSystem sparkParticle; // 스파크 파티클

    public int currentRepairStep = 0; // -1: 영구 고장, 0: 수리 전, 1: 1회 수리, 2: 2회 수리, 3: 완전 수리
    public bool IsComplete => currentRepairStep >= 3; // 수리 완료 여부

    private EngineController _engineController; // 엔진 컨트롤러 참조

    public bool IsBrokenWithJumpscare { get; private set; } = false;

    private void Start()
    {
        _engineController = FindObjectOfType<EngineController>();
    }

    #region 상호작용 인터페이스 구현
    public override string GetInteractText()
    {
        if (IsBrokenWithJumpscare) return "Broken (Repair not available)";
        if (IsComplete) return "";
        if (IsRequiredItemSelected()) return "Repair Engine [E]";
        else return "Need Hammer";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 수리 퍼즐 활성화
    /// </summary>
    public override void Interact()
    {
        if (IsBrokenWithJumpscare || IsComplete) return;
        if (IsRequiredItemSelected())
        {
            _engineController.SetTargetEngine(this); // 현재 엔진 등록
            _engineController.ActivatePuzzle(); // 퍼즐 활성화
        }
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return item == _engineController.hammerItem; // 망치 아이템이 선택되어야 상호작용 가능
    }
    #endregion

    /// <summary>
    /// 엔진 시각 효과 설정
    /// </summary>
    /// <param name="isFocused">퍼즐이 활성화된 상태</param>
    public void SetVisuals(bool isFocused)
    {
        if (isFocused) // 퍼즐이 활성화된 상태
        {
            flashlight.SetActive(true);
            repairPatch.SetActive(true);

            originalParticle.gameObject.SetActive(false);
            sparkParticle.gameObject.SetActive(false);
            blockedParticle.gameObject.SetActive(true);

            UpdateSmoke();
        }
        else if (!IsComplete) // 퍼즐이 비활성화된 상태지만 수리가 완전히 되지 않은 경우
        {
            flashlight.SetActive(false);
            repairPatch.SetActive(false);

            originalParticle.gameObject.SetActive(true);
            sparkParticle.gameObject.SetActive(true);
            blockedParticle.gameObject.SetActive(false);
        }
        else // 수리가 완전히 된 경우
        {
            flashlight.SetActive(false);
            repairPatch.SetActive(true);

            originalParticle.gameObject.SetActive(false);
            sparkParticle.gameObject.SetActive(false);
            blockedParticle.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 엔진 수리 단계별 연기 비율
    /// <para> - 0단계: 100% </para>
    /// <para> - 1단계: 66% </para>
    /// <para> - 2단계: 33% </para>
    /// <para> - 3단계: 0% </para>
    /// </summary>
    public void UpdateSmoke()
    {
        if (blockedParticle == null) return;

        var emission = blockedParticle.emission;
        emission.rateOverTime = 50f * (1f - (currentRepairStep / 3f));
    }

    public void PlayFailureEffect()
    {
        sparkParticle.gameObject.SetActive(true);
    }

    public void ForceComplete()
    {
        repairPatch.SetActive(true);

        originalParticle.gameObject.SetActive(false);
        sparkParticle.gameObject.SetActive(false);
        blockedParticle.gameObject.SetActive(false);

        currentRepairStep = 3;
    }

    /// <summary>
    /// 점프스케어 시 즉시 파괴를 통한 고장
    /// </summary>
    public void BrokeWithJumpscare()
    {
        originalParticle.gameObject.SetActive(true);
        sparkParticle.gameObject.SetActive(true);
        blockedParticle.gameObject.SetActive(false);

        currentRepairStep = -1;
        _engineController.currentRepairCount = -1;
        IsBrokenWithJumpscare = true;

        // 깜빡거림 추가할 수도

        // 파워스위치로 전력 끄기
        FindAnyObjectByType<PowerSwitch>().TogglePower(false);
    }
}

