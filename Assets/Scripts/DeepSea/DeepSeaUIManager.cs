using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeepSeaUIManager : MonoBehaviour
{
    public static DeepSeaUIManager Instance { get; private set; }

    [Header("=== Target Reference ===")]
    [SerializeField] private DeepSeaPlayerMove player;

    [Header("=== Coordinate UI ===")]
    [SerializeField] private TextMeshProUGUI positionText;

    [Header("=== Oxygen UI ===")]
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private TextMeshProUGUI oxygenText;

    private void Start()
    {
        // Player 레퍼런스를 지정하지 않았을 경우 자동 검색
        if (player == null)
        {
            player = FindObjectOfType<DeepSeaPlayerMove>();
        }

        // Slider 최대값 초기화
        if (oxygenSlider != null && player != null)
        {
            oxygenSlider.maxValue = player.MaxOxygen;
        }
    }

    private void Update()
    {
        if (player == null) return;

        UpdatePositionUI();
        UpdateOxygenUI();
    }

    // 1. 좌표 UI 표시
    private void UpdatePositionUI()
    {
        if (positionText == null) return;

        Vector3 pos = player.transform.position;
        positionText.text = $"X: {pos.x:F0} | Y: {pos.y:F0} | Z: {pos.z:F0} | 수심: {player.DisplayDepth:F0}m";
    }

    // 2. 산소 UI 표시
    private void UpdateOxygenUI()
    {
        float currentO2 = player.CurrentOxygen;
        float maxO2 = player.MaxOxygen;

        if (oxygenSlider != null)
        {
            oxygenSlider.value = currentO2;
        }

        if (oxygenText != null)
        {
            oxygenText.text = $"{Mathf.CeilToInt(currentO2)} / {Mathf.CeilToInt(maxO2)}";
        }
    }
}