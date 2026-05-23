using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 레이더 컨트롤러
/// /// <para>- 수동 전환 로직</para>
/// <para>- 심해 괴물과 다른 잠수함의 위치를 계산한다.</para>
/// <para>- 레이더 UI가 활성화되어 있을 때 레이더 UI에서 위치를 표시한다.</para>
/// <para>- 레이더 UI에서 거리와 높이를 선택하여 어뢰를 발사할 수 있도록 한다.</para>
/// </summary>
public class ExRadarController : MonoBehaviour
{
    [Header("Radar UI")]
    [SerializeField] private GameObject _radarUI; // 레이더 UI
    [SerializeField] private RectTransform radarArea; // 레이더 구역
    [SerializeField] private TextMeshProUGUI logText; // 로그 화면 텍스트
    [SerializeField] private RectTransform torpedoGroup; // 어뢰 그룹
    [SerializeField] private Button exitButton; // 종료 버튼

    [Header("Radar Target")]
    [SerializeField] private RectTransform originDot; // 원점(현재 잠수함 점)
    [SerializeField] private RectTransform monsterDot; // 심해 괴물 점
    [SerializeField] private RectTransform submarine2Dot; // 다른 잠수함 점
    [SerializeField] private TextMeshProUGUI monsterPosText; // 심해 괴물 좌표 텍스트
    [SerializeField] private TextMeshProUGUI submarine2PosText; // 다른 잠수함 좌표 텍스트

    [Header("Select & Real Fire")]
    [SerializeField] private RectTransform selectionIndicator; // 선택 표시
    [SerializeField] private RectTransform distanceLine; // 거리 선
    [SerializeField] private RectTransform realTorpedo; // 실제 어뢰
    [SerializeField] private RectTransform realExplosionRange; // 폭발 범위
    [SerializeField] private Button fireButton; // 발사 버튼

    [Header("Target Height Setting UI")]
    [SerializeField] private GameObject heightLabelGroup; // 높이 라벨 그룹
    [SerializeField] private RectTransform animTorpedo; // 애니메이션 어뢰
    [SerializeField] private RectTransform animTargetDot; // 애니메이션 타겟 위치
    [SerializeField] private RectTransform animDistanceLine; // 애니메이션 타겟 위치
    [SerializeField] private RectTransform animExplosionRange; // 애니메이션 폭발 범위

    [Header("Before Changing To Manual Mode")]
    [SerializeField] private GameObject redBackground; // 빨간 배경(수동 전환 전 조작 막기 용도)
    [SerializeField] private MeshRenderer monitorScreenRenderer; // 레이더 모니터 오브젝트 렌더러

    public RadarTarget deepSeaMonster; // 심해 괴물 (데이터)
    public RadarTarget submarine2; // 다른 잠수함 (데이터)

    private bool _isUpdateStart = false;
    public bool IsUpdateStart { get => _isUpdateStart; set => _isUpdateStart = value; }

    // 수동 전환
    private bool _canType = true; // 코드 입력 가능 여부
    private const string CHANGE_MODE_HELP_TEXT = "[ERROR]: 어뢰의 자동 추적 발사가 불가능합니다.\n수동 전환 코드를 입력하세요.\n"; // 모드 변경 안내 메시지
    private const string CORRECT_CODE = "XJHU"; // 정답 코드
    private string _currentInput = ""; // 현재 입력
    private const int MAX_LENGTH = 4; // 입력 제한 길이
    private bool _isManualModeActive = false; // 수동 모드 활성화 여부

    // UI
    private bool _isUIVisible = false; // UI 보여주는 중인지 여부

    // 범위
    private float _radarRadius = 325f; // 레이더 화면 너비 절반
    private float _maxDistance = 100f; // 실제 최대 거리
    private float _maxHeight = 50f;  // 실제 최대 높이(Z 최대 표현값)

    // 선택
    private bool _isDistanceSelected = false; // 거리 선택 여부
    private bool _isHeightSelected = false; // 높이 선택 여부
    private float _targetHeight; // 현재 목표 높이
    private GameObject[] _heightLabels = new GameObject[11]; // 높이 라벨 배열
    private int _currentHeightLableIndex = 0;

    // 어뢰 발사
    private bool _isFiring = false; // 발사 중 여부
    private float _torpedoSpeed = 150f; // 어뢰 속도
    private float _explosionDistance = 10f; // 폭발 반경
    private int _currentTorpedoIndex = 0; // 현재 어뢰 인덱스
    private Coroutine _currentFireAnimCoroutine = null; // 현재 어뢰 발사 애니메이션 코루틴
    private Vector2 _animTargetPos; // 애니메이션에서의 타겟 위치
    private float _fadeDuration = 3f;

    // 선택
    private Vector2 _currentSelectPos; // 현재 선택 위치
    private RectTransform _clickedTarget = null; // 현재 선택 대상

