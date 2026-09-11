using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class OxygenGaugeMinigame : MonoBehaviour
{
    // [Header("Input Settings")]
    // [SerializeField] private PlayerInput playerInput; // Inspector에서 drag & drop 또는 GetComponent
    // private InputAction clickAction;

    [Header("UI")]
    [SerializeField] private GameObject oxgenGaugeMinigameUI;
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private RectTransform indicatorRect;
    [SerializeField] private RectTransform safeZoneRect;

    [Header("Indicator Movement Settings")]
    [SerializeField] private float rightSpeed = 300f;
    [SerializeField] private float leftSpeed = 200f;

    [Header("Safe Zone Settings")]
    [SerializeField] private int totalZoneMoves = 7;
    [SerializeField] private float moveInterval = 3f;

    [SerializeField] private DeepSeaPlayerMove deepSeaPlayerMove;

    private float trackWidth;
    private float indicatorWidth;
    private float safeZoneWidth;

    private float indicatorPosX;
    private bool isClicking;
    private bool isMinigameActive;

    public bool IsInSafeZone { get; private set; }

    private PlayerInput _playerInput;
    private InputAction _leftClickAction;

    private void Awake()
    {
        // 인풋 관련
        _playerInput = FindAnyObjectByType<PlayerInput>();
        _leftClickAction = _playerInput.actions.FindAction("LeftClick");
    }

    private void OnDisable()
    {
        _leftClickAction.started -= OnClickStarted;
        _leftClickAction.canceled -= OnClickCanceled;
    }

    private void OnClickStarted(InputAction.CallbackContext context) => isClicking = true;
    private void OnClickCanceled(InputAction.CallbackContext context) => isClicking = false;

    public void StartMinigame()
    {
        isMinigameActive = true;
        oxgenGaugeMinigameUI.SetActive(true);

        _leftClickAction.started += OnClickStarted;
        _leftClickAction.canceled += OnClickCanceled;

        trackWidth = trackRect.rect.width;
        indicatorWidth = indicatorRect.rect.width;
        safeZoneWidth = safeZoneRect.rect.width;

        indicatorPosX = trackWidth * 0.5f;

        StartCoroutine(Routine_MoveSafeZone());
    }

    public void StopMinigame()
    {
        isMinigameActive = false;
        oxgenGaugeMinigameUI.SetActive(false);
        StopAllCoroutines();
    }

    private void Update()
    {
        if (!isMinigameActive) return;

        UpdateIndicatorPosition();
        CheckSafeZone();
        UpdateUI();
    }

    private void UpdateIndicatorPosition()
    {
        if (isClicking)
        {
            indicatorPosX += rightSpeed * Time.deltaTime;
        }
        else
        {
            indicatorPosX -= leftSpeed * Time.deltaTime;
        }

        float maxPos = trackWidth - indicatorWidth;
        indicatorPosX = Mathf.Clamp(indicatorPosX, 0f, maxPos);
    }

    private void CheckSafeZone()
    {
        float indicatorCenter = indicatorPosX + (indicatorWidth * 0.5f);
        float safeZoneLeft = safeZoneRect.anchoredPosition.x;
        float safeZoneRight = safeZoneLeft + safeZoneWidth;

        IsInSafeZone = (indicatorCenter >= safeZoneLeft && indicatorCenter <= safeZoneRight);
        deepSeaPlayerMove.IsOxgenNonSafe = !IsInSafeZone;
    }

    private void UpdateUI()
    {
        indicatorRect.anchoredPosition = new Vector2(indicatorPosX, indicatorRect.anchoredPosition.y);
    }

    private IEnumerator Routine_MoveSafeZone()
    {
        for (int i = 0; i < totalZoneMoves; i++)
        {
            float minX = 0f;
            float maxX = trackWidth - safeZoneWidth;
            float targetX = Random.Range(minX, maxX);

            float elapsed = 0f;
            float duration = 0.5f;
            Vector2 startPos = safeZoneRect.anchoredPosition;
            Vector2 endPos = new Vector2(targetX, safeZoneRect.anchoredPosition.y);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                safeZoneRect.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }

            safeZoneRect.anchoredPosition = endPos;
            yield return new WaitForSeconds(moveInterval);
        }
    }
}