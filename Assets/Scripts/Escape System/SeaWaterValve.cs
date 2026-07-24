using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public class SeaWaterValve : InteractableBase
{
    [SerializeField] private Door escapeRoomDoor; // 탈출실 문
    [SerializeField] private Transform hatchTransform; // 해치 트랜스폼
    [SerializeField] private Transform hatchViewPoint; // 해치 열리는 거 보는 위치
    [SerializeField] private GameObject hatchLightObj; // 해치 비추는 조명 오브젝트

    [Header("Target References")]
    public Transform waterTransform;
    public AudioLowPassFilter lowPassFilter;
    public Volume underwaterVolume; // 물속 효과가 담긴 볼륨

    [Header("UI")]
    [SerializeField] private GameObject interactorUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject statUI;

    private bool _isActivated = false; // 작동되었는지 여부
    public bool IsActivated => _isActivated;
    private float _targetHeight = 4.4f; // 물이 차오를 최종 높이
    private float _duration = 10f;    // 차오르는 데 걸리는 시간
    private float _underwaterCutoff = 600f; // 물속 소리 주파수

    private bool _isUnderwater = false;

    private ObjectiveManager objectiveManager;
    private EqualizingQTE equalizingQTE;

    [Header("Sound")]
    private AudioSource _audioSource;
    [SerializeField] private AudioClip floodSound; // 물 차는 소리
    [SerializeField] private AudioClip muffledSound; // 물 속에 있어서 웅웅대는 소리

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        objectiveManager = FindObjectOfType<ObjectiveManager>();
        equalizingQTE = GetComponent<EqualizingQTE>();

        hatchLightObj.SetActive(false);
    }

    public override bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public override string GetInteractText()
    {
        if (!escapeRoomDoor.gameObject.activeSelf)
            return LocalizationHelper.GetLocalizedInteractText("Interact/DoorBroken");
        if (!_isActivated)
            return LocalizationHelper.GetLocalizedInteractText("Interact/FloodEscapeRoom", "E"); // 탈출실 물 채우기
        else
            return "";
    }

    public override void Interact()
    {
        if (escapeRoomDoor.gameObject.activeSelf && !_isActivated)
            FloodEscapeRoom(); // 탈출실 물 채우기
    }

    /// <summary>
    /// 탈출실 물 채우기
    /// </summary>
    private void FloodEscapeRoom()
    {
        _isActivated = true;

        if (escapeRoomDoor.isOpened) // 탈출실 문 열려있으면
            escapeRoomDoor.CloseDoor(); // 탈출실 문 자동으로 닫기
        escapeRoomDoor.isLocked = true; // 탈출실 문 잠그기 (이제 열 수 없음)

        interactorUI.SetActive(false);
        inventoryUI.SetActive(false);
        statUI.SetActive(false);

        objectiveManager.CompleteObjective("GoToEscapeRoom");
        Debug.Log("물 채우기를 시작합니다.");
        StartWaterSequence();
    }

    void StartWaterSequence()
    {
        // 물 차는 소리
        _audioSource.clip = floodSound;
        _audioSource.Play(); // 루프 재생

        // 초기 설정
        underwaterVolume.weight = 0;
        lowPassFilter.cutoffFrequency = 22000;

        // 1. 물 상승 시퀀스
        waterTransform.DOLocalMoveY(_targetHeight, _duration)
            .SetEase(Ease.Linear) // 일정한 속도로 상승
            .OnUpdate(() =>
            {
                // 매 프레임 물 수위와 카메라 높이 비교
                CheckCameraLevel();
            })
            .OnComplete(() =>
            {
                _audioSource.clip = muffledSound;

                // 치료제 투여 여부에 따른 엔딩
                PlayerMutation playerMutation = SubmarineInGameManager.instance.player.GetComponent<PlayerMutation>();
                if (playerMutation.IsCured)
                {
                    // 탈출 QTE 성공 후 탈출 연출 재생
                    //Escape();
                    equalizingQTE?.StartQTE();
                }
                else
                {
                    // 완전 괴물화
                    playerMutation.SpawnSpike(true);
                }
            });
    }

    void CheckCameraLevel()
    {
        // 카메라가 수면보다 아래에 있는지 체크
        if (!_isUnderwater && Camera.main.transform.position.y < waterTransform.position.y)
        {
            EnterUnderwater();
        }
        else if (_isUnderwater && Camera.main.transform.position.y > waterTransform.position.y)
        {
            ExitUnderwater();
        }
    }

    void EnterUnderwater()
    {
        _isUnderwater = true;

        // DOTween을 이용한 부드러운 연출 전환
        DOTween.To(() => underwaterVolume.weight, x => underwaterVolume.weight = x, 1f, 0.5f);
        DOTween.To(() => lowPassFilter.cutoffFrequency, x => lowPassFilter.cutoffFrequency = x, _underwaterCutoff, 0.5f);

        // 안개 색상 변경 (기본 렌더링 설정 사용 시)
        RenderSettings.fog = true;
        DOTween.To(() => RenderSettings.fogColor, x => RenderSettings.fogColor = x, new Color(0, 0.2f, 0.4f), 0.5f);
    }

    void ExitUnderwater()
    {
        _isUnderwater = false;

        DOTween.To(() => underwaterVolume.weight, x => underwaterVolume.weight = x, 0f, 0.5f);
        DOTween.To(() => lowPassFilter.cutoffFrequency, x => lowPassFilter.cutoffFrequency = x, 22000f, 0.5f);

        DOTween.To(() => RenderSettings.fogColor, x => RenderSettings.fogColor = x, Color.gray, 0.5f);
    }

    /// <summary>
    /// 탈출
    /// </summary>
    public void Escape()
    {
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 포커스 상태로 변경
        // SubmarineInGameManager.instance.SetFocus(true); // 포커스
        PlayerManager.Instance.SetPlayerGeoActive(false); // 플레이어 모습 안 보이게

        hatchLightObj.SetActive(true); // 해치 비추는 조명 켜기
        SubmarineInGameManager.instance.SetActiveInGameUI(false);

        // 연출
        Sequence seq = DOTween.Sequence();

        // 카메라가 해치 보는 위치로 이동
        seq.Append(Camera.main.transform.DOMove(hatchViewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad));
        seq.Join(Camera.main.transform.DORotateQuaternion(hatchViewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad));

        // 해치 문 열기
        seq.Append(hatchTransform.DOLocalRotate(new Vector3(0f, -90f, 0f), 2f, RotateMode.WorldAxisAdd));

        // 1초 기다리기
        seq.AppendInterval(1f);

        seq.Append(FXManager.instance.fadeImage.DOFade(1f, 2f)); // 화면이 완전히 검게 변함

        // 완료되면 -> 탈출 성공
        seq.OnComplete(() =>
        {
            _audioSource.Stop();
            GameManager.instance.GameClear(); // 탈출 성공
        });
    }

}