    // 심해 괴물
    private Image _monsterDotImg; // 심해 괴물 점 이미지
    private Vector3 _monsterPos; // 심해 괴물 위치
    private bool _isUpdatingMonsterPos = true; // 심해 괴물 위치 갱신 중인지 여부
    private int _currentMonsterPeriodIndex = 0; // 심해 괴물 주기 인덱스(몇번째 출현인가)
    private float _monsterTimer = 0f; // 심해 괴물의 타이머(다시 나타날 때 0으로 초기화)
    private float startAngle = 90f; // 시작 각도(심해 괴물의 시작 위치 변경 시 사용)
    private float _monsterWaitTime = 300f; // 심해 괴물 재등장 대기 시간: 5분
    private float[] _monsterPeriods = { 600f / 0.7712f, 1020f / 0.7712f, 1020f / 0.7712f, 1020f / 0.7712f }; // 심해 괴물이 다가오기까지 걸리는 시간: 10분, 17분, 17분 (잠수함과의 거리가 5f 되는 시점이 전체 시간의 0.771f 정도이기 때문에 해당 시점을 원하는 시간으로 맞추기 위해 0.7712f로 원하는 시간으로 나누어준다.(약간의 널널함을 주기 위해 0.0002f 더함))

    // 다른 잠수함
    private Image _subamrine2DotImg; // 다른 잠수함 점 이미지
    private Vector3 _submarine2Pos; // 다른 잠수함 위치 갱신 중인지 여부
    private bool _isUpdatingSubmarine2Pos = true; // 다른 잠수함 점 보여주는 중인지 여부
    private float _submarine2Period = 540f / 0.7712f; // 다른 잠수함 한 바퀴 주기: 9분 (괴물과 주기를 맞추기 위해 0.7712f로 똑같이 나누어줌)

    private float _maxR = 12f; // 최대 R 수치(정규화할 때 필요)
    private float _maxZ = 4f; // 최대 Z 수치(정규화할 때 필요)

    // 사운드
    [Header("Sound")]
    [SerializeField] private AudioClip deepSeaMonsterCloseSound;
    [SerializeField] private AudioClip radarOpenSound;
    [SerializeField] private AudioClip radarCloseSound;
    [SerializeField] private AudioClip modeChangeFailSound;
    [SerializeField] private AudioClip modeChangeSuccessSound;
    private float _deepMonsterVolumeMaxDistance = 10f; // 최대 볼륨 되기 시작하는 거리
    [SerializeField] private AudioSource _monsterAudioSource;

    private void Start()
    {
        // 점 이미지 가져오기
        _monsterDotImg = monsterDot.gameObject.GetComponent<Image>();
        _subamrine2DotImg = submarine2Dot.gameObject.GetComponent<Image>();

        // 버튼 함수 할당
        // 높이 선택 버튼 -> 높이 설정 함수 할당
        float height = _maxHeight;
        foreach (Transform t in heightLabelGroup.transform)
        {
            float h = height;
            int idx = _currentHeightLableIndex;
            _heightLabels[idx] = t.gameObject;
            _heightLabels[idx].GetComponent<Button>().onClick.AddListener(() => OnHeightLabelClicked(h, idx));
            _currentHeightLableIndex++;
            height -= 10f;
        }
        // 발사 버튼 함수 할당
        fireButton.onClick.AddListener(OnFireButtonClicked);
        // 종료 버튼 함수 할당
        exitButton.onClick.AddListener(() => SetRadarVisible(false));

        // 초기 상태: 수동 전환 필요 상태
        logText.color = Color.red;
        redBackground.SetActive(true);

        // 초기 위치로 이동
        UpdateMonsterPos(0f);
        UpdateSubmarine2Pos(0f);
    }

    private void Update()
    {
        // 정지 중이거나 아직 업데이트 시작 안됐을 때(전력 복구 X) -> 아무것도 안 함
        if (SubmarineInGameManager.instance.IsPausing || !_isUpdateStart)
            return;

        // 심해 괴물 위치 갱신 중 -> 심해 괴물 타이머 계산 (보여지는 중 아닐 때는 사라졌을 때이므로 계산 X)
        if (_isUpdatingMonsterPos)
            _monsterTimer += Time.deltaTime;

        // 각각 위치가 갱신 중일 때만 심해 괴물과 다른 잠수함 위치 갱신
        if (_isUpdatingMonsterPos) UpdateMonsterPos(_monsterTimer);
        if (_isUpdatingSubmarine2Pos) UpdateSubmarine2Pos(GameTime.Instance.TimeSinceStart);
    }

    private void LateUpdate()
    {
        // UI 보여지는 중이고, 심해 괴물이 현재 선택 대상일 때만 -> 심해 괴물 좌표 텍스트가 구역 바깥으로 나가서 가려지지 않도록 보정
        if (_isUIVisible && _clickedTarget == monsterDot)
            ClampPosTextsInside(monsterPosText, monsterDot);
    }

