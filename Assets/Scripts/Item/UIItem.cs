using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class UIItem : MonoBehaviour
{
    [Header("Localized Sprite Settings")]
    private string _assetTableKey; // 예: "Item/Sprite/KeyPadManual"
    [SerializeField] private SpriteRenderer spriteRenderer; // 이미지 갈아끼울 렌더러 (인스펙터에서 꼭 설정 안해도 됨)
    private InventoryManager _inventoryManager;

    private void Awake()
    {
        string itemName = GetComponent<ItemPickUp>().Item.ItemName;
        string cleanItemName = itemName.Replace(" ", "");
        _assetTableKey = $"Item/Sprite/{cleanItemName}";
        if (spriteRenderer == null) // 인스펙터에서 따로 설정해주지 않았으면 -> 그냥 붙어있는 거 가져옴
            spriteRenderer = GetComponent<SpriteRenderer>();
        _inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    private void OnEnable()
    {
        // 언어 변경 이벤트 구독 및 초기 갱신
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
        UpdateLocalizedSprite();
    }

    private void OnDisable()
    {
        // 언어 변경 이벤트 해제
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateLocalizedSprite();
    }

    /// <summary>
    /// 현재 언어에 맞는 Sprite를 Asset Table에서 가져와 적용
    /// </summary>
    private void UpdateLocalizedSprite()
    {
        if (spriteRenderer == null || string.IsNullOrEmpty(_assetTableKey)) return;

        // Asset Table 'AT_UI'에서 언어별 Sprite 로드
        Sprite localizedSprite = LocalizationSettings.AssetDatabase.GetLocalizedAsset<Sprite>("AT_UI", _assetTableKey);

        if (localizedSprite != null)
        {
            spriteRenderer.sprite = localizedSprite;
        }
    }

    public void Use(Item item, bool isViewing)
    {
        Debug.Log($"Used item: {item.ItemName}");
        switch (item.ItemName)
        {
            // case "Map":
            //     FindAnyObjectByType<MapViewController>().UnlockMap(); // 맵 잠금 해제
            //     Debug.Log("맵 잠금 해제 - Tab키로 열고 닫을 수 있음");
            //     break;
            case "KeyPad Manual":
                Debug.Log(isViewing);
                if (!isViewing) // 확대
                {
                    transform.localPosition = new Vector3(0, 0.03f, 0.3f);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else // 원래 위치로
                {
                    transform.localPosition = new Vector3(0, 0, 0.5f);
                    transform.localRotation = Quaternion.Euler(19, 0, 0);
                }
                Debug.Log("키패드 매뉴얼 - 확대");
                break;
            case "Radar System Manual":
                Debug.Log(isViewing);
                if (!isViewing) // 확대
                {
                    transform.localPosition = new Vector3(0, 0.05f, 0.4f);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else // 원래 위치로
                {
                    transform.localPosition = new Vector3(0, 0, 0.5f);
                    transform.localRotation = Quaternion.Euler(19, 0, 0);
                }
                Debug.Log("레이더시스템메뉴얼 - 확대");
                break;
            case "Morse Code Chart":
                Debug.Log(isViewing);
                if (!isViewing) // 확대
                {
                    transform.localPosition = new Vector3(0, 0.05f, 0.4f);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else // 원래 위치로
                {
                    transform.localPosition = new Vector3(0, 0, 0.5f);
                    transform.localRotation = Quaternion.Euler(19, 0, 0);
                }
                Debug.Log("모스부호표 - 확대");
                break;
            case "Backroom Escape Hint Memo":
                Debug.Log(isViewing);
                if (!isViewing) // 확대
                {
                    transform.localPosition = new Vector3(0, 0f, 0.3f);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else // 원래 위치로
                {
                    transform.localPosition = new Vector3(0, 0, 0.5f);
                    transform.localRotation = Quaternion.Euler(19, 0, 0);
                }
                Debug.Log("백룸 힌트 메모 - 확대");
                break;
            case "SubjectFolder":
                PlayerNoteManager.instance.RegisterClue("SubjectFolder1");
                PlayerNoteManager.instance.RegisterClue("SubjectFolder2");
                PlayerNoteManager.instance.RegisterClue("SubjectFolder3");
                _inventoryManager.ConsumeItemInSlot(GetComponent<ItemPickUp>().Item); // 소모
                break;
            case "Project_Participant":
                PlayerNoteManager.instance.RegisterClue("Project_Participant");
                _inventoryManager.ConsumeItemInSlot(GetComponent<ItemPickUp>().Item); // 소모
                break;
        }
    }
}
