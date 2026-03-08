using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TorpedoTubeHandle : PuzzleController, IInteractable
{
    [SerializeField] private TorpedoTube torpedoTube; // 어뢰 발사관
    [SerializeField] private GameObject handImage; // 손 이미지
    [SerializeField] private ForcePoint[] forcePoints; // 힘 줘야 하는 위치 배열(풀링)
    [SerializeField] private GameObject torpedoTubeUnlockUI; // 잠금 해제 UI
    [SerializeField] private Image progressImage; // 진행도 이미지
    [SerializeField] private TorpedoAutoLoadSwitch torpedoAutoLoadSwitch;
    [SerializeField] private string negativeKey = "z";
    [SerializeField] private string positiveKey = "c";

    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => false;

    public List<ForcePoint> currentForcePoints = new List<ForcePoint>(); // 현재 힘 줘야 하는 위치 리스트

    private float _finalTargetAngle = 1080f; // 최종 목표 각도
    private float[] _targetAngles = { 300f, 600f, 900f };
    public float[] TargetAngles => _targetAngles;
    private int[] _forcePointCounts = { 1, 2, 2 };
    public int[] ForcePointCounts => _forcePointCounts;
    private int _currentForcePartIndex = 0; // 현재 힘주는 구간 인덱스
    public int CurrentForcePartIndex => _currentForcePartIndex;
    private float _currentAngle = 0f; // 로직상 현재 y 로컬 회전 값 (360도 넘어도 보정 안하고 그대로 값을 비교하기 위해 따로 변수로 관리)
    public float CurrentAngle => _currentAngle;
    public float CurrentRange { get; private set; } // 현재 범위(목표 각도 - 현재 각도)
    public ForcePoint CurrentPressingForcePoint { get; set; } // 현재 누르고 있는 힘 줘야 하는 위치
    public int CurrentCompletePointCnt { get; set; }  // 현재 완료(게이지 1)된 힘 줘야 하는 위치 수

    private void Awake()
    {
        torpedoTubeUnlockUI.SetActive(false);
    }

    #region IInteratable
    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 전력 필요
            return "Power Restoration Required";

        if (torpedoAutoLoadSwitch.IsSwitchOn) // 아직 어뢰 자동 탑재 스위치가 켜져 있는 경우 -> 상호작용 불가
            return "Auto Mode";

        if (torpedoTube.IsOpened) // 열렸을 때 -> 더 이상 상호작용 x
            return "";

        if (torpedoTube.IsUnlocked) // 잠금 해제된 경우 -> 더 이상 상호작용 X, 이미 잠금 해제되었음 메시지
            return "Already Unlocked";

        return "Unlock Door [E]";
    }

    public void Interact()
    {
        if (!LightingManager.instance.IsPowerOn) // 전력 없을 때 -> 상호작용 X
            return;

        if (torpedoAutoLoadSwitch.IsSwitchOn) // 아직 어뢰 자동 탑재 스위치가 켜져 있는 경우 -> 상호작용 불가
            return;

        if (torpedoTube.IsOpened) // 열렸을 때 -> 더 이상 상호작용 x
            return;

        if (torpedoTube.IsUnlocked) // 잠금 해제된 경우 -> 더 이상 상호작용 X
            return;

        ActivatePuzzle();
    }
    #endregion

    #region Puzzle Controller
    public override void StartPuzzle()
    {
        base.StartPuzzle();

        torpedoTube.OnUnlocked += ExitPuzzle; // 완전한 잠금 해제 시 퍼즐 종료

        torpedoTubeUnlockUI.SetActive(true); // UI 켜기

        _currentForcePartIndex = 0;
        CurrentPressingForcePoint = null;
        _currentAngle = 0f;
        progressImage.fillAmount = 0f;

        // torpedoTube.SetLockingDogOutlinesShow(true); // 잠금장치 아웃라인 보이기

        for (int i = 0; i < forcePoints.Length; i++)
        {
            forcePoints[i].FastDecreaseAmount = 0f;
        }

        StartCoroutine(StartHandleRotateCoroutine());
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        torpedoTube.OnUnlocked -= ExitPuzzle;

        StopAllCoroutines();
        for (int i = 0; i < forcePoints.Length; i++)
        {
            forcePoints[i].gameObject.SetActive(false);
        }
        torpedoTubeUnlockUI.SetActive(false); // UI 끄기
    }
    #endregion

    #region 입력 이벤트 함수
    public void OnForceKeyAxis(InputAction.CallbackContext context)
    {
        // 현재 입력값 읽기 (-1, 0, 1)
        float value = context.ReadValue<float>();

        // 아무것도 안 누른 상태 (Canceled)
        if (value == 0)
        {
            if (CurrentPressingForcePoint != null)
            {
                // 현재 누르고 있던 ID 찾기 (A는 0번, D는 1번)
                int lastId = (CurrentPressingForcePoint == currentForcePoints[0]) ? 0 : 1;
                CancelForceKey(lastId);
            }
            return;
        }

        // 키를 새로 누른 상태 (Started/Performed)
        int currentId = (value < 0) ? 0 : 1; // -1이면 A(0), 1이면 D(1)

        // 이미 다른 키를 누르고 있다면 무시 (기존 로직 유지)
        if (CurrentPressingForcePoint != null)
            return;

        StartForceKey(currentId);
    }

    private void StartForceKey(int currentId)
    {
        if (currentId >= _forcePointCounts[_currentForcePartIndex] || !currentForcePoints[currentId].gameObject.activeSelf)
            return;

        CurrentPressingForcePoint = currentForcePoints[currentId];

        currentForcePoints[currentId].SetKeyIconActive(true);

        for (int i = 0; i < currentForcePoints.Count; i++)
        {
            if (i == currentId) continue;
            currentForcePoints[i].SetKeyIconActive(false);
        }

        CurrentPressingForcePoint.StartForce(_forcePointCounts[_currentForcePartIndex]);
    }

    private void CancelForceKey(int currentId)
    {
        if (currentId >= _forcePointCounts[_currentForcePartIndex] || !currentForcePoints[currentId].gameObject.activeSelf)
            return;

        CurrentPressingForcePoint.CancelForce(_forcePointCounts[_currentForcePartIndex]);
        CurrentPressingForcePoint = null;

        for (int i = 0; i < currentForcePoints.Count; i++)
        {
            if (i == currentId) continue;
            currentForcePoints[i].SetKeyIconActive(true);
        }
    }

    public void CompleteForce(ForcePoint forcePoint)
    {
        // 게이지 채우기 성공
        CurrentCompletePointCnt++; // 완료 개수 1 증가
        forcePoint.gameObject.SetActive(false); // 안 보이게 하기
        CurrentPressingForcePoint = null;
        for (int i = 0; i < currentForcePoints.Count; i++)
        {
            if (currentForcePoints[i] == forcePoint) continue;
            currentForcePoints[i].SetKeyIconActive(true);
        }
    }
    #endregion

    #region 퍼즐용 함수
    /// <summary>
    /// 핸들 회전 시작
    /// </summary>
    private IEnumerator StartHandleRotateCoroutine()
    {
        // 시작: 0 ~ 100도까지 정상 회전
        yield return StartCoroutine(RotateWithUpdateProgress(100f));

        StartCoroutine(ForcePartCoroutine());
    }

    /// <summary>
    /// 핸들 회전 끝
    /// </summary>
    private void EndHandleRotate()
    {
        torpedoTubeUnlockUI.SetActive(false); // UI 끄기

        // 성공
        torpedoTube.Unlock(); // 잠금 해제
    }

    /// <summary>
    /// 진행도 갱신하면서 회전
    /// </summary>
    /// <param name="targetAngle">목표 각도</param>
    private IEnumerator RotateWithUpdateProgress(float targetAngle)
    {
        while (_currentAngle < targetAngle)
        {
            // 현재 회전 값 갱신 및 회전
            _currentAngle += 150f * Time.deltaTime;
            if (_currentAngle > targetAngle)
                _currentAngle = targetAngle;
            UpdateProgressUI(Color.green);
            transform.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);

            yield return null;
        }
    }

    /// <summary>
    /// 힘 줘야 하는 구간 코루틴
    /// </summary>
    private IEnumerator ForcePartCoroutine()
    {
        // 힘 줘야 하는 위치 생성
        SpawnForcePoint(_forcePointCounts[_currentForcePartIndex]);
        float startAngle = _currentAngle;
        if (_currentForcePartIndex == 0)
            CurrentRange = _targetAngles[_currentForcePartIndex] - 100f;
        else
            CurrentRange = _targetAngles[_currentForcePartIndex] - (_targetAngles[_currentForcePartIndex - 1] + 100f);

        while (CurrentCompletePointCnt < _forcePointCounts[_currentForcePartIndex])
        {
            // 처음으로 돌아오면 -> 바로 종료 (실패)
            if (_currentAngle <= 0f)
            {
                Debug.Log("Exit");
                ExitPuzzle();
                yield break;
            }

            // 현재 각도 변화량 계산
            float totalAngleAmount = 0f;
            for (int i = 0; i < currentForcePoints.Count; i++)
            {
                // 현재 구간의 힘 줘야 하는 위치들(비활성화된 것도 포함)의 각 게이지가 가리키는 각도의 합
                totalAngleAmount += currentForcePoints[i].CurrentAngleAmount;
            }

            float calculatedAngle = Mathf.Clamp(startAngle + totalAngleAmount, 0f, _targetAngles[_currentForcePartIndex]);
            float delta = calculatedAngle - _currentAngle;

            // 현재 회전 값 갱신 및 회전
            _currentAngle = calculatedAngle;
            if (delta < 0)
                UpdateProgressUI(Color.red);
            else
                UpdateProgressUI(Color.green);
            transform.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);

            yield return null;
        }

        ForceKey.performed -= OnForceKeyAxis;
        ForceKey.canceled -= OnForceKeyAxis;

        if (_currentForcePartIndex == _targetAngles.Length - 1)
        {
            // 핸들 회전 최종 각도까지 하기
            yield return StartCoroutine(RotateWithUpdateProgress(_finalTargetAngle));

            // 핸들 회전 끝 (잠금 해제)
            EndHandleRotate();
        }
        else
        {
            // 정상 회전 100도
            yield return StartCoroutine(RotateWithUpdateProgress(_targetAngles[_currentForcePartIndex] + 100f));

            // 다음 구간 시작
            _currentForcePartIndex++;
            StartCoroutine(ForcePartCoroutine());
        }
    }

    /// <summary>
    /// 힘 줘야 하는 위치 생성
    /// </summary>
    private void SpawnForcePoint(int cnt)
    {
        if (cnt > forcePoints.Length)
            return;

        currentForcePoints.Clear(); // 현재 힘 줘야 하는 위치 리스트 초기화
        CurrentCompletePointCnt = 0; // 현재 완료된 개수 초기화

        float lastAngle = Random.Range(0f, 360f); // 첫 번째 포인트의 시작 각도
        float minAngleGap = 90f; // 두 포인트 사이의 최소 각도 차이 (90도 이상 떨어지게)

        // 개수만큼 랜덤 생성
        for (int i = 0; i < cnt; i++)
        {
            float angle;

            if (i == 0)
            {
                angle = lastAngle;
            }
            else
            {
                // 겹치지 않도록 최소 90도 이상 떨어지도록 다음 각도 설정
                float randomOffset = Random.Range(minAngleGap, 360f - minAngleGap);
                angle = (lastAngle + randomOffset) % 360f;
            }

            // 각도를 좌표로 변환
            float radian = angle * Mathf.Deg2Rad;
            float radius = 0.55f;
            Vector3 spawnPos = new Vector3(Mathf.Cos(radian) * radius, 0.1f, Mathf.Sin(radian) * radius);

            forcePoints[i].transform.localPosition = spawnPos;
            forcePoints[i].FastDecreaseAmount += 350f;
            forcePoints[i].gameObject.SetActive(true);
            currentForcePoints.Add(forcePoints[i]);

            lastAngle = angle;
        }

        // ForceKey 바인딩 경로 동적 변경 (어뢰 발사관마다 다르도록)
        ForceKey.Disable(); // 기존 액션을 잠시 비활성화 (바인딩 변경을 위해 필요)
        ForceKey.ApplyBindingOverride(1, $"<Keyboard>/{negativeKey}");
        ForceKey.ApplyBindingOverride(2, $"<Keyboard>/{positiveKey}");
        ForceKey.Enable(); // 액션 다시 활성화

        ForceKey.performed += OnForceKeyAxis;
        ForceKey.canceled += OnForceKeyAxis;
    }

    /// <summary>
    /// 진행도 UI 갱신
    /// </summary>
    private void UpdateProgressUI(Color color)
    {
        progressImage.color = color;
        progressImage.fillAmount = _currentAngle / _finalTargetAngle;
    }
    #endregion
}
