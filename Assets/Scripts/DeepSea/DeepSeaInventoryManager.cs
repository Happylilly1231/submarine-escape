using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeepSeaInventoryManager : MonoBehaviour
{
    public static DeepSeaInventoryManager Instance { get; private set; }

    [SerializeField] private DeepSeaPlayerMove player; // 플레이어 레퍼런스
    [SerializeField] private GameObject divingHelmet; // 잠수복 헬멧 오브젝트

    [Header("=== Item References ===")]
    public Item oxygenCapsuleItem; // 산소 캡슐 아이템
    public Item thermalProtectorItem; // 열 보호 장치 아이템
    public Item helicopterLocatorItem; // 헬리콥터 좌표 입력 손목시계 아이템

    [Header("=== Player Item States ===")]
    public bool IsDivingsuitEquipped = false; // 잠수복 착용 여부
    public bool HasOxygenCapsule = false; // 산소 캡슐 장착 여부
    public bool HasThermalProtector = false; // 열 보호 장치 장착 여부
    public bool IsThermalProtectorActive = false; // 열 보호 장치 활성화 여부
    public bool HasHelicopterLocator = false; // 헬리콥터 좌표 입력 손목시계 장착 여부

    public List<Item> playerInventory = new List<Item>(); // 플레이어 인벤토리
    private int currentSlotIndex = 0; // 현재 선택된 슬롯 인덱스

    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _itemEquipController = GetComponent<ItemEquipController>();

        PlayerItemStates playerItemStates = DeepSeaBridge.Instance?.PlayerItemStates;
        //ApplyData(playerItemStates);

        // 초기 인벤토리 상태 설정
        if (HasOxygenCapsule) playerInventory.Add(oxygenCapsuleItem);
        if (HasThermalProtector) playerInventory.Add(thermalProtectorItem);
        if (HasHelicopterLocator) playerInventory.Add(helicopterLocatorItem);
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<DeepSeaPlayerMove>();
        }

        if (IsDivingsuitEquipped) // 잠수복 착용 시
        {
            player.SurvivalTime = 300f; // 생존 시간 300초로 설정

            divingHelmet.SetActive(true); // 잠수복 헬멧 오브젝트 활성화

            // 잠수복 color 노란색으로 변경

            // 김 서림 효과 제거
        }
        else // 잠수복 미착용 시
        {
            player.SurvivalTime = 120f; // 생존 시간 120초로 설정

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
        HasHelicopterLocator = data.HasHelicopterLocator;
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
        if (!context.performed) return;

        int slotIdx = context.control.name[0] - '1';

        if (DeepSeaUIManager.Instance != null)
        {
            if (currentSlotIndex == slotIdx)
            {
                // 이미 선택된 슬롯을 다시 누르면 장착 해제
                _itemEquipController.UnequipItem();
                DeepSeaUIManager.Instance.SetSlotGlow(-1); // 모든 슬롯 글로우 해제
                currentSlotIndex = -1; // 현재 선택된 슬롯 인덱스를 -1로 설정
            }
            else
            {
                if (playerInventory.Count > slotIdx) _itemEquipController.EquipItem(playerInventory[slotIdx]);
                DeepSeaUIManager.Instance.SetSlotGlow(slotIdx);
                currentSlotIndex = slotIdx;
            }
        }
    }

    /// <summary>
    /// 플레이어가 아이템 사용 키를 눌렀을 때 호출되는 메서드
    /// <para> - 현재 선택된 슬롯의 아이템을 사용 </para>
    /// </summary>
    /// <param name="context"></param>
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (currentSlotIndex < 0 || currentSlotIndex >= playerInventory.Count) return;

        GameObject heldItemObject = _itemEquipController.HeldItemObject;
        Item heldItemData = _itemEquipController.HeldItemData;

        Debug.Log($"Using item: {heldItemObject?.name ?? "No item equipped"}");

        if (heldItemObject != null && heldItemObject.TryGetComponent<DeepSeaItem>(out DeepSeaItem deepSeaItem))
        {
            if (deepSeaItem.Use(heldItemData))
            {
                // 아이템 사용 후 인벤토리에서 제거
                playerInventory[currentSlotIndex] = null;
                _itemEquipController.UnequipItem();

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
