using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.VisualScripting;
using System;

public class DeepSeaUIManager : MonoBehaviour
{
    public static DeepSeaUIManager Instance { get; private set; }

    [Header("=== Target Reference ===")]
    [SerializeField] private DeepSeaPlayerMove player;

    [Header("=== Helmet HUD ===")]
    [SerializeField] private GameObject helmetHUD; // 헬멧 hud

    [Header("=== Oxygen UI ===")]
    [SerializeField] private Image oxygenBackgroundImg; // 산소 게이지 배경 이미지
    [SerializeField] private Image oxygenFillImg; // 산소 게이지 이미지
    [SerializeField] private TextMeshProUGUI oxygenText; // 산소 수치 텍스트

    [Header("=== Temperature UI ===")]
    [SerializeField] private Image temperatureBackgroundImg; // 체온 게이지 배경 이미지
    [SerializeField] private Image temperatureFillImg; // 체온 게이지 이미지
    [SerializeField] private TextMeshProUGUI temperatureText; // 체온 수치 텍스트
    [SerializeField] private TextMeshProUGUI protectorTimerText; // 열 보호 장치 지속 시간 텍스트

    [Header("=== Depth UI ===")]
    [SerializeField] private RectTransform posBounds; // 깊이 UI의 경계
    [SerializeField] private RectTransform playerPosDot; // 플레이어 위치
    [SerializeField] private TextMeshProUGUI playerPosText; // 플레이어 위치 텍스트
    [SerializeField] private Image helicopterImg; // 헬리콥터 이미지


    [Header("=== ItemSlot UI ===")]
    [SerializeField] private Image[] itemSlotImgs; // 아이템 슬롯 이미지 배열
    [SerializeField] private Sprite slotImg; // 아이템 슬롯 기본 이미지
    [SerializeField] private Sprite glowSlotImg; // 아이템 슬롯 글로우 이미지

    [Header("=== DeepSea Guide ===")]
    [SerializeField] private GameObject guideUI; // 심해 가이드
    [SerializeField] private GameObject[] pages; // 가이드 페이지(조작법, 플레이어 상태, 아이템)
    [SerializeField] private Button[] pageControls; // 페이지 조작 버튼(이전, 다음, 닫기)

    public bool IsActiveGuide { get; private set; } = false;

    private int inactivePageIdx; // 비활성화된 페이지
    private DeepSeaMonsterController _deepSeaMonsterController;


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