    /// <summary>
    /// 레이더 보이기 여부 설정
    /// </summary>
    /// <param name="visible">보이기 여부</param>
    public void SetRadarVisible(bool visible)
    {
        if (visible) // 보이기
        {
            // 수동 전환이 아직 되지 않았을 때 -> 키 입력 이벤트 구독(수동 전환 코드 입력 위해서)
            if (!_isManualModeActive)
            {
                Keyboard.current.onTextInput += OnTextInput;
                _currentInput = "";
                logText.text = CHANGE_MODE_HELP_TEXT + "> ";
                // SubmarineInGameManager.instance.player.GetComponent<PlayerInput>().actions.Disable(); // 인풋 막기
            }

            _isUIVisible = true; // UI 보이는 중으로 설정
            _radarUI.SetActive(true); // 레이더 UI 활성화

            // 선택 여부 초기화
            SelectDistance(false);
            SelectHeight(false);

            // 실제 어뢰 발사 UI 요소 숨기기
            distanceLine.gameObject.SetActive(false);
            realTorpedo.gameObject.SetActive(false);
            realExplosionRange.gameObject.SetActive(false);

            // 애니메이션 어뢰 발사 UI 요소 숨기기
            animTargetDot.gameObject.SetActive(false);
            animTorpedo.gameObject.SetActive(false);
            animDistanceLine.gameObject.SetActive(false);
            animExplosionRange.gameObject.SetActive(false);

            // 클릭 대상 초기화
            _clickedTarget = null;
            monsterPosText.gameObject.SetActive(false);
            submarine2PosText.gameObject.SetActive(false);

            // 레이더 열리는 소리
            AudioManager.Instance.PlayGlobalOneShot(radarOpenSound);
        }
        else // 숨기기
        {
            // UI에 포커스 비활성화
            SubmarineInGameManager.instance.SetPuzzleFocus(false);

            // 수동 전환이 아직 되지 않았을 때 -> 키 입력 이벤트 구독 해제
            if (!_isManualModeActive)
            {
                Keyboard.current.onTextInput -= OnTextInput;
                // SubmarineInGameManager.instance.player.GetComponent<PlayerInput>().actions.Enable();
            }

            _isUIVisible = false; // UI 보이는 중 아님으로 설정
            _radarUI.SetActive(false); // 레이더 UI 비활성화

            // 로그 텍스트 초기화
            logText.text = "";

            // 레이더 꺼지는 소리
            AudioManager.Instance.PlayGlobalOneShot(radarCloseSound);
        }
    }

