using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class DeepSeaInventoryManager : MonoBehaviour
{
    public static DeepSeaInventoryManager Instance { get; private set; }

    [SerializeField] private UnderwaterPlayerStat underwaterPlayerStat; // 플레이어 스탯 레퍼런스
    [SerializeField] private GameObject divingHelmet; // 잠수복 헬멧 오브젝트
    [SerializeField] private Rig playerHandRig; // 플레이어 손 Rig
    [SerializeField] private Transform playerHandTarget; // 플레이어 손 Target (들고 있는 아이템 위치)

    [SerializeField] private EndingChoices endingChoices; // EndingChoices 스크립트

    [Header("=== Item References ===")]
    public Item energyGunItem; // 에너지 건 아이템
    public Item oxygenCapsuleItem; // 산소 캡슐 아이템
    public Item thermalProtectorItem; // 열 보호 장치 아이템
    public Item helicopterLocatorItem; // 헬리콥터 좌표 입력 손목시계 아이템

    [Header("=== Player Item States ===")]
    public bool IsDivingsuitEquipped = false; // 잠수복 착용 여부
    public bool HasOxygenCapsule = false; // 산소 캡슐 장착 여부
    public bool HasThermalProtector = false; // 열 보호 장치 장착 여부
    public bool IsThermalProtectorActive = false; // 열 보호 장치 활성화 여부

    public List<Item> playerInventory = new List<Item>(); // 플레이어 인벤토리
    private int currentSlotIndex = -1; // 현재 선택된 슬롯 인덱스
    public int CurrentSlotIndex => currentSlotIndex;

    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            PlayerItemStates playerItemStates = DeepSeaBridge.Instance?.PlayerItemStates;

            // // 테스트용
            // playerItemStates = new PlayerItemStates
            // {
            //     IsDivingsuitEquipped = true,
            //     HasThermalProtector = true,
            //     HasOxygenCapsule = true,
            //     HasMonsterGasBomb = true,
            //     HasSleepingGas = true
            // };
            // DeepSeaBridge.Instance.SetPlayerItemStates(playerItemStates);

            ApplyData(playerItemStates);

            // 능력 사용 게임일 때 -> 아래 설정 필요 없음
            if (GameManager.instance.IsAbilityActive)
                return;

            _itemEquipController = GetComponent<ItemEquipController>();

            // 초기 인벤토리 상태 설정
            playerInventory.Add(energyGunItem); // 에너지 건 아이템 1번 슬롯에 추가
            if (HasOxygenCapsule) playerInventory.Add(oxygenCapsuleItem);
            if (HasThermalProtector) playerInventory.Add(thermalProtectorItem);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 능력 사용 게임일 때 -> 잠수복 헬멧 오브젝트 비활성화
        if (GameManager.instance.IsAbilityActive)
        {
            divingHelmet.SetActive(false); // 잠수복 헬멧 오브젝트 비활성화
            return;
        }

        if (underwaterPlayerStat == null)
        {
            underwaterPlayerStat = FindObjectOfType<UnderwaterPlayerStat>();
        }

        if (IsDivingsuitEquipped) // 잠수복 착용 시
        {
            underwaterPlayerStat.SurvivalTime = 300f; // 생존 시간 300초로 설정
            underwaterPlayerStat.OxygenCostPerSec = 0.1f; // 기본 초당 산소 소모량 설정

            divingHelmet.SetActive(true); // 잠수복 헬멧 오브젝트 활성화

            // 잠수복 color 노란색으로 변경

            // 김 서림 효과 제거
        }
        else // 잠수복 미착용 시
        {
            underwaterPlayerStat.SurvivalTime = 90f; // 생존 시간 90초로 설정
            underwaterPlayerStat.OxygenCostPerSec = 1.7f; // 1분에 100% 소모되도록 초당 산소 소모량 설정

            divingHelmet.SetActive(false); // 잠수복 헬멧 오브젝트 비활성화
                                           // 잠수복 color 기본 색상으로 변경
                                           // 김 서림 효과 추가
                                           // 체온 게이지 glow 효과. 단, 열 보호 장치 장착 상태와는 다른 glow 효과를 적용해야 함
        }
    }

    /// <summary>
    /// 잠수함씬에서 심해씬으로 전환될 때 정보 전달
    /// </summary>
    /// <param name="data"></param>
    private void ApplyData(PlayerItemStates data)
    {
        IsDivingsuitEquipped = data.IsDivingsuitEquipped;
        HasOxygenCapsule = data.HasOxygenCapsule;
        HasThermalProtector = data.HasThermalProtector;
    }

    /// <summary>
    /// 플레이어가 아이템 슬롯 키를 눌렀을 때 호출되는 메서드
    /// <para> - slotIdx에 해당하는 슬롯만 글로우 효과 적용 </para>
    /// <para> - slotIdx에 해당하는 슬롯의 아이템을 장착 </para>
    /// <para> - 이미 선택된 슬롯을 다시 누르면 장착 해제 </para>
    /// </summary>
    /// <param name="context"></param>
    public void OnSlotKeyPress(InputAction.CallbackContext context)
    {
        if (!context.performed || GameManager.instance.IsAbilityActive) return;

        if (endingChoices != null && endingChoices.gameObject.activeInHierarchy)
        {
            endingChoices.HandleSlotKeyPress(context); // EndingChoices 스크립트에서 슬롯 키 입력 처리
            return;
        }

        int slotIdx = context.control.name[0] - '1';

        // 키 입력 시에만 토글 처리
        if (currentSlotIndex == slotIdx)
            UnselectSlot();
        else
            SelectSlot(slotIdx);

        // if (DeepSeaUIManager.Instance != null)
        // {
        //     if (currentSlotIndex == slotIdx)
        //     {
        //         // 이미 선택된 슬롯을 다시 누르면 장착 해제
        //         _itemEquipController.UnequipItem();
        //         DeepSeaUIManager.Instance.SetSlotGlow(-1); // 모든 슬롯 글로우 해제
        //         currentSlotIndex = -1; // 현재 선택된 슬롯 인덱스를 -1로 설정
        //     }
        //     else
        //     {
        //         if (playerInventory.Count > slotIdx) _itemEquipController.EquipItem(playerInventory[slotIdx]);
        //         DeepSeaUIManager.Instance.SetSlotGlow(slotIdx);
        //         currentSlotIndex = slotIdx;
        //     }
        // }
    }

    /// <summary>
    /// 특정 슬롯의 아이템 장착 (외부 호출용)
    /// </summary>
    public void SelectSlot(int slotIdx)
    {
        if (GameManager.instance.IsAbilityActive) return;
        if (slotIdx < 0 || slotIdx >= playerInventory.Count || playerInventory[slotIdx] == null) return;
        if (currentSlotIndex == slotIdx) return; // 이미 들고 있으면 중복 실행 방지

        _itemEquipController.EquipItem(playerInventory[slotIdx]);
        DeepSeaUIManager.Instance?.SetSlotGlow(slotIdx);
        currentSlotIndex = slotIdx;

        playerHandTarget.transform.position = _itemEquipController.HeldItemObject.transform.position;

        DOTween.To(() => playerHandRig.weight, x => playerHandRig.weight = x, 1f, 1f); // 손으로 잡기
    }

    public void UpdatePlayerHandTarget(bool IsLookingBack = false)
    {
        if (_itemEquipController.HeldItemObject != null)
        {
            if (IsLookingBack)
                playerHandTarget.transform.position = underwaterPlayerStat.transform.position;
            else
                playerHandTarget.transform.position = _itemEquipController.HeldItemObject.transform.position;
        }
    }

    /// <summary>
    /// 현재 장착 중인 아이템 해제 (외부 호출용)
    /// </summary>
    public void UnselectSlot()
    {
        if (currentSlotIndex == -1) return; // 이미 빈손이면 중복 실행 방지

        _itemEquipController.UnequipItem();
        DeepSeaUIManager.Instance?.SetSlotGlow(-1);
        currentSlotIndex = -1;

        DOTween.To(() => playerHandRig.weight, x => playerHandRig.weight = x, 0f, 1f); // 손 원래대로
    }

    /// <summary>
    /// 플레이어가 아이템 사용 키를 눌렀을 때 호출되는 메서드
    /// <para> - 현재 선택된 슬롯의 아이템을 사용 </para>
    /// </summary>
    /// <param name="context"></param>
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (!context.performed || GameManager.instance.IsAbilityActive) return;

        if (currentSlotIndex < 0 || currentSlotIndex >= playerInventory.Count) return;

        GameObject heldItemObject = _itemEquipController.HeldItemObject;
        Item heldItemData = _itemEquipController.HeldItemData;

        Debug.Log($"Using item: {heldItemObject?.name ?? "No item equipped"}");

        if (heldItemObject != null && heldItemObject.TryGetComponent<DeepSeaItem>(out DeepSeaItem deepSeaItem))
        {
            if (deepSeaItem.Use(heldItemData))
            {
                // 에너지 건은 아래 로직 실행 X
                if (heldItemData.ItemName == "Energy Gun")
                    return;

                // 아이템 사용 후 인벤토리에서 제거
                playerInventory[currentSlotIndex] = null;
                _itemEquipController.UnequipItem();
                DOTween.To(() => playerHandRig.weight, x => playerHandRig.weight = x, 0f, 1f); // 손 원래대로

                // UI 업데이트
                if (DeepSeaUIManager.Instance != null)
                {
                    // 인벤토리 슬롯 업데이트
                    DeepSeaUIManager.Instance.EmptySlot(currentSlotIndex);
                    DeepSeaUIManager.Instance.SetSlotGlow(-1); // 모든 슬롯 글로우 해제
                    currentSlotIndex = -1; // 현재 선택된 슬롯 인덱스를 -1

                    // 산소, 체온 게이지 업데이트
                    StartCoroutine(DeepSeaUIManager.Instance.GlowGaugeImg(heldItemData));
                }
            }
        }
    }
}
