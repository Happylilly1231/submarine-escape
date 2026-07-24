using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class EqualizingQTE : MonoBehaviour
{
    [Header("이퀄라이징 QTE UI")]
    [SerializeField] private GameObject QTEUI;
    [SerializeField] private Image backgroundImg;
    [SerializeField] private Image progressImg;
    [SerializeField] private Image successZoneImg;
    [SerializeField] private Image perfectZoneImg;
    [SerializeField] private TextMeshProUGUI gaugeText;

    private enum CheckResult { Perfect, Success, Fail }

    private PlayerInput playerInput;
    private InputAction _space;

    // 바늘이 12시 방향일 때의 Z축 시작 회전값
    private const float START_ANGLE_OFFSET = 15f;

    // 바늘 색상 정의
    private Color normalColor = Color.white;
    private Color perfectColor;
    private Color successColor;
    private Color failColor;

    // 게이지 및 진행 상태 정보
    private float currentGauge = 0f;
    private float currentAngle = 0f;

    // 현재 스테이지에 난이도
    private float progressSpeed = 200f;
    private float currentSpeedMultiplier = 1.0f;
    private float successZoneSizePercent = 0.6f;

    // 각도 영역 변수 (0 ~ 360도 기준)
    private float successStartAngle;
    private float successEndAngle;
    private float perfectStartAngle;
    private float perfectEndAngle;

    private bool isWaiting = false;
    private Coroutine resultCoroutine;

    private SeaWaterValve seaWaterValve;

    /// <summary>
    /// 이퀄라이징 QTE 시작
    /// </summary>
    public void StartQTE()
    {
        InputManager.instance.SwitchActionMapWithPermanent("Puzzle");
        FocusManager.Instance.PushFocusState(GameFocusState.Puzzle);

        _space = playerInput.actions["Space"];
        _space.performed += OnCheckTiming;

        QTEUI.SetActive(true);

        progressImg.color = normalColor;

        progressImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, START_ANGLE_OFFSET);
        UpdateStageSettings();
        RandomizeZones();
    }

    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#F5DE9F", out perfectColor);
        ColorUtility.TryParseHtmlString("#56BAFF", out successColor);
        ColorUtility.TryParseHtmlString("#FF5A56", out failColor);
    }

    private void Start()
    {
        seaWaterValve = GetComponent<SeaWaterValve>();

        playerInput = PlayerManager.Instance.playerInput;
    }

    private void Update()
    {
        if (!QTEUI.activeSelf || isWaiting) return;

        // 바늘 회전 (시계 방향)
        float speed = progressSpeed * currentSpeedMultiplier;
        currentAngle += speed * Time.deltaTime;

        if (currentAngle >= 360f)
        {
            // 한 바퀴 돌 동안 입력이 없었다면 Fail 처리 후 영역 재배치
            TriggerResultProcess(CheckResult.Fail);
            return;
        }

        // 바늘 UI 회전 적용
        progressImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(currentAngle - START_ANGLE_OFFSET));
    }

    /// <summary>
    /// 현재 게이지 값에 따라 단계별 난이도 업데이트
    /// </summary>
    private void UpdateStageSettings()
    {
        if (currentGauge < 40f)     // 1단계 (0% ~ 40%)
        {
            currentSpeedMultiplier = 1.0f;
            successZoneSizePercent = 0.3f;
        }
        else if (currentGauge < 80f)    // 2단계 (40% ~ 80%)
        {
            currentSpeedMultiplier = 1.3f;
            successZoneSizePercent = 0.2f;
        }
        else    // 3단계 (80% ~ 100%)
        {
            currentSpeedMultiplier = 1.6f;
            successZoneSizePercent = 0.15f;
        }
    }

    /// <summary>
    /// zone의 위치를 랜덤하게 지정하고 fillAmount와 회전을 업데이트
    /// </summary>
    private void RandomizeZones()
    {
        float successAngleSize = 360f * successZoneSizePercent;
        float perfectAngleSize = 360f * 0.07f;

        // Success Zone의 위치
        successStartAngle = Random.Range(0f, 360f - successAngleSize);
        successEndAngle = successStartAngle + successAngleSize;
        successZoneImg.fillAmount = successZoneSizePercent;
        backgroundImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -successStartAngle);

        // Perfect Zone의 위치
        perfectEndAngle = successEndAngle;
        perfectStartAngle = perfectEndAngle - perfectAngleSize;
        perfectZoneImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -perfectStartAngle);
    }

    private void TriggerResultProcess(CheckResult result)
    {
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
        }
        resultCoroutine = StartCoroutine(Co_ProcessResultAndReset(result));
    }

    /// <summary>
    /// 결과를 적용하고 1초간 멈춘 뒤 존을 재배치하고 바늘을 다시 움직임
    /// </summary>
    private IEnumerator Co_ProcessResultAndReset(CheckResult result)
    {
        isWaiting = true;

        // 바늘 이미지 색상 변경
        SetProgressImgColor(result);

        // 100% 달성해서 UI가 비활성화되면 대기 로직 생략
        if (!QTEUI.activeSelf) yield break;

        yield return new WaitForSeconds(1.0f);

        // 게이지 증가/감소 및 상태 업데이트
        QTECheck(result);

        // 1초 후 바늘 원래 색상으로 복구회전 각도 리셋 및 영역 재설정
        progressImg.color = normalColor;

        // 회전 각도 리셋 및 영역 재설정
        currentAngle = 0f;
        progressImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        RandomizeZones();

        isWaiting = false;
    }

    /// <summary>
    /// space키 입력 시 QTE 결과 판정
    /// </summary>
    /// <param name="context"></param>
    private void OnCheckTiming(InputAction.CallbackContext context)
    {
        Debug.Log(perfectStartAngle + " " + perfectEndAngle);
        Debug.Log(successStartAngle + " " + successEndAngle);
        Debug.Log(currentAngle);
        if (currentAngle >= perfectStartAngle && currentAngle <= perfectEndAngle)
        {
            TriggerResultProcess(CheckResult.Perfect);
        }
        else if (currentAngle >= successStartAngle && currentAngle <= successEndAngle)
        {
            TriggerResultProcess(CheckResult.Success);
        }
        else
        {
            TriggerResultProcess(CheckResult.Fail);
        }
    }

    /// <summary>
    /// 판정 결과에 따른 바늘 이미지 색상 적용
    /// <para> - perfect: 노란색 </para>
    /// <para> - success: 파란색 </para>
    /// <para> - fail: 빨간색 </para>
    /// </summary>
    /// <param name="result"></param>
    private void SetProgressImgColor(CheckResult result)
    {
        switch (result)
        {
            case CheckResult.Perfect:
                progressImg.color = perfectColor;
                break;
            case CheckResult.Success:
                progressImg.color = successColor;
                break;
            case CheckResult.Fail:
                progressImg.color = failColor;
                break;
        }
    }

    /// <summary>
    /// 게이지 증가/감소 및 상태 업데이트
    /// </summary>
    /// <param name="result"></param>
    private void QTECheck(CheckResult result)
    {
        switch (result)
        {
            case CheckResult.Perfect:
                currentGauge += 20f;
                Debug.Log("<color=green>PERFECT! (+20%)</color>");
                break;
            case CheckResult.Success:
                currentGauge += 10f;
                Debug.Log("<color=cyan>SUCCESS! (+10%)</color>");
                break;
            case CheckResult.Fail:
                currentGauge -= 10f;
                Debug.Log("<color=red>FAIL! (-10%)</color>");
                break;
        }

        // 게이지 클램핑 (0% ~ 100%)
        currentGauge = Mathf.Clamp(currentGauge, 0f, 100f);

        gaugeText.text = $"{Mathf.RoundToInt(currentGauge)}%";

        UpdateStageSettings();

        // 100% 달성 시 탈출 성공 처리
        if (currentGauge >= 100f)
        {
            EqualizingComplete();
        }
    }

    /// <summary>
    /// 이퀄라이징 완료 -> 탈출 연출 재생
    /// </summary>
    private void EqualizingComplete()
    {
        QTEUI.SetActive(false);

        _space.performed -= OnCheckTiming;

        SubmarineInGameManager.instance.SetEscaped();
        seaWaterValve.Escape();
        Debug.Log("<b>이퀄라이징 성공! 압력 평형 완료. 잠수함 탈출!</b>");
    }
}
