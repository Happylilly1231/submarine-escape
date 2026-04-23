using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UI;

public class SampleSlotUIManager : MonoBehaviour
{
    public static SampleSlotUIManager Instance;

    [Header("샘플 슬롯")]
    [SerializeField] private GameObject[] sampleSlots;
    [SerializeField] private Image[] sampleImage;

    [Header("서브 슬롯")]
    [SerializeField] private GameObject subSlotPanel;
    [SerializeField] private GameObject[] subSlots;
    [SerializeField] private Image[] subIcons;

    [Header("실험기구 슬롯 스프라이트")]
    [SerializeField] private Sprite slotSprite; // 슬롯 기본 스프라이트
    [SerializeField] private Sprite selectedSlotSprite; // 슬롯 선택 스프라이트

    private int _currentSelectedIdx = 0;
    public int CurrentSelectedIdx => _currentSelectedIdx;
    private int _maxSlotCnt = 0;
    public int MaxSlotCnt => _maxSlotCnt;
    public bool IsActive = false;

    private Image[] _mainSlotFrameImgs;
    private LabEquipment _currentEquipment;
    private InventoryManager _inventoryManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        _mainSlotFrameImgs = new Image[sampleSlots.Length];
        for (int i = 0; i < sampleSlots.Length; i++)
            _mainSlotFrameImgs[i] = sampleSlots[i].GetComponent<Image>();

        _inventoryManager = FindObjectOfType<InventoryManager>();
        HideSlotUI();
    }

    /// <summary>
    /// 실험기구 슬롯 활성화
    /// <para> - 해당 실험기구의 최대 슬롯만큼 활성화 </para>
    /// <para> - 0번째 슬롯의 스프라이트는 하이라이트 표시 </para>
    /// <para> - 슬롯에 넣어진 샘플 보임 </para>
    /// </summary>
    public void ShowLabUI(LabEquipment equipment)
    {
        // 같은 실험기구를 보고 있다면 초기화 X
        if (_currentEquipment == equipment) return;

        _currentEquipment = equipment;
        _maxSlotCnt = Mathf.Min(equipment.MaxSlots, sampleSlots.Length);
        _currentSelectedIdx = 0;
        IsActive = true;

        UpdateSampleUI();
        _inventoryManager.UpdateActionText();
    }

    public void UpdateSampleUI()
    {
        if (_currentEquipment == null) return;

        SlotData[] slotData = _currentEquipment.Slots;
        _maxSlotCnt = _currentEquipment.MaxSlots;

        // 1. 메인 슬롯 업데이트
        for (int i = 0; i < sampleSlots.Length; i++)
        {
            if (i < _maxSlotCnt)
            {
                sampleSlots[i].SetActive(true);
                if (i < slotData.Length)
                {
                    RefreshSlotIcon(sampleImage[i], slotData[i]);
                }
                _mainSlotFrameImgs[i].sprite = (i == _currentSelectedIdx ? selectedSlotSprite : slotSprite);
            }
            else
            {
                sampleSlots[i].SetActive(false);
            }
        }

        // 2. 현재 선택된 슬롯이 '기구'를 포함하고 있다면 서브 슬롯 표시
        UpdateSubSlotUI(slotData[Mathf.Clamp(_currentSelectedIdx, 0, slotData.Length - 1)]);
    }

    /// <summary>
    /// 하위 기구의 슬롯 정보를 표시하는 로직
    /// </summary>
    private void UpdateSubSlotUI(SlotData selectedSlot)
    {
        subSlotPanel.SetActive(false);
        foreach (var slot in subSlots) slot.SetActive(false);

        if (!selectedSlot.HasEqipment) return;

        LabEquipment currentSubEq = selectedSlot.equipment;

        subSlotPanel.SetActive(true);
        int iconIdx = 0;

        for (int i = 0; i < currentSubEq.MaxSlots; i++)
        {
            SlotData subData = currentSubEq.Slots[i];

            if (subData.IsEmpty) continue;
            // 시험관 슬롯에 페트리 접시가 들어있다면
            if (subData.HasEqipment)
            {
                LabEquipment deepEq = subData.equipment;
                // 그 안의 샘플들을 서브 슬롯에 나열
                for (int j = 0; j < deepEq.MaxSlots; j++)
                {
                    if (iconIdx >= subSlots.Length) break;

                    if (!deepEq.Slots[j].IsEmpty)
                    {
                        subSlots[iconIdx].SetActive(true);
                        // 페트리 접시 안의 샘플 이미지를 표시
                        RefreshSlotIcon(subIcons[iconIdx], deepEq.Slots[j]);
                        iconIdx++;
                    }
                }
            }
            // 시험관 슬롯에 샘플이 들어있다면
            else
            {
                if (iconIdx >= subSlots.Length) break;

                subSlots[iconIdx].SetActive(true);
                RefreshSlotIcon(subIcons[iconIdx], subData);
                iconIdx++;
            }
        }
    }

    /// <summary>
    /// 슬롯 데이터에 따라 아이콘 이미지와 투명도 설정
    /// </summary>
    private void RefreshSlotIcon(Image iconTarget, SlotData data)
    {
        if (data.IsEmpty)
        {
            iconTarget.sprite = null;
            SetAlpha(iconTarget, 0f);
        }
        else
        {
            // 기구가 있으면 기구 아이콘(Sprite), 없으면 샘플 아이콘(Sprite)
            iconTarget.sprite = data.HasEqipment ? data.equipment.equipmentSprite : data.sample.ItemImage;
            SetAlpha(iconTarget, 1f);
        }
    }

    /// <summary>
    /// 아이템 이미지의 투명도 설정
    /// </summary>
    private void SetAlpha(Image img, float alpha)
    {
        var color = img.color;
        color.a = alpha;
        img.color = color;
    }

    public void HideSlotUI()
    {
        _currentEquipment = null;
        IsActive = false;
        foreach (var slot in sampleSlots) slot.SetActive(false);
        subSlotPanel.SetActive(false);
        _inventoryManager.UpdateActionText();
    }

    /// <summary>
    /// 마우스 휠로 슬롯 선택
    /// </summary>
    /// <param name="wheelInput">마우스 휠 y값</param>
    public void UpdateSelection(float wheelInput)
    {
        if (_maxSlotCnt <= 0) return;

        if (wheelInput > 0) // 휠 위로: scrollValue.y 120
        {
            _currentSelectedIdx = (_currentSelectedIdx - 1 + _maxSlotCnt) % _maxSlotCnt;
        }
        else if (wheelInput < 0) // 휠 아래로: scrollValue.y -120
        {
            _currentSelectedIdx = (_currentSelectedIdx + 1) % _maxSlotCnt;
        }

        UpdateSampleUI(); // 선택이 바뀔 때 서브 슬롯도 함께 갱신되어야 함
        _inventoryManager.UpdateActionText();
    }

    public bool IsSelectedSlotEmpty()
    {
        if (_currentEquipment == null) return false;
        return _currentEquipment.Slots[_currentSelectedIdx].IsEmpty;
    }
}
