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

    protected override bool IsHoverRequired => false;

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

        Click.started += OnClickStarted; // 클릭 시작 사용
        Click.canceled += OnClickCanceled; // 클릭 끝 사용
        Point.performed += OnPoint;
        torpedoTube.OnUnlocked += ExitPuzzle; // 완전한 잠금 해제 시 퍼즐 종료

        torpedoTubeUnlockUI.SetActive(true); // UI 켜기

        _currentForcePartIndex = 0;
        CurrentPressingForcePoint = null;
        _currentAngle = 0f;
        progressImage.fillAmount = 0f;

        // torpedoTube.SetLockingDogOutlinesShow(true); // 잠금장치 아웃라인 보이기

        StartCoroutine(StartHandleRotateCoroutine());
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        Click.started -= OnClickStarted;
        Click.canceled -= OnClickCanceled;
        Point.performed -= OnPoint;
        torpedoTube.OnUnlocked -= ExitPuzzle;

        StopAllCoroutines();
        for (int i = 0; i < forcePoints.Length; i++)
        {
            forcePoints[i].gameObject.SetActive(false);
        }
        torpedoTubeUnlockUI.SetActive(false); // UI 끄기
        // torpedoTube.SetLockingDogOutlinesShow(false); // 잠금장치 아웃라인 숨기기
    }
    #endregion

    #region 입력 이벤트 함수
    private void OnClickStarted(InputAction.CallbackContext context)
    {
        // 현재 힘 줘야 하는 위치 중 한 곳을 누르면 -> 회전 각도 증가, 게이지 증가
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ForcePoint forcePoint = hit.collider.GetComponent<ForcePoint>();
            if (forcePoint != null && currentForcePoints.Contains(forcePoint))
            {
                CurrentPressingForcePoint = forcePoint;
                CurrentPressingForcePoint.StartForce(_forcePointCounts[_currentForcePartIndex]);
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (CurrentPressingForcePoint != null)
        {
            CurrentPressingForcePoint.CancelForce(_forcePointCounts[_currentForcePartIndex]);
            CurrentPressingForcePoint = null;
        }
    }

    public override void OnPoint(InputAction.CallbackContext context)
    {
        if (CurrentPressingForcePoint != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ForcePoint forcePoint = hit.collider.GetComponent<ForcePoint>();
                if (forcePoint == null || CurrentPressingForcePoint != forcePoint)
                {
                    CurrentPressingForcePoint.CancelForce(_forcePointCounts[_currentForcePartIndex]);
                    CurrentPressingForcePoint = null;
                }
            }
            else
            {
                CurrentPressingForcePoint.CancelForce(_forcePointCounts[_currentForcePartIndex]);
                CurrentPressingForcePoint = null;
            }
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

        // // 개수만큼 랜덤 생성
        // for (int i = 0; i < cnt; i++)
        // {
        //     Vector2 randomPos = Random.insideUnitCircle.normalized * 0.55f; // 겹치지 않도록 로직 추가 필요!
        //     Vector3 spawnPos = new Vector3(randomPos.x, 0.1f, randomPos.y);
        //     forcePoints[i].transform.localPosition = spawnPos;
        //     forcePoints[i].gameObject.SetActive(true);
        //     currentForcePoints.Add(forcePoints[i]); // 현재 힘 줘야 하는 위치 리스트에 추가
        // }

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
            forcePoints[i].gameObject.SetActive(true);
            currentForcePoints.Add(forcePoints[i]);

            lastAngle = angle;
        }
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
