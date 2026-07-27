using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;

public class PowerSwitch : InteractableBase
{
    [Header("인스펙터 제어용")]
    [SerializeField] private bool debugPowerToggle = false;

    [SerializeField] private Transform switchHandle;
    [SerializeField] private MeshRenderer lightRenderer;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material orangeMaterial;
    [SerializeField] private DieselEngine[] dieselEngines;

    private bool _isPowerOn = false;
    private bool _isFirst = false;

    private EngineController _engineController;
    private PowerController _powerController;

    private void Start()
    {
        _engineController = FindAnyObjectByType<EngineController>();
        _powerController = FindAnyObjectByType<PowerController>();

        debugPowerToggle = _isPowerOn;
    }

    private void Update()
    {
        UpdateIndicatorLight();
    }

    /// <summary>
    /// 인스펙터에서 debugPowerToggle 값을 변경했을 때 실행 - 전력 켜기/끄기
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && debugPowerToggle != _isPowerOn)
        {
            TogglePower(debugPowerToggle);
        }
    }

    #region 상호작용 인터페이스 구현
    /// <summary>
    /// 상호작용 텍스트
    /// <para> - 전력이 켜진 상태: 끄는 상호작용 표시 </para>
    /// <para> - 전력이 꺼져 있으며 복구된 상태: 켜는 상호작용 표시 </para>
    /// <para> - 전력이 꺼져 있으며 복구되지 않은 상태: 전력 복구 필요 표시 </para>
    /// </summary>
    public override string GetInteractText()
    {
        if (_isPowerOn) return LocalizationHelper.GetLocalizedInteractText("Interact/TurnOffPower", "E");
        else if (IsAllConditionsMet()) return LocalizationHelper.GetLocalizedInteractText("Interact/RestorePower", "E");
        else if (_engineController != null && _engineController.currentRepairCount == -1) return LocalizationHelper.GetLocalizedInteractText("Interact/PermanentFailure"); // 점프스케어로 인한 고장 시 영구적으로 전력 고장
        else if (_engineController != null && _engineController.currentRepairCount >= 3) return LocalizationHelper.GetLocalizedInteractText("Interact/SwitchboardWireConnectionRequired");
        else if (_powerController != null && _powerController.IsComplete) return LocalizationHelper.GetLocalizedInteractText("Interact/EngineRepairRequired");
        else return LocalizationHelper.GetLocalizedInteractText("Interact/PowerRestorationRequired");
    }

    /// <summary>
    /// 상호작용
    /// </summary>
    public override void Interact()
    {
        if (_isPowerOn) TogglePower(false); // 전력 끄기
        else if (IsAllConditionsMet()) TogglePower(true); // 전력 키기
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }
    #endregion

    /// <summary>
    /// 전력 켜는/끄는 모션
    /// </summary>
    public void TogglePower(bool turnOn)
    {
        if (!_isFirst)
        {
            // if (SaveSystemManager.Instance != null)
            // {
            //     SaveSystemManager.Instance.UpdateSavePoint(ESavePointType.PowerRestoration, GameTime.Instance.TimeSinceStart);
            // }
            ObjectiveManager.Instance.CompleteObjective("RestorePower");
            _isFirst = true;
        }
        _isPowerOn = turnOn;

        // 기존 회전값 저장 (올라간 상태 0,0,0 / 내려간 상태 180,0,0)
        Vector3 targetRotation = turnOn ? new Vector3(180, 0, 0) : new Vector3(0, 0, 0);

        switchHandle.DOLocalRotate(targetRotation, 1.5f)
                    .SetEase(turnOn ? Ease.InCubic : Ease.OutCubic) // 켤 때는 뻑뻑하게, 끌 때는 부드럽게
                    .OnComplete(() =>
                    {
                        // 고정되는 느낌의 진동
                        switchHandle.DOShakeRotation(0.5f, new Vector3(10, 0, 0), 10, 50);

                        // 라이팅 매니저 연동
                        if (LightingManager.instance != null)
                        {
                            LightingManager.instance.LightToggle(turnOn);
                            Debug.Log($"Power {(turnOn ? "Restored" : "Off")}: Lights {(turnOn ? "On" : "Off")}!");
                        }
                    });
    }

    /// <summary>
    /// 조건 달성도에 따른 라이트 색상 업데이트
    /// </summary>
    private void UpdateIndicatorLight()
    {
        if (lightRenderer == null) return;

        int metCount = GetMetConditionsCount();

        if (metCount == 2)
        {
            lightRenderer.material = greenMaterial;
        }
        else if (metCount == 1)
        {
            lightRenderer.material = orangeMaterial;
        }
        else
        {
            lightRenderer.material = redMaterial;
        }
    }

    private int GetMetConditionsCount()
    {
        int count = 0;
        if (_engineController != null && _engineController.currentRepairCount >= 3) count++;
        if (_powerController != null && _powerController.IsComplete) count++;
        return count;
    }

    private bool IsAllConditionsMet()
    {
        return GetMetConditionsCount() == 2;
    }

    public void ForcePowerRestoration()
    {
        foreach (var engine in dieselEngines)
        {
            engine.ForceComplete();
        }

        _powerController.IsComplete = true;
        _engineController.currentRepairCount = 3;

        UpdateIndicatorLight();
        TogglePower(true);
    }
}
