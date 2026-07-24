using System.Collections;
using System.Collections.Generic;
using InnerMonsterStates;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Text;
using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary> 인벤토리 관리자
/// <para> - 플레이어 인벤토리 전체를 관리 </para> 
/// <para> - 인벤토리 UI 표시, 슬롯 선택, 아이템 추가/교체/사용/버리기/인벤토리에 반환 등 인벤토리의 모든 기능을 제어 </para>
/// <para> - 모든 인벤토리 관련 입력은 PlayerAction(Input System)의 'Player' 액션 맵에서 처리 </para>
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("인벤토리 UI 및 슬롯")]
    [SerializeField] private GameObject inventoryUI; // 인벤토리 전체 UI 오브젝트
    [SerializeField] private InventorySlot[] inventorySlots; // 인벤토리 슬롯 배열
    [SerializeField] private TMPro.TextMeshProUGUI actionText; // 상호작용 UI - 아이템 사용, 버리기, 맵 열기 키 표시
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText; // 선택된 슬롯의 아이템 이름 표시
    [Header("인벤토리 슬롯 스프라이트")]
    [SerializeField] private Sprite slotSprite; // 슬롯 기본 스프라이트
    [SerializeField] private Sprite selectedSlotSprite; // 슬롯 선택 스프라이트
    [Header("플레이어 오른손 위치")]
    [SerializeField] private Transform rightHandTransform; // 플레이어 오른손 위치 (아이템 버리기 위치 계산에 사용)

    public InventorySlot[] InventorySlots => inventorySlots;
    private int _selectedSlotIndex = -1; // 선택된 슬롯 인덱스
    public int SelectedSlotIndex => _selectedSlotIndex;
    private bool _isSwapMode = false; // T키 눌림 상태
    // public bool HasRegisteredMap = false; // 지도 아이템 사용 여부
    private bool _isViewingUI = false; // UI 아이템 사용으로 UI를 보고 있는 상태 여부
    private ItemEquipController _itemEquipController; // 아이템 장착 컨트롤러
    private PlayerInteractor _playerInteractor;

    // 각 UI용 LocalizedString 선언 (테이블 미리 지정)
    private LocalizedString actionLocString = new LocalizedString { TableReference = "ST_UI" };
    // private LocalizedString interactLocString = new LocalizedString { TableReference = "ST_UI" };
    // private LocalizedString itemNameLocString = new LocalizedString { TableReference = "ST_UI" };
    private string currentKeyBinding = "";

    void Awake()
    {
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _playerInteractor = FindObjectOfType<PlayerInteractor>();
    }

    private void OnEnable()
    {
        // 언어가 바뀌었을 때 자동으로 UpdateActionText()를 다시 렌더링하도록 구독
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateActionText(); // 언어가 바뀌는 순간 현재 상황에 맞는 텍스트 재조합
    }

    // /// <summary>
    // /// 액션 텍스트 변경
    // /// </summary>
    // /// <param name="actionKey">Localization 키</param>
    // /// <param name="keyBinding">실제 조작 키</param>
    // public void ChangeActionText(string actionKey, string keyBinding = "")
    // {
    //     currentKeyBinding = keyBinding;

    //     // Key 지정 시 비동기 번역 시작 -> 완료되면 OnActionTextTranslated 실행
    //     actionLocString.TableEntryReference = actionKey;
    // }

    // private void OnActionTextTranslated(string translatedText)
    // {
    //     if (actionText == null) return;

    //     if (string.IsNullOrEmpty(currentKeyBinding))
    //         actionText.text = translatedText;
    //     else
    //         actionText.text = $"{translatedText} [{currentKeyBinding}]";
    // }

    /// <summary>
    /// 인벤토리 비활성화
    /// </summary>
    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        SelectSlot(-1);
    }

    /// <summary>
    /// 인벤토리 활성화
    /// </summary>
    public void OpenInventory()
    {
        inventoryUI.SetActive(true);
    }

    /// <summary>
    /// T키 눌림 상태 업데이트 (슬롯 교체 기능에 사용)
    /// </summary>
    public void OnTHold(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isSwapMode = true;
        }
        else if (context.canceled)
        {
            _isSwapMode = false;
        }
    }

    /// <summary>
    /// 숫자키(1~6) 입력으로 인벤토리 슬롯 선택해서 아이템 들기 / 아이템 회수 / 슬롯 교체
    /// <para> - T키가 눌린 상태라면 선택된 슬롯과 교체 </para>
    /// <para> - T키가 눌리지 않은 상태라면 해당 슬롯 선택 후 아이템 들기 </para>
    /// <para> - 아이템을 든 상태에서 같은 슬롯 번호를 누르면 아이템 회수 </para>
    /// </summary>
    public void OnSlotKeyPress(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        var activePuzzle = FindObjectOfType<KeyPadController>();
        if (activePuzzle != null && activePuzzle.IsActionProcessing) return;

        int slotIndex = context.control.name[0] - '1';

        if (_isSwapMode && _selectedSlotIndex >= 0) // 슬롯 교체
        {
            SwapSlots(_selectedSlotIndex, slotIndex);
        }
        else if (!_isSwapMode && _selectedSlotIndex != slotIndex) // 슬롯 선택 및 선택된 슬롯에 있는 아이템 들기
        {
            SelectSlot(slotIndex);
            _itemEquipController.EquipItem(inventorySlots[slotIndex].Item, inventorySlots[slotIndex].ItemInstanceNum);
        }
        else if (!_isSwapMode && _selectedSlotIndex == slotIndex) // 아이템 회수
        {
            SelectSlot(-1);
            _itemEquipController.UnequipItem();
        }
    }

    /// <summary>
    /// 선택된 슬롯의 테두리를 네온 스프라이트로 변경
    /// </summary>
    private void SelectSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventorySlots.Length) inventorySlots[slotIndex].GetComponent<Image>().sprite = selectedSlotSprite;
        if (_selectedSlotIndex >= 0) inventorySlots[_selectedSlotIndex].GetComponent<Image>().sprite = slotSprite;
        _selectedSlotIndex = slotIndex;
        _isViewingUI = false;
        UpdateActionText();
    }

    /// <summary>
    /// 액션 텍스트 및 아이템 이름 텍스트 업데이트
    /// <para> - 실험기구 조작 가이드 </para>
    /// <para> - 아이템 이름 텍스트 업데이트 </para>
    /// <para> - 선택된 아이템과 현재 퍼즐 상호작용 상태에 따라 표시할 텍스트 결정 </para>
    /// </summary>
    public void UpdateActionText()
    {
        if (_playerInteractor == null || actionText == null) return;

        StringBuilder sb = new StringBuilder();

        // 실험기구 조작 가이드
        if (SampleSlotUIManager.Instance.IsActive)
        {
            // 슬롯 선택
            if (SampleSlotUIManager.Instance.MaxSlotCnt > 1)
                AppendAction(sb, "Action/Lab/SelectSlot", "Wheel");

            // 슬롯이 비었다면
            if (SampleSlotUIManager.Instance.IsSelectedSlotEmpty())
            {
                Item heldItem = _selectedSlotIndex >= 0 ? inventorySlots[_selectedSlotIndex].Item : null;

                if (_playerInteractor.IsHoldingEquipment &&
                    _playerInteractor.HeldEquipment is TestTube tube && tube.IsResultTube &&
                    _playerInteractor.CurrentEquipment is PetriDish)
                {
                    AppendAction(sb, "Action/Lab/Pour", "E");
                }
                else
                {
                    // 인벤토리 슬롯에 있는 샘플 넣기
                    if (heldItem != null && heldItem.ItemType == EItemType.Sample)
                    {
                        AppendAction(sb, "Action/Lab/InsertSample", "E");
                    }
                    // 실험기구를 손에 들고 있다면 넣기
                    if (_playerInteractor.IsHoldingEquipment && _playerInteractor.CurrentEquipment.CanInsert(_playerInteractor.HeldEquipment))
                    {
                        AppendAction(sb, "Action/Lab/UseLabEquipment", "E");
                    }
                }
            }
            else // 슬롯에 무언가 들어있을 때
            {
                // 현미경 확대 가이드
                if (_playerInteractor.CurrentEquipment is Microscope microscope)
                {
                    if (!microscope.Slots[0].IsEmpty && microscope.IsPowerOn) AppendAction(sb, "Action/Lab/Inspect", "E"); // 슬롯에 샘플이 들어있고 전력이 켜져 있을 때 현미경 관찰 가능
                }
                // 슬롯에 무언가 들어있다면 꺼내기 가이드
                if (_playerInteractor.CurrentEquipment is Centrifuge centrifuge1)
                {
                    if (!centrifuge1.IsOperating) AppendAction(sb, "Action/Lab/RetrieveSample", "R");
                }
                else if (_playerInteractor.CurrentEquipment is TestTube testTube)
                {
                    if (!testTube.IsResultTube) AppendAction(sb, "Action/Lab/RetrieveSample", "R");
                }
                else if (_playerInteractor.CurrentEquipment is PetriDish petriDish)
                {
                    if (!petriDish.ContainsResult) AppendAction(sb, "Action/Lab/RetrieveSample", "R");
                }
                else AppendAction(sb, "Action/Lab/RetrieveSample", "R");
            }

            // 실험기구 들기 가이드
            if (!_playerInteractor.IsHoldingEquipment && _playerInteractor.CurrentEquipment.IsGrabbable)
            {
                AppendAction(sb, "Action/Lab/HoldLabEquipment", "G");
            }

            if (_playerInteractor.CurrentEquipment is Centrifuge centrifuge)
            {
                if (centrifuge.CanStartOperation() && !centrifuge.IsOperating && centrifuge.IsPowerOn) // 시험관 3개 꽉 찼을 때, 작동 중이 아닐 때, 전력 켜져 있을 때 원심분리기 작동 가능
                {
                    AppendAction(sb, "Action/Lab/StartSynthesizer", "E");
                }
            }
            if (_playerInteractor.CurrentEquipment is PetriDish petriDish1)
            {
                if (!petriDish1.isMonsterBloodMixed && petriDish1.isMonsterBloodDropped && !petriDish1.isMixing)
                {
                    AppendAction(sb, "Action/Lab/Mix", "E");
                }
            }
        }

        // 실험기구를 들고 있는 상태라면 놓기 가이드
        if (_playerInteractor.IsHoldingEquipment)
        {
            AppendAction(sb, "Action/Lab/DropLabEquipment", "G");

            if (_playerInteractor.HeldEquipment is Syringe syringe)
            {
                if (syringe.HasContent) AppendAction(sb, "Action/Lab/UseCure", "E");
            }
        }

        Item currentItem = _selectedSlotIndex >= 0 ? inventorySlots[_selectedSlotIndex].Item : null;
        itemNameText.text = (currentItem != null) ? currentItem.LocalizedDisplayName : "";
        if (currentItem != null)
        {
            switch (currentItem.ItemType)
            {
                case EItemType.Toggle:
                    if (FocusManager.Instance.CurrentPuzzleController == null) AppendAction(sb, "Action/Item/Toggle", "LMB");
                    break;
                case EItemType.Consumable:
                    if (FocusManager.Instance.CurrentPuzzleController == null)
                        AppendAction(sb, "Action/Item/Use", "E");
                    break;
                case EItemType.Puzzle: // 퍼즐 상호작용 중이라면 표시
                    if (FocusManager.Instance.CurrentPuzzleController != null)
                    {
                        if (currentItem.ItemName == "Hammer")
                            AppendAction(sb, "Action/Puzzle/RepairEngine", "Space");
                        else if (currentItem.ItemName == "Screwdriver")
                            AppendAction(sb, "Action/Puzzle/Unscrew", "E");
                        else if (currentItem.ItemName.Contains("Battery"))
                            AppendAction(sb, "Action/Puzzle/ReplaceBattery", "E");
                    }
                    break;
                case EItemType.UI:
                    if (FocusManager.Instance.CurrentPuzzleController == null)
                    {
                        if (_isViewingUI)
                            AppendAction(sb, "Action/Item/Close", "E");
                        else if (currentItem.ItemName != "LabID CHM" && currentItem.ItemName != "LabID SEC")
                            AppendAction(sb, "Action/Item/Read", "E");
                    }
                    break;
                case EItemType.Wearable:
                    if (FocusManager.Instance.CurrentPuzzleController == null)
                        AppendAction(sb, "Action/Item/Equip", "E");
                    break;
            }
        }

        AppendAction(sb, "Action/General/TabMenu", "Tab");

        if (currentItem != null && FocusManager.Instance.CurrentPuzzleController == null) // 퍼즐 상호작용 중이 아니라면 버리기 키 표시
            AppendAction(sb, "Action/General/DropItem", "Q");

        // SOS 신호 퍼즐 상호작용 메시지
        if (FocusManager.Instance.CurrentPuzzleController is TelegraphKey telegraphKey)
        {
            AppendAction(sb, "Action/Puzzle/InputMorseCode", "LMB");
            AppendAction(sb, "Action/Puzzle/TransmitFinalMessage", "RMB");
            if (telegraphKey.IsSubmarineLeft)
                AppendAction(sb, "Action/Puzzle/PlayRecording", "E");
        }

        actionText.text = sb.ToString();
    }

    /// <summary>
    /// ST_UI 테이블의 key에 해당하는 번역문을 찾아 keyBinding을 붙인 뒤 StringBuilder에 줄바꿈으로 추가합니다.
    /// </summary>
    private void AppendAction(StringBuilder sb, string key, string keyBinding = "")
    {
        // Preload 덕분에 메모리에서 즉시 텍스트를 읽어옵니다.
        string translatedText = LocalizationSettings.StringDatabase.GetLocalizedString("ST_UI", key);

        if (string.IsNullOrEmpty(keyBinding))
        {
            sb.AppendLine(translatedText);
        }
        else
        {
            sb.AppendLine($"{translatedText} [{keyBinding}]");
        }
    }

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템과 개수를 서로 교환
    /// </summary>
    private void SwapSlots(int selectedSlotIndex, int swapSlotIndex)
    {
        Debug.Log($"슬롯 {selectedSlotIndex + 1} 과 슬롯 {swapSlotIndex + 1} 교체");
        var selectedSlot = inventorySlots[selectedSlotIndex];
        var swapSlot = inventorySlots[swapSlotIndex];

        var tempItem = selectedSlot.Item;
        var tempCount = selectedSlot.ItemCount;
        var tempItemInstanceNum = selectedSlot.ItemInstanceNum;
        var tempUniqueID = selectedSlot.ItemUniqueID;

        selectedSlot.SetSlot(swapSlot.Item, swapSlot.ItemCount, swapSlot.ItemInstanceNum, swapSlot.ItemUniqueID);
        swapSlot.SetSlot(tempItem, tempCount, tempItemInstanceNum, tempUniqueID);
    }

    /// <summary> 인벤토리에 아이템 추가
    /// <para> - 중첩 가능한 아이템인 경우 기존 슬롯에 개수만 업데이트 </para>
    /// <para> - 중첩 불가능한 아이템이거나 기존에 없는 아이템인 경우 빈 슬롯에 새로 추가 </para>
    /// <para> - 빈 슬롯이 없으면 아이템 추가 실패 </para>
    /// </summary>
    /// <returns>인벤토리에 아이템 추가 성공 여부</returns>
    public bool AddItemToInventory(Item newItem, int count = 1, IStatableItem statableItem = null, string uniqueID = "")
    {
        if (newItem.CanOverlap)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot.Item != null && slot.Item.ItemName == newItem.ItemName)
                {
                    slot.UpdateItemCount(count);
                    return true;
                }
            }
        }

        // 개별 상태 저장 아이템의 경우
        if (statableItem != null)
        {
            // 해당 아이템이 아직 개별 상태 저장 딕셔너리에 올라가 있지 않다면 -> 딕셔너리에 등록
            if (statableItem.ItemInstanceNum == 0)
            {
                // 개별 상태 저장 딕셔너리에 현재 아이템 인스턴스 상태 데이터 등록
                StatableItemManager.Instance.RegisterItemState(statableItem);
            }
        }

        // 선택된 슬롯이 비어있는 경우 해당 슬롯에 추가 및 아이템 들기
        if (_selectedSlotIndex >= 0 && inventorySlots[_selectedSlotIndex].Item == null)
        {
            if (statableItem != null)
            {
                inventorySlots[_selectedSlotIndex].AddItem(newItem, count, statableItem.ItemInstanceNum, uniqueID); // 개별 상태 저장 아이템을 위해 슬롯에 현재 아이템 인스턴스 번호도 저장(개별 상태 저장 아이템이 아니면 자동으로 0)
            }
            else
            {
                inventorySlots[_selectedSlotIndex].AddItem(newItem, count, 0, uniqueID);
            }
            _itemEquipController.EquipItem(inventorySlots[_selectedSlotIndex].Item, inventorySlots[_selectedSlotIndex].ItemInstanceNum);
            UpdateActionText();
            return true;
        }

        // 선택된 슬롯이 없는 경우 앞 슬롯에 추가
        foreach (var slot in inventorySlots)
        {
            if (slot.Item == null)
            {
                if (statableItem != null)
                {
                    slot.AddItem(newItem, count, statableItem.ItemInstanceNum, uniqueID); // 개별 상태 저장 아이템을 위해 슬롯에 현재 아이템 인스턴스 번호도 저장(개별 상태 저장 아이템이 아니면 자동으로 0)
                }
                else
                {
                    slot.AddItem(newItem, count, 0, uniqueID);
                }
                return true;
            }
        }

        Debug.Log("인벤토리가 가득 찼습니다.");
        return false;
    }

    /// <summary> 1회용 아이템 소비
    /// <para> - 인벤토리에서 아이템 개수 1개 감소 </para>
    /// <para> - 아이템 장착 해제 </para>
    public void ConsumeItemInSlot(Item item)
    {
        if (item == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].Item == item)
            {
                var targetSlot = inventorySlots[i];
                targetSlot.UpdateItemCount(-1);
                _itemEquipController.UnequipItem();
                UpdateActionText();
                return;
            }
        }
    }

    /// <summary>
    /// E키 입력으로 아이템 사용
    /// <para> - 손전등은 마우스 좌클릭으로 사용 </para>
    /// </summary>
    public void OnItemUse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_selectedSlotIndex < 0) return;

        var selectedSlot = inventorySlots[_selectedSlotIndex];
        if (selectedSlot.Item == null || selectedSlot.Item.ItemPrefab == null) return;

        GameObject currentEquippedItem = _itemEquipController.HeldItemObject;

        switch (selectedSlot.Item.ItemType)
        {
            case EItemType.Consumable:
                if (currentEquippedItem.TryGetComponent<ConsumableItem>(out var consumable))
                {
                    if (consumable.Use(selectedSlot.Item))
                    {
                        // 소비 아이템 사용에 성공한 경우에만 인벤토리에서 개수 감소 및 장착 해제
                        ConsumeItemInSlot(selectedSlot.Item);
                    }
                }
                // if (FindAnyObjectByType<ConsumableItem>().Use(selectedSlot.Item))
                // {
                //     // 소비 아이템은 사용 시 바로 소비
                //     ConsumeItemInSlot(selectedSlot.Item);
                // }
                break;
            case EItemType.UI:
                if (currentEquippedItem.TryGetComponent<ItemPickUp>(out var item))
                {
                    if (item.Item.ItemName == "Diary")
                    {
                        Diary diary = FindObjectOfType<Diary>();
                        diary?.ViewDiary();
                    }
                }
                if (currentEquippedItem.TryGetComponent<UIItem>(out var uiItem))
                {
                    uiItem.Use(selectedSlot.Item, _isViewingUI);
                }
                // if (selectedSlot.Item.ItemName == "Map")
                // {
                //     HasRegisteredMap = true;
                //     // UI 아이템 중 지도는 사용 시 바로 소비
                //     ConsumeItemInSlot(selectedSlot.Item);
                // }

                _isViewingUI = !_isViewingUI;
                break;
            case EItemType.Wearable:
                //FindAnyObjectByType<WearableItem>()?.Use(selectedSlot.Item);
                break;
        }

        UpdateActionText();
    }

    /// <summary>
    /// 마우스 좌클릭으로 손전등 사용
    /// </summary>
    public void OnToggleItemUse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_selectedSlotIndex < 0) return;

        var selectedSlot = inventorySlots[_selectedSlotIndex];
        GameObject currentEquippedItem = _itemEquipController.HeldItemObject;

        if (currentEquippedItem == null) return;

        if (selectedSlot.Item.ItemName == "Flashlight")
        {
            // 손에 든 오브젝트에서 Flashlight 컴포넌트를 찾음
            if (currentEquippedItem.TryGetComponent<Flashlight>(out var flashlight))
            {
                flashlight.Use();
            }
        }
        else if (selectedSlot.Item.ItemName == "Thermometer")
        {
            if (currentEquippedItem.TryGetComponent<Therometer>(out var thermometer))
            {
                thermometer.Use();
            }
        }
    }

    /// <summary>
    /// Q키로 선택된 슬롯의 아이템 1개 버리기
    /// <para> - 아이템 프리팹을 현재 플레이어 앞에 생성하고 아이템 개수 1개 감소 </para>
    /// <para> - 손에 들고 있는 상태라면 아이템 오브젝트도 제거 </para>
    /// </summary>
    public void OnItemDrop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameManager.instance.IsPausing) return;

        if (_selectedSlotIndex < 0) return;

        InventorySlot targetSlot = inventorySlots[_selectedSlotIndex];

        if (targetSlot == null || targetSlot.Item == null) return;

        Debug.Log($"슬롯 {Array.IndexOf(inventorySlots, targetSlot) + 1} 의 아이템 버리기: {targetSlot.Item.ItemName}");

        Vector3 dropPosition = rightHandTransform.position + rightHandTransform.forward * 0.5f;
        GameObject droppedItemObject = Instantiate(targetSlot.Item.ItemPrefab, dropPosition, Quaternion.identity);
        StartCoroutine(ApplyRigidbody(droppedItemObject, 2f));

        StatableItemManager.Instance.RestoreItemState(droppedItemObject, targetSlot.ItemInstanceNum); // 해당 아이템 오브젝트(인스턴스)의 저장된 상태가 있다면, 저장된 상태로 복원
        droppedItemObject.GetComponent<ItemPickUp>().uniqueID = targetSlot.ItemUniqueID;

        if (_itemEquipController.HasItem)
        {
            _itemEquipController.UnequipItem();
        }

        targetSlot.UpdateItemCount(-1);
        UpdateActionText();
    }

    /// <summary>
    /// 특정 시간 동안 오브젝트에 Rigidbody를 추가하여 중력을 적용한 후 제거
    /// </summary>
    private IEnumerator ApplyRigidbody(GameObject itemObject, float duration)
    {
        Rigidbody rb = itemObject.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDrag = 0.05f;
        rb.drag = 0f;
        rb.useGravity = true;

        yield return new WaitForSeconds(duration);

        if (rb != null)
        {
            Destroy(rb);
        }
    }

    /// <summary>
    /// 인벤토리에 해당 아이템을 갖고 있는지 여부 검사
    /// </summary>
    /// <param name="item">아이템</param>
    /// <returns>인벤토리에 해당 아이템을 갖고 있는지 여부</returns>
    public bool CheckHasItemInInventory(Item item)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.Item != null && slot.Item.ItemName == item.ItemName)
            {
                return true;
            }
        }
        return false;
    }
}
