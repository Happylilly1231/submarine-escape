using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ability UI 오브젝트에 붙임 (해당 오브젝트를 활성화/비활성화하지 않고 아래에 있는 Ability Total UI 오브젝트를 관리함)
/// </summary>
public class AbilityUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerAbility playerAbility;
    [SerializeField] private PlayerStat playerStat;

    [Header("능력 전체 UI")]
    [SerializeField] private GameObject abilityUI; // 능력 전체 UI

    [Header("능력 설명 UI")]
    [SerializeField] private GameObject abilityInfoUI; // 능력 설명 UI
    [SerializeField] private Button abilityInfoCloseButton; // 능력 설명 UI 닫기 버튼

    [Header("차징 UI")]
    [SerializeField] private GameObject chargingUI; // 차징 UI
    [SerializeField] private Image chargingGaugeImage; // 차징 게이지 이미지
    [SerializeField] private TextMeshProUGUI chargingActionText; // 차징 액션 텍스트

    [Header("스킬 UI")]
    [SerializeField] private GameObject skillUI; // 스킬 UI
    [SerializeField] private TextMeshProUGUI attackCoolDownText;
    [SerializeField] private GameObject attackIconImage;
    [SerializeField] private GameObject transformLockUI;
    [SerializeField] private GameObject transformUI;
    [SerializeField] private GameObject monsterImage;
    [SerializeField] private GameObject humanImage;
    [SerializeField] private GameObject attackUnavailableImage;
    [SerializeField] private GameObject transformUnavailableImage;

    // 쿨타임
    private bool _isCoolDownUIUpdating = false;
    private int _lastDisplayedSeconds = -1; // 쿨타임 텍스트 갱신 최적화용

    private void OnEnable()
    {
        playerStat.OnSanityStabilized += UnlockTransformUI; // 정신 안정 시 -> 변신 능력 해제
    }

    private void OnDisable()
    {
        if (playerStat != null)
        {
            playerStat.OnSanityStabilized -= UnlockTransformUI;
        }
    }

    private void Start()
    {
        SetShowAbilityUI(GameManager.instance.IsAbilityActive); // 능력 UI 활성화 여부 설정

        // 현재 능력 사용 여부에 따라 이 컴포넌트 활성화 여부 결정
        enabled = GameManager.instance.IsAbilityActive; // 능력 사용하지 않으면 이 컴포넌트 비활성화
        if (!GameManager.instance.IsAbilityActive)
        {
            return; // 아래 로직 실행 x
        }

        playerAbility = PlayerManager.Instance.playerAbility;

        attackUnavailableImage.SetActive(true);
        transformUnavailableImage.SetActive(true);
        transformLockUI.SetActive(true);
        transformUI.SetActive(false);

        if (abilityInfoUI != null)
        {
            abilityInfoUI.SetActive(false);
            abilityInfoCloseButton.onClick.AddListener(HideAbilityInfoUI);
        }
    }

    private void Update()
    {
        UpdateAttackCooldownUI(); // 공격 쿨타임 UI 업데이트
        UpdateSkillUnavailableUI(); // 스킬 불가능 UI 업데이트
    }

    public void SetShowAbilityUI(bool isShow)
    {
        abilityUI.SetActive(isShow);
    }

    /// <summary>
    /// 공격 쿨타임 UI 업데이트
    /// </summary>
    private void UpdateAttackCooldownUI()
    {
        float remainingTime = playerAbility.RemainingAttackCooldown;

        if (remainingTime > 0f)
        {
            if (!_isCoolDownUIUpdating)
            {
                _isCoolDownUIUpdating = true;
                attackCoolDownText.gameObject.SetActive(true);
                attackIconImage.SetActive(false);
            }

            // 매 프레임 문자열을 새로 만들지 않고, 초가 바뀔 때만 텍스트 갱신해서 최적화
            int currentSeconds = Mathf.CeilToInt(remainingTime);
            if (_lastDisplayedSeconds != currentSeconds)
            {
                _lastDisplayedSeconds = currentSeconds;
                attackCoolDownText.text = $"{currentSeconds}s";
            }
        }
        else
        {
            if (_isCoolDownUIUpdating)
            {
                _isCoolDownUIUpdating = false;
                attackCoolDownText.gameObject.SetActive(false);
                attackIconImage.SetActive(true);
                _lastDisplayedSeconds = -1;
            }
        }
    }

    /// <summary>
    /// 스킬 불가능 UI 업데이트
    /// </summary>
    private void UpdateSkillUnavailableUI()
    {
        // 현재 가능 여부의 반대로 변경 (가능 -> 비활성화 / 불가능 -> 활성화)
        SetActiveIfChanged(attackUnavailableImage, !playerAbility.IsAttackAvailable);
        SetActiveIfChanged(transformUnavailableImage, !playerAbility.IsTransformAvailable);
    }

    /// <summary>
    /// 변경되었을 때만 활성화 여부 변경 (중복 방지)
    /// </summary>
    /// <param name="uiObj">UI 오브젝트</param>
    /// <param name="isActive">원하는 활성화 여부</param>
    private void SetActiveIfChanged(GameObject uiObj, bool isActive)
    {
        // 해당 UI 오브젝트가 존재하고 현재 활성화 여부가 원하는 활성화 여부와 다른 경우에만 -> 변경 (중복 방지) 
        if (uiObj != null && uiObj.activeSelf != isActive)
        {
            uiObj.SetActive(isActive);
        }
    }

    /// <summary>
    /// 변신 UI 잠금 해제
    /// </summary>
    public void UnlockTransformUI()
    {
        transformLockUI.SetActive(false);
        transformUI.SetActive(true);
    }

    /// <summary>
    /// 능력 설명 UI 보여주기
    /// </summary>
    public void ShowAbilityInfoUI()
    {
        FocusManager.Instance.PushFocusState(GameFocusState.Diary);
        SubmarineInGameManager.instance.SetActiveInGameUI(false); // 인게임 UI 끄기
        abilityInfoUI.SetActive(true);
    }

    /// <summary>
    /// 능력 설명 UI 닫기
    /// </summary>
    public void HideAbilityInfoUI()
    {
        abilityInfoUI.SetActive(false);
        SubmarineInGameManager.instance.SetActiveInGameUI(true); // 인게임 UI 켜기
        SubmarineInGameManager.instance.InventoryManager.CloseInventory(); // 인벤토리는 닫기
        SetShowAbilityUI(true); // 능력 UI 켜기
        FocusManager.Instance.PopFocusState();
    }

    public void ChangeTransformIconUI(bool isMonster)
    {
        monsterImage.SetActive(!isMonster);
        humanImage.SetActive(isMonster);
    }

    /// <summary>
    /// 차징 UI 보여주기 여부 설정
    /// </summary>
    /// <param name="isShow"></param>
    public void SetShowChargingUI(bool isShow)
    {
        chargingUI.SetActive(isShow);
    }

    public void UpdateChargingGaugeUI(float value)
    {
        chargingGaugeImage.fillAmount = value;
    }

    public void UpdateChargingActionText(string text)
    {
        chargingActionText.text = text;
    }
}
