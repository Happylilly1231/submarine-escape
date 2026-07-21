using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레이더 조작 패널
/// </summary>
public class RadarControlPanel : InteractableBase
{
    [SerializeField] private RadarController radarController; // 레이더 컨트롤러
    [SerializeField] private Item toolKitItem; // 공구 상자 아이템 (파괴되어 고장났을 때 필요)
    [SerializeField] private GameObject firstRepairUI; // 1차 수리 UI
    [SerializeField] private Image repairGaugeImg; // 수리 게이지 이미지

    private bool _isPowerOn = false; // 전력 켜져 있는지 여부
    private bool _isBroken = false; // 고장 여부

    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        radarController.RadarControlPanelAudioSource = GetComponent<AudioSource>();

        firstRepairUI.SetActive(false);
        repairGaugeImg.fillAmount = 0f;
    }

    private void OnEnable()
    {
        LightingManager.instance.OnLightChanged += SetPower;
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        if (!_isBroken) return false; // 고장 나지 않았을 때는 X
        return item == toolKitItem; // 고장 났을 때는 공구 상자 선택 여부에 따름
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public override string GetInteractText()
    {
        if (!_isPowerOn) // 전력 없을 때 -> 전력 필요
            return "Power Restoration Required";

        if (!_isBroken) // 고장 나지 않았을 때 -> 레이더 보기
            return "View Radar [E]";

        // 고장 났을 때 - 공구 상자가 선택되어있을 때 -> 수리 / 선택 안됨 -> 수리 필요(공구 상자 필요) 메시지
        return IsRequiredItemSelected() ? "Repair [E]" : "Repair Required (Tool Kit Required)";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 레이더 화면을 띄우거나 끔
    /// </summary>
    public override void Interact()
    {
        if (!_isPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        if (!_isBroken) // 고장 나지 않았을 때 -> 레이더 보기
        {
            radarController.ActivatePuzzle();
        }
        else if (_isBroken && IsRequiredItemSelected()) // 고장 났을 때는 공구 상자가 선택되어있을 때 -> 수리
        {
            StartCoroutine(Repair());
        }
        // 이외는 상호작용 X
    }

    void OnDisable()
    {
        LightingManager.instance.OnLightChanged -= SetPower;
    }

    /// <summary>
    /// 전력 켜거나 끄기
    /// </summary>
    private void SetPower(bool isPowerOn)
    {
        if (isPowerOn && !radarController.IsUpdateStart)
        {
            radarController.IsUpdateStart = true;
            radarController.CurrentMonsterAppearTime = GameTime.Instance.TimeSinceStart;
        }
        _isPowerOn = isPowerOn;
    }

    /// <summary>
    /// 고장 
    /// </summary>
    public void Broke()
    {
        _isBroken = true;
        radarController.SetTurnOnScreen(false);
    }

    /// <summary>
    /// 수리
    /// </summary>
    private IEnumerator Repair()
    {
        float repairTime = 3f;
        float timer = repairTime;

        FocusManager.Instance.PushFocusState(GameFocusState.Puzzle); // 퍼즐 포커스 상태로 변경
        SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 비활성화

        // inventoryManager.UpdateActionText();

        firstRepairUI.SetActive(true);

        while (timer > 0)
        {
            repairGaugeImg.fillAmount = Mathf.Clamp01((repairTime - timer) / repairTime);
            // MessageUIController.Instance.ShowMessage($"수리 중...({timer:F0}초)");
            timer -= Time.deltaTime;
            yield return null;
        }

        firstRepairUI.SetActive(false);

        // MessageUIController.Instance.ShowMessage("수리 완료");
        // yield return new WaitForSeconds(1f);
        // MessageUIController.Instance.HideMessage();

        FocusManager.Instance.PopFocusState(); // 이전 포커스 복구
        SubmarineInGameManager.instance.SetActiveInGameUI(true); // 인게임 UI 활성화
        _isBroken = false;
    }
}