    #region Mode Change
    /// <summary>
    /// 텍스트 입력될 때 실행되는 함수
    /// </summary>
    /// <param name="c">입력된 문자</param>
    private void OnTextInput(char c)
    {
        // 입력 불가 시 or 게임 정지 중 -> 아무것도 안 하고 종료
        if (!_canType || SubmarineInGameManager.instance.IsPausing) return;

        // 엔터 -> 제출
        if (c == '\n' || c == '\r')
        {
            StartCoroutine(SubmitCoroutine()); // 제출
            return;
        }

        // 백스페이스 -> 지우기
        if (c == '\b')
        {
            if (_currentInput.Length > 0)
                _currentInput = _currentInput[..^1];

            logText.text = CHANGE_MODE_HELP_TEXT + "> " + _currentInput;
            return;
        }

        // 알파벳 아닌 경우 -> 입력 불가
        if (!(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z'))
        {
            return;
        }

        // 현재 길이가 4글자 이상이면 -> 더 이상 입력 불가
        if (_currentInput.Length >= MAX_LENGTH)
            return;

        // 대문자로 변경 후 현재 입력 텍스트에 추가
        _currentInput += char.ToUpper(c);

        // 로그 텍스트 변경
        logText.text = CHANGE_MODE_HELP_TEXT + "> " + _currentInput;
    }

    /// <summary>
    /// 제출 코루틴(성공 / 실패)
    /// </summary>
    /// <returns></returns>
    IEnumerator SubmitCoroutine()
    {
        if (_currentInput == CORRECT_CODE) // 정답 코드
        {
            // 수동 모드로 전환
            _isManualModeActive = true;
            logText.text = "[ACCESS GRANTED]";

            // 색 변경
            logText.color = Color.green; // 로그 텍스트 색 -> 초록색으로 변경
            // 레이더 모니터 색 -> 초록색으로 변경
            monitorScreenRenderer.material.color = Color.green;
            monitorScreenRenderer.material.EnableKeyword("_EMISSION");
            monitorScreenRenderer.material.SetColor("_EmissionColor", Color.green);
            // 빨간 배경 제거
            redBackground.SetActive(false);

            // 입력 불가
            _canType = false;
            Keyboard.current.onTextInput -= OnTextInput; // 입력 이벤트 구독 해제

            AudioManager.Instance.PlayGlobalOneShot(modeChangeSuccessSound);

            // 1.5초 대기
            yield return new WaitForSeconds(1.5f);

            // 인풋 막았던 거 해제
            // SubmarineInGameManager.instance.player.GetComponent<PlayerInput>().actions.Enable();

            // 텍스트 초기화
            _currentInput = "";
            logText.text = "";
        }
        else // 실패
        {
            // 실패 메시지 띄우기
            logText.text = "[INVALID CODE]";

            AudioManager.Instance.PlayGlobalOneShot(modeChangeFailSound);

            // 1.5초 대기(대기하는 동안 입력 불가)
            _canType = false;
            yield return new WaitForSeconds(1.5f);
            _canType = true;

            // 텍스트 초기화
            _currentInput = "";
            logText.text = CHANGE_MODE_HELP_TEXT + "> ";
        }
    }
    #endregion

    #region Update Pos
    /// <summary>
    /// 심해 괴물 위치 갱신 
    /// </summary>
    /// <param name="timer">타이머</param>
    private void UpdateMonsterPos(float timer)
    {
        // 시간, 필요 수식
        float t = (timer % _monsterPeriods[_currentMonsterPeriodIndex]) / _monsterPeriods[_currentMonsterPeriodIndex] * 10f; // t가 0 ~ 10이므로 그에 맞춤
        float R = 10f - t + 2f * Mathf.Sin(3f * t);
        float Z = 2f * Mathf.Sin(4f * t) * (1f - t / 10f);
        float angle = startAngle + 5f * t;

        // R, z 정규화 (-1 ~ 1)
        float normalized = Mathf.Clamp(R / _maxR, -1f, 1f);
        float zNormalized = Mathf.Clamp(Z / _maxZ, -1f, 1f);

        // 정규화된 위치(x, y, z)
        Vector3 radarNormPos;
        radarNormPos.x = normalized * Mathf.Cos(angle);
        radarNormPos.y = normalized * Mathf.Sin(angle);
        radarNormPos.z = zNormalized;

        // 실제 게임 상 위치(x, y, z)
        _monsterPos.x = radarNormPos.x * _maxDistance;
        _monsterPos.y = radarNormPos.y * _maxDistance;
        _monsterPos.z = radarNormPos.z * _maxHeight;

        // UI 보일 때만 -> UI 갱신
        if (_isUIVisible)
        {
            // UI 위치(x, y)
            Vector2 uiPos = new Vector2(radarNormPos.x, radarNormPos.y) * _radarRadius;

            // 점 위치, 색상, 크기 갱신
            monsterDot.anchoredPosition = uiPos;
            _monsterDotImg.color = new Color(0, 1, 0, 0.7f + zNormalized * 0.3f); // 높이가 올라가면 진해짐(높이 0 기준 0.7f)
            monsterDot.localScale = Vector3.one * (1f + zNormalized * 0.3f); // 높이가 올라가면 커짐(높이 0 기준 1f)

            // 현재 심해 괴물이 선택된 상태면 -> 심해 괴물 위치 텍스트 갱신
            if (_clickedTarget == monsterDot)
            {
                monsterPosText.text = $"({_monsterPos.x:F0}, {_monsterPos.y:F0}, {_monsterPos.z:F0})";
            }
        }

        // 거리에 따른 로직
        float monsterDistance = Vector3.Distance(_monsterPos, Vector3.zero);
        if (timer >= 1200f) // 20분이 되면(잠수함과의 거리가 거의 5f가 되는 시점) -> 폭발, 게임 종료
        {
            Debug.Log("폭발!!! " + timer);
            AudioManager.Instance.PlayDeepSeaMonsterExplosionSound(); // 폭발 소리
            GameManager.instance.GameOver(EEndingType.SubmarineExplode);
        }
        else if (monsterDistance <= _maxDistance / 2) // 잠수함과 어느 정도 가까워지면 -> 소리나기 시작(가까워질수록 커짐)
        {
            Debug.Log("가까워지기 시작: " + timer);
            AudioManager.Instance.PlaySoundSafe(_monsterAudioSource, deepSeaMonsterCloseSound);
            float v = Mathf.InverseLerp(_maxDistance / 2, _deepMonsterVolumeMaxDistance, monsterDistance);
            _monsterAudioSource.volume = v; // 0~1
        }
        else // 잠수함과 거리 먼 경우 -> 소리 안 남
        {
            if (_monsterAudioSource.isPlaying)
            {
                _monsterAudioSource.Stop();
                _monsterAudioSource.volume = 0f;
            }
        }
    }

    /// <summary>
    /// 다른 잠수함 위치 갱신
    /// </summary>
    /// <param name="timer">타이머</param>
    private void UpdateSubmarine2Pos(float timer)
    {
        // 시간, 필요 수식
        float t = (timer % _submarine2Period) / _submarine2Period * 10f; // t가 0 ~ 10이므로 그에 맞춤
        float R = 7f + Mathf.Cos(12f * Mathf.PI * (t / 10f));
        float angle = 2f * Mathf.PI * (t / 10f);
        float Z = -3f + Mathf.Sin(angle);

        // R, z 정규화 (-1 ~ 1)
        float normalized = Mathf.Clamp01(R / _maxR); // (0 ~ 1)
        float zNormalized = Mathf.Clamp(Z / _maxZ, -1f, 1f);

        // 정규화된 위치(x, y, z)
        Vector3 radarNormPos;
        radarNormPos.x = normalized * Mathf.Cos(angle);
        radarNormPos.y = normalized * Mathf.Sin(angle);
        radarNormPos.z = zNormalized;

        // 실제 게임 상 위치(x, y, z)
        _submarine2Pos.x = radarNormPos.x * _maxDistance;
        _submarine2Pos.y = radarNormPos.y * _maxDistance;
        _submarine2Pos.z = radarNormPos.z * _maxHeight;

        // UI 보일 때만 -> UI 갱신
        if (_isUIVisible)
        {
            // UI 위치(x, y)
            Vector2 uiPos = new Vector2(radarNormPos.x, radarNormPos.y) * _radarRadius;

            // 점 위치, 색상, 크기 갱신
            submarine2Dot.anchoredPosition = uiPos;
            _subamrine2DotImg.color = new Color(0, 1, 0, 0.7f + zNormalized * 0.3f); // 높이가 올라가면 진해짐(높이 0 기준 0.7f)
            submarine2Dot.localScale = Vector3.one * (1f + zNormalized * 0.3f); // 높이가 올라가면 커짐(높이 0 기준 1f)

            // 현재 다른 잠수함 선택된 상태면 -> 다른 잠수함 위치 텍스트 갱신
            if (_clickedTarget == submarine2Dot)
            {
                submarine2PosText.text = $"({_submarine2Pos.x:F0}, {_submarine2Pos.y:F0}, {_submarine2Pos.z:F0})";
            }
        }
    }
    #endregion

    #region Select
    /// <summary>
    /// 거리 선택
    /// </summary>
    /// <param name="isSelect">선택 여부</param>
    private void SelectDistance(bool isSelect)
    {
        _isDistanceSelected = isSelect;
        SetFireButtonInteractable(); // 발사 버튼 활성화 여부 설정
    }

    /// <summary>
    /// 높이 선택
    /// </summary>
    /// <param name="isSelect">선택 여부</param>
    private void SelectHeight(bool isSelect)
    {
        if (_isHeightSelected && _currentHeightLableIndex < _heightLabels.Length)
        {
            Color c = _heightLabels[_currentHeightLableIndex].GetComponent<Image>().color;
            c.g = 145f / 255f;
            _heightLabels[_currentHeightLableIndex].GetComponent<Image>().color = c;
        }

        _isHeightSelected = isSelect;
        SetFireButtonInteractable(); // 발사 버튼 활성화 여부 설정
    }

    /// <summary>
    /// 발사 버튼 활성화 여부 설정
    /// </summary>
    private void SetFireButtonInteractable()
    {
        // 거리나 높이가 하나라도 선택되지 않았을 때 or 어뢰 3번 발사한 이후 or 현재 발사 중 or 현재 경보 발생 중일 때 -> 비활성화
        if (!_isDistanceSelected || !_isHeightSelected || _currentTorpedoIndex >= 3 || _isFiring || SubmarineInGameManager.instance.IsAlerting)
        {
            // 비활성화
            fireButton.interactable = false;
            fireButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(155f / 255f, 155f / 255f, 155f / 255f);
        }
        else // 위 경우가 아닌 경우에만 활성화
        {
            // 활성화
            fireButton.interactable = true;
            fireButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 146f / 255f, 0f);
        }
    }

    /// <summary>
    /// 현재 선택된 위치 갱신 -> 거리 선택
    /// </summary>
    /// <param name="clicked">선택된 대상</param>
    /// <param name="currentPos">현재 선택 위치</param>
    public void UpdateCurrentSelectPos(GameObject clicked, Vector2 currentPos)
    {
        // 실제 어뢰 발사 중일 때 -> 조작 불가능(거리 선택 불가능)
        if (_isFiring)
            return;

        logText.text = "";
        _currentSelectPos = currentPos;

        // 거리가 선택되지 않은 상태라면 -> 거리 선택됨으로 설정
        if (!_isDistanceSelected)
            SelectDistance(true);

        if (clicked == monsterDot.gameObject) // 심해 괴물 클릭 시
        {
            Debug.Log("심해 괴물 클릭!");
            _clickedTarget = monsterDot;
            monsterPosText.gameObject.SetActive(true);
            submarine2PosText.gameObject.SetActive(false);
            logText.text = "[Target Selected]";
        }
        else if (clicked == submarine2Dot.gameObject) // 다른 잠수함 클릭 시
        {
            Debug.Log("다른 잠수함 클릭!");
            _clickedTarget = submarine2Dot;
            submarine2PosText.gameObject.SetActive(true);
            monsterPosText.gameObject.SetActive(false);
            logText.text = "[Target Selected]";
        }
        else // 그 외 다른 위치 클릭 시
        {
            Vector2 currentRealSelectPos = new Vector2(
                _currentSelectPos.x / _radarRadius * _maxDistance,
                _currentSelectPos.y / _radarRadius * _maxDistance);

            logText.text = $"Current Selected Pos: ({currentRealSelectPos.x:F0}, {currentRealSelectPos.y:F0})";
        }

        // 선택 표시 UI
        if (!selectionIndicator.gameObject.activeSelf)
            selectionIndicator.gameObject.SetActive(true);
        selectionIndicator.anchoredPosition = _currentSelectPos;

        // 현재 선택 위치를 타겟으로 선 그리기
        DrawLineUI(_currentSelectPos, distanceLine);

        // 애니메이션 UI는 초기화함(거리 새로 선택될 때마다 무조건 초기화함, 높이 선택 안됨으로 설정)
        ResetAnimUI();
    }

    /// <summary>
    /// 선 그리기
    /// </summary>
    /// <param name="targetPos">타겟 위치</param>
    /// <param name="line">선 이미지 RectTransform</param>
    private void DrawLineUI(Vector2 targetPos, RectTransform line)
    {
        // 선 활성화
        if (!line.gameObject.activeSelf)
            line.gameObject.SetActive(true);

        // 원점, 방향, 거리 설정
        Vector2 originPos = originDot.anchoredPosition;
        Vector2 dir = targetPos - originPos;
        float distance = dir.magnitude;
        dir = dir.normalized; // 방향 정규화

        // 선이 원점에서 타겟까지 이어지도록 함
        line.anchoredPosition = originDot.anchoredPosition;
        line.sizeDelta = new Vector2(distance, line.sizeDelta.y); // 길이

        // 해당 방향으로 선 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 높이 라벨 버튼 클릭
    /// </summary>
    /// <param name="height">높이</param>
    public void OnHeightLabelClicked(float height, int idx)
    {
        // 실제 어뢰 발사 중일 때 -> 높이 선택 불가능
        if (_isFiring)
            return;

        _targetHeight = height;
        SelectHeight(true);
        _currentHeightLableIndex = idx;
        Color c = _heightLabels[idx].GetComponent<Image>().color;
        _heightLabels[idx].GetComponent<Image>().color = new Color(161f / 255f, 1f, 65f / 255f, c.a);


        // 거리가 현재 선택되어 있을 때
        if (_isDistanceSelected)
        {
            if (_currentFireAnimCoroutine != null)
            {
                StopCoroutine(_currentFireAnimCoroutine);
            }
            _currentFireAnimCoroutine = StartCoroutine(FireAnimation());
        }
    }
    #endregion

    /// <summary>
    /// 애니메이션 UI 리셋
    /// </summary>
    private void ResetAnimUI()
    {
        SelectHeight(false);
        if (_currentFireAnimCoroutine != null)
        {
            StopCoroutine(_currentFireAnimCoroutine);
            _currentFireAnimCoroutine = null;
        }
        animTargetDot.gameObject.SetActive(false);
        animTorpedo.gameObject.SetActive(false);
        animDistanceLine.gameObject.SetActive(false);
        animExplosionRange.gameObject.SetActive(false);
    }

    #region Fire Animation
    /// <summary>
    /// 어뢰 발사 애니메이션 코루틴
    /// </summary>
    IEnumerator FireAnimation()
    {
        // 원점
        Vector2 originPos = Vector2.zero;

        // 애니메이션 타겟
        float distance = (_currentSelectPos - originPos).magnitude; // 거리 계산
        animTargetDot.gameObject.SetActive(true); // 애니메이션 타겟 활성화
        _animTargetPos = new Vector2(distance / _radarRadius * 280f, _targetHeight * 5f); // 애니메이션 타겟 위치 계산
        animTargetDot.anchoredPosition = _animTargetPos; // 애니메이션 타겟 위치 설정

        // 애니메이션 어뢰에서 타겟까지 선 잇기
        DrawLineUI(_animTargetPos, animDistanceLine);

        // 변수 설정
        Vector2 targetPos = _animTargetPos;
        RectTransform line = animDistanceLine;
        RectTransform torpedo = animTorpedo;
        RectTransform explosionRange = animExplosionRange;

        // 방향 계산
        Vector2 dir = targetPos - originPos;
        dir = dir.normalized;

        // 방향에 맞춘 회전 각도 계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 어뢰 활성화, 위치 설정, 각도 설정
        torpedo.gameObject.SetActive(true);
        torpedo.anchoredPosition = originPos;
        torpedo.localRotation = Quaternion.Euler(0, 0, angle);

        // 현재 어뢰 위치
        Vector2 currentTorpedoPos = originPos;

        // 거리 선택과 높이 선택이 모두 되어있을 때만 -> 애니메이션 반복 재생
        while (_isDistanceSelected && _isHeightSelected)
        {
            // 어뢰 위치 업데이트
            currentTorpedoPos += dir * _torpedoSpeed * Time.deltaTime;
            torpedo.anchoredPosition = currentTorpedoPos;

            // 현재 목표 위치에 도달하면
            if (Vector2.Distance(currentTorpedoPos, targetPos) <= _torpedoSpeed * Time.deltaTime)
            {
                // 폭발
                explosionRange.gameObject.SetActive(true);
                explosionRange.anchoredPosition = targetPos;

                // 대기 후 폭발 범위 끄기
                yield return new WaitForSeconds(2f);
                explosionRange.gameObject.SetActive(false);

                // 현재 어뢰 위치
                currentTorpedoPos = originPos;

            }

            yield return null;
        }

        // 초기화
        animTargetDot.gameObject.SetActive(false); // 애니메이션 타겟 끄기
        torpedo.gameObject.SetActive(false); // 어뢰 끄기
        line.gameObject.SetActive(false); // 선 끄기
    }
    #endregion

    #region Real Fire
    /// <summary>
    /// 발사 버튼 클릭 시 함수 -> 실제 어뢰 발사
    /// </summary>
    public void OnFireButtonClicked()
    {
        // 거리 / 높이에서 선택되지 않은 것이 있다면 -> 어뢰 발사 불가능
        if (!_isDistanceSelected || !_isHeightSelected || _currentTorpedoIndex >= 3)
        {
            Debug.Log("어뢰 발사 불가능");
            return;
        }

        // 실제 어뢰 발사
        StartCoroutine(FireRealTorpedo());
        SetFireButtonInteractable(); // 발사 버튼 활성화 여부 설정

        // 경보 발생
        SubmarineInGameManager.instance.AlertOn();
    }

    /// <summary>
    /// 어뢰 실제 발사 코루틴
    /// </summary>
    IEnumerator FireRealTorpedo()
    {
        // 변수 설정
        Vector2 originPos = Vector2.zero;
        Vector2 targetPos = _currentSelectPos;
        RectTransform line = distanceLine;
        RectTransform torpedo = realTorpedo;
        RectTransform explosionRange = realExplosionRange;

        // 발사 중으로 설정
        _isFiring = true;

        // 어뢰 소모
        torpedoGroup.GetChild(_currentTorpedoIndex).GetChild(0).gameObject.SetActive(false); // 현재 발사할 어뢰 UI에서 비활성화 
        _currentTorpedoIndex++; // 현재 어뢰 인덱스 증가

        // 방향 계산
        Vector2 dir = targetPos - originPos;
        dir = dir.normalized;
        Debug.Log(dir);

        // 방향에 맞춘 회전 각도 계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 어뢰 활성화, 위치 설정, 각도 설정
        torpedo.gameObject.SetActive(true);
        torpedo.anchoredPosition = originPos;
        torpedo.localRotation = Quaternion.Euler(0, 0, angle);

        // 실제 발사 -> 선 활성화, 위치 설정, 각도 설정 
        line.gameObject.SetActive(true);
        line.anchoredPosition = originDot.anchoredPosition + dir * 0.5f; // 위치: 중간
        line.localRotation = Quaternion.Euler(0, 0, angle);

        // 현재 선택 위치의 실제 좌표(플레이 상)
        Vector3 currentRealSelectPos = new Vector3(
                    _currentSelectPos.x / _radarRadius * _maxDistance,
                    _currentSelectPos.y / _radarRadius * _maxDistance,
                    _targetHeight);

        // 현재 어뢰 위치
        Vector2 currentTorpedoPos = originPos;

        // 거리 선택과 높이 선택이 모두 되어있을 때만 -> 어뢰 발사 가능
        if (_isDistanceSelected && _isHeightSelected)
        {
            while (true)
            {
                // 어뢰 위치 업데이트
                currentTorpedoPos += dir * _torpedoSpeed * Time.deltaTime;
                torpedo.anchoredPosition = currentTorpedoPos;

                // 실제 발사 -> 선은 현재 어뢰 위치에 맞춰 그어짐, 폭발 로직

                // 선 길이 업데이트
                float currentDistance = Vector2.Distance(originPos, currentTorpedoPos);
                line.sizeDelta = new Vector2(currentDistance, line.sizeDelta.y);

                // 심해 괴물에 닿으면 -> 즉시 폭발, 심해 괴물 맞춤
                if (Vector2.Distance(currentTorpedoPos, monsterDot.anchoredPosition) <= _torpedoSpeed * Time.deltaTime)
                {
                    Debug.Log("심해 괴물 닿아서 폭발");

                    // 폭발
                    explosionRange.gameObject.SetActive(true);
                    explosionRange.anchoredPosition = targetPos;

                    // 심해 괴물 맞춤
                    HitMonster();

                    // 대기 후 폭발 범위 끄기
                    yield return new WaitForSeconds(2f);
                    explosionRange.gameObject.SetActive(false);

                    break;
                }

                // 다른 잠수함에 닿으면 -> 즉시 폭발, 이후 레이더에서 아예 사라짐(통신 불가)
                if (Vector2.Distance(currentTorpedoPos, submarine2Dot.anchoredPosition) <= _torpedoSpeed * Time.deltaTime)
                {
                    Debug.Log("다른 잠수함 닿아서 폭발");

                    // 폭발
                    explosionRange.gameObject.SetActive(true);
                    explosionRange.anchoredPosition = targetPos;

                    // 다른 잠수함 위치 갱신 중 아님으로 설정
                    _isUpdatingSubmarine2Pos = false;

                    // 다른 잠수함이 선택되어 있던 경우 -> 선택 대상 초기화
                    if (_clickedTarget == submarine2Dot)
                        _clickedTarget = null;

                    // 다른 잠수함 페이드 아웃되면서 물러남(이후 아예 사라짐)
                    StartCoroutine(FadeInOut(false, submarine2Dot, _fadeDuration));

                    // 대기 후 폭발 범위 끄기
                    yield return new WaitForSeconds(2f);
                    explosionRange.gameObject.SetActive(false);

                    break;
                }

                // 현재 목표 위치에 도달하면 -> 실제 발사의 경우는 폭발, 애니메이션은 초기화
                if (Vector2.Distance(currentTorpedoPos, targetPos) <= _torpedoSpeed * Time.deltaTime)
                {
                    // 폭발
                    explosionRange.gameObject.SetActive(true);
                    explosionRange.anchoredPosition = targetPos;

                    // 현재 잠수함에 닿으면 -> 게임 오버
                    if (Vector3.Distance(currentRealSelectPos, Vector3.zero) <= _explosionDistance)
                    {
                        Debug.Log("잠수함 폭발");
                        GameManager.instance.GameOver(EEndingType.SubmarineExplode); // 게임 오버
                        yield break;
                    }

                    // 심해 괴물에 닿으면 -> 심해 괴물 맞춤
                    Debug.Log(Vector3.Distance(currentRealSelectPos, _monsterPos));
                    if (Vector3.Distance(currentRealSelectPos, _monsterPos) <= _explosionDistance)
                    {
                        Debug.Log("심해 괴물 폭발");
                        HitMonster(); // 심해 괴물 맞춤
                    }

                    // 다른 잠수함에 닿으면 -> 레이더에서 아예 사라짐(통신 불가)
                    if (Vector3.Distance(currentRealSelectPos, _submarine2Pos) <= _explosionDistance)
                    {
                        Debug.Log("다른 잠수함 폭발");

                        // 다른 잠수함 위치 갱신 중 아님으로 설정
                        _isUpdatingSubmarine2Pos = false;

                        // 다른 잠수함이 선택되어 있던 경우 -> 선택 대상 초기화
                        if (_clickedTarget == submarine2Dot)
                            _clickedTarget = null;

                        // 다른 잠수함 페이드 아웃되면서 물러남(이후 아예 사라짐)
                        StartCoroutine(FadeInOut(false, submarine2Dot, _fadeDuration));
                    }

                    // 대기 후 폭발 범위 끄기
                    yield return new WaitForSeconds(2f);
                    explosionRange.gameObject.SetActive(false);

                    break;
                }

                yield return null;
            }
        }

        // 초기화
        _isFiring = false; // 발사 중 아님으로 설정
        ResetAnimUI(); // 애니메이션 UI 초기화
        selectionIndicator.gameObject.SetActive(false); // 선택 표시 끄기
        torpedo.gameObject.SetActive(false); // 어뢰 끄기
        line.gameObject.SetActive(false); // 선 끄기
    }

    /// <summary>
    /// 심해 괴물 맞춤
    /// </summary>
    private void HitMonster()
    {
        SubmarineInGameManager.instance.DeepSeaMonsterController.OnTorpedoHit(); // 심해 괴물 어뢰 맞았을 때 함수 호출
        SubmarineInGameManager.instance.IsFireSuccess = true;
        StartCoroutine(RunAwayMonster()); // 심해 괴물 도망
    }

    /// <summary>
    /// 심해 괴물 도망 코루틴
    /// </summary>
    private IEnumerator RunAwayMonster()
    {
        // 심해 괴물 위치 갱신 중 아님으로 설정
        _isUpdatingMonsterPos = false;

        // 선택 대상이 심해 괴물일 시 -> 선택 대상 초기화
        if (_clickedTarget == monsterDot)
            _clickedTarget = null;

        // 심해 괴물 타이머 0부터 다시 시작하도록 초기화
        _monsterTimer = 0f;

        // 심해 괴물 좌표 텍스트 초기화
        monsterPosText.text = "";

        // 후퇴
        yield return StartCoroutine(FadeInOut(false, monsterDot, _fadeDuration)); // 심해 괴물 페이드 아웃되면서 물러남(코루틴 완료될 때까지 대기)

        // 대기
        yield return new WaitForSeconds(_monsterWaitTime); // 심해 괴물 다시 나타날 때까지 대기 시간만큼 대기

        // 재등장
        _currentMonsterPeriodIndex++;
        _isUpdatingMonsterPos = true; // 심해 괴물 위치 갱신 중으로 설정
        startAngle += 90f; // 시작 위치 변경을 위해 시작 각도 90 더해주기
        StartCoroutine(FadeInOut(true, monsterDot, _fadeDuration)); // 심해 괴물 페이드 인되면서 나타남
    }
    #endregion

    #region Extra 
    /// <summary>
    /// 페이드 인/아웃
    /// </summary>
    /// <param name="isFadeIn">페이드 인 여부</param>
    /// <param name="target">타겟</param>
    /// <param name="duration">페이드 시간</param>
    IEnumerator FadeInOut(bool isFadeIn, RectTransform target, float duration)
    {
        // 페이드 인 여부에 따른 시작, 끝 수치 설정
        float start;
        float end;
        if (isFadeIn) // 페이드 인 -> (0 -> 1), 타겟 활성화
        {
            start = 0f;
            end = 1f;
            target.gameObject.SetActive(true); // 타겟 활성화
        }
        else // 페이드 아웃 -> (1 -> 0)
        {
            start = 1f;
            end = 0f;
        }

        Image targetImg = target.GetComponent<Image>(); // 타겟 이미지
        Vector2 retreatDir = target.anchoredPosition.normalized; // 후퇴 방향
        float retreatSpeed = 15f;

        // 불투명도 변경
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            // 불투명도 변경
            float alpha = Mathf.Clamp01(Mathf.Lerp(start, end, t));
            Color c = targetImg.color;
            c.a = alpha;
            targetImg.color = c;

            // 크기 변경
            float scale = Mathf.Lerp(0.3f, 1f, alpha);
            target.localScale = Vector3.one * scale;

            // 페이드 아웃일 때만 -> 위치 변경(물러남)
            if (!isFadeIn)
            {
                // 위치 변경(물러남)
                target.anchoredPosition += retreatDir * retreatSpeed * Time.deltaTime;
            }

            yield return null;
        }

        // 페이드 아웃 -> 타겟 비활성화
        if (!isFadeIn)
            target.gameObject.SetActive(false);
    }

    /// <summary>
    /// 위치 텍스트가 레이더 화면 안에서 보이도록 보정
    /// </summary>
    void ClampPosTextsInside(TextMeshProUGUI text, RectTransform dot)
    {
        Rect textRect = text.rectTransform.rect;
        Vector2 dir = Vector2.down;
        float offset = 30f;

        bool hitRight = dot.anchoredPosition.x + textRect.width * 0.5f > _radarRadius;
        bool hitLeft = dot.anchoredPosition.x - textRect.width * 0.5f < -_radarRadius;
        bool hitTop = dot.anchoredPosition.y + textRect.height * 0.5f > _radarRadius;
        bool hitBottom = dot.anchoredPosition.y - textRect.height * 0.5f - offset < -_radarRadius;

        if (hitRight) dir.x = -2f;
        if (hitLeft) dir.x = 2f;
        if (hitTop) dir.y = -1f;
        if (hitBottom) dir.y = 1f;

        text.rectTransform.anchoredPosition = dir * offset;
    }
    #endregion
}