        _deepSeaMonsterController = FindObjectOfType<DeepSeaMonsterController>();
    }

    private void Start()
    {
        // Player 레퍼런스를 지정하지 않았을 경우 자동 검색
        if (player == null)
        {
            player = FindObjectOfType<DeepSeaPlayerMove>();
        }

        helmetHUD.SetActive(false);
        guideUI.SetActive(false);
    }

    private void Update()
    {
        if (player == null) return;
        if (DeepSeaIntroCutScene.Instance.IsCutScene) return;

        UpdateOxygenUI();
        UpdateTemperatureUI();
        UpdatePositionUI();
    }

    /// <summary>
    /// 심해 가이드 ui 설정
    /// </summary>
    public void SetDeepSeaGuide()
    {
        IsActiveGuide = true;

        // 헬멧 hud 비활성화 및 가이드 ui 활성화
        helmetHUD.SetActive(false);
        guideUI.SetActive(true);

        // 다음 버튼 활성화 및 닫기 버튼 비활성화
        pageControls[0].gameObject.SetActive(false);
        pageControls[0].onClick.AddListener(PrevPage);
        pageControls[1].gameObject.SetActive(true);
        pageControls[1].onClick.AddListener(NextPage);
        pageControls[2].gameObject.SetActive(false);
        pageControls[2].onClick.AddListener(ClosePage);
    }

    private void PrevPage()
    {
        inactivePageIdx = 3;

        UpdatePage();
    }

    private void NextPage()
    {
        inactivePageIdx = 0;

        UpdatePage();
    }

    private void UpdatePage()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i != inactivePageIdx);
        }

        bool isFirstPage = inactivePageIdx == 3;

        pageControls[0].gameObject.SetActive(!isFirstPage);
        pageControls[1].gameObject.SetActive(isFirstPage);
        pageControls[2].gameObject.SetActive(!isFirstPage);
    }

    /// <summary>
    /// 가이드 ui 비활성화 후 게임 시작
    /// <para> - 게임 ui 비활성화 및 헬멧 hud 활성화</para>
    /// <para> - </para>
    /// </summary>
    private void ClosePage()
    {
        helmetHUD.SetActive(true);
        guideUI.SetActive(false);

        // 헬멧 hud 초기화
        oxygenFillImg.fillAmount = 1f;
        temperatureFillImg.fillAmount = 1f;

        SetItemSlotUI();

        helicopterImg.enabled = DeepSeaBridge.Instance.HasContactedHQ;

        FocusManager.Instance.PushFocusState(GameFocusState.DeepSea);

        _deepSeaMonsterController.IsFSMPause = false; // 심해 몬스터 이동 시작
        IsActiveGuide = false;
    }

    /// <summary>
    /// 산소 UI 업데이트
    /// </summary>
    private void UpdateOxygenUI()
    {
        float currentO2 = player.CurrentOxygen;
        float maxO2 = player.MaxOxygen;


        if (oxygenFillImg != null)
        {
            oxygenFillImg.fillAmount = Mathf.Clamp01(currentO2 / maxO2);
        }

        if (oxygenText != null)
        {
            oxygenText.text = $"{Mathf.CeilToInt(oxygenFillImg.fillAmount * 100)}%";
        }
    }

    /// <summary>
    /// 체온 UI 업데이트
    /// </summary>
    private void UpdateTemperatureUI()
    {
        float currentTemp = player.CurrentTemperature;
        float maxTemp = player.MaxTemperature;

        if (temperatureFillImg != null)
        {
            temperatureFillImg.fillAmount = Mathf.Clamp01(currentTemp / maxTemp);
        }

        if (temperatureText != null)
        {
            temperatureText.text = $"{Mathf.CeilToInt(temperatureFillImg.fillAmount * 100)}%";
        }

        if (DeepSeaInventoryManager.Instance.IsThermalProtectorActive)
        {
            protectorTimerText.enabled = true;

            float remainingTime = player.ThermalProtectorDuration;
            float minutes = Mathf.FloorToInt(remainingTime / 60f);
            float seconds = Mathf.FloorToInt(remainingTime % 60f);

            protectorTimerText.text = $"[ {minutes:00}:{seconds:00} ]";
        }
        else
        {
            protectorTimerText.enabled = false;
        }
    }

    /// <summary>
    /// 위치 UI 업데이트
    /// </summary>
    private void UpdatePositionUI()
    {
        if (playerPosText == null) return;

        Vector3 pos = player.transform.position; // 플레이어의 현재 위치 가져오기

        float normalizedX = Mathf.InverseLerp(-250f, 250f, pos.x); // -250 ~ 250 범위를 0~1로 정규화
        float normalizedDepth = Mathf.InverseLerp(0f, 350f, player.DisplayDepth); // 0 ~ 350 범위를 0~1로 정규화

        float x = Mathf.Lerp(-posBounds.rect.width / 2f, posBounds.rect.width / 2f, normalizedX);
        float y = Mathf.Lerp(-posBounds.rect.height / 2f, posBounds.rect.height / 2f, 1 - normalizedDepth);

        // 플레이어 점이 영역 밖으로 나가지 않도록 제한
        float halfWidth = playerPosDot.rect.width / 2f;
        float halfHeight = playerPosDot.rect.height / 2f;

        x = Mathf.Clamp(x, -posBounds.rect.width / 2f + halfWidth, posBounds.rect.width / 2f - halfWidth);
        y = Mathf.Clamp(y, -posBounds.rect.height / 2f + halfHeight, posBounds.rect.height / 2f - halfHeight);

        playerPosDot.anchoredPosition = new Vector2(x, y);

        playerPosText.text = $"({pos.x:F0}, {pos.z:F0})"; // 플레이어의 x, z 좌표를 소수점 없이 표시

        // 플레이어 위치 텍스트가 영역 밖으로 나가지 않도록 제한
        RectTransform textRect = playerPosText.rectTransform;

        float textWidth = textRect.rect.width;

        float minX = -posBounds.rect.width / 2f + textWidth / 2f;
        float maxX = posBounds.rect.width / 2f - textWidth / 2f;

        float clampedX = Mathf.Clamp(x, minX, maxX);
        textRect.anchoredPosition = new Vector2(clampedX, y - playerPosDot.rect.height / 2f - 15f); // 텍스트를 점 아래에 위치시키고 약간의 여백 추가
    }

    /// <summary>
    /// 아이템 슬롯 UI 초기화
    /// </summary>
    private void SetItemSlotUI()
    {
        if (DeepSeaInventoryManager.Instance == null) return;

        foreach (var slotImg in itemSlotImgs)
        {
            slotImg.transform.GetChild(0).GetComponent<Image>().enabled = false; // 모든 슬롯 비활성화
        }

        int slotIndex = 0;

        foreach (var item in DeepSeaInventoryManager.Instance.playerInventory)
        {
            if (slotIndex >= itemSlotImgs.Length) break;

            itemSlotImgs[slotIndex].transform.GetChild(0).GetComponent<Image>().enabled = true;
            itemSlotImgs[slotIndex].transform.GetChild(0).GetComponent<Image>().sprite = item.ItemImage; // 아이템 이미지 설정
            slotIndex++;
        }
    }

    /// <summary>
    /// 아이템 슬롯 글로우 효과 설정
    /// <para> - slotIndex에 해당하는 슬롯만 글로우 효과 적용 </para>
    /// </summary>
    /// <param name="slotIndex"></param>
    public void SetSlotGlow(int slotIndex)
    {
        // 모든 슬롯 글로우 해제
        foreach (var slotImg in itemSlotImgs)
        {
            slotImg.sprite = this.slotImg;
        }

        if (slotIndex < 0 || slotIndex >= itemSlotImgs.Length) return;

        itemSlotImgs[slotIndex].sprite = glowSlotImg;
    }

    /// <summary>
    /// 아이템 슬롯 비우기
    /// <para> - slotIndex에 해당하는 슬롯만 비우기 </para>
    /// </summary>
    /// <param name="slotIndex"></param>
    public void EmptySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= itemSlotImgs.Length) return;

        itemSlotImgs[slotIndex].transform.GetChild(0).GetComponent<Image>().enabled = false;
    }

    /// <summary>
    /// 게이지 glow 효과
    /// </summary>
    /// <param name="item">게이지 관련 아이템</param>
    /// <returns></returns>
    public IEnumerator GlowGaugeImg(Item item)
    {
        switch (item.ItemName)
        {
            case "Thermal Protector": // 2초동안 glow 효과
                SetImageAlpha(temperatureBackgroundImg, 255f);
                yield return new WaitWhile(() => DeepSeaInventoryManager.Instance.IsThermalProtectorActive);
                SetImageAlpha(temperatureBackgroundImg, 100f);
                break;
            case "Oxygen Capsule": // 열 보호 장치가 지속되는 동안 glow 효과
                SetImageAlpha(oxygenBackgroundImg, 255f);
                yield return new WaitForSeconds(2f);
                SetImageAlpha(oxygenBackgroundImg, 100f);
                break;
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha / 255f;
        image.color = color;
    }
}