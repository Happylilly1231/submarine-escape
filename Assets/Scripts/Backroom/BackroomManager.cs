using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BackroomManager : MonoBehaviour
{
    [Header("백룸 구성")]
    [SerializeField] private GameObject totalBackroom; // 백룸 전체
    [SerializeField] private Transform startPos; // 시작 위치
    [SerializeField] private Portal[] leftEntrances; // 왼쪽 입구 포탈들
    [SerializeField] private Portal[] rightEntrances; // 오른쪽 입구 포탈들
    public List<Portal> allRandomRoomPortals; // 모든 랜덤 방 포탈
    private List<RoomType> _map1Composition = new List<RoomType> {
        RoomType.Maze, RoomType.Large, RoomType.Puzzle2, RoomType.Puzzle3, RoomType.Blocked, RoomType.Blocked
    }; // 맵 구성 정의

    // 엔티티
    private BigSkull[] _bigSkulls; // 거대 해골 배열
    private SunkenSkullPoint[] _sunkenSkullPoints; // 가라앉은 해골 배열
    private BackroomEntity[] _selectedEntities; // 선택된 엔티티 배열
    private int _currentEntityId;
    public int CurrentEntityId => _currentEntityId;

    [Header("긴 복도 이벤트")]
    [SerializeField] private Portal longCorridorPortal;
    [SerializeField] private Portal mazeRoomExitPortal;
    [SerializeField] private Portal puzzleRoom1Portal;
    [SerializeField] private GameObject longCorridorDoorWall; // 문 벽
    [SerializeField] private GameObject longCorridorDoorNotExistWall; // 문 없어질 때 대신 생기는 벽
    public bool isLongCorridorTargetPortalChanged { get; set; } = false;

    [Header("퍼즐1")]
    [SerializeField] private Door largeCenterRoomDoor; // 비대 중앙 방 문
    [SerializeField] private Light largeCenterRoomDoorLight; // 비대 중앙 방 문 천장 조명 빛
    [SerializeField] private TriggerDetector[] skullSpiritTriggers; // 해골 영혼 트리거 배열
    private int _approachingSkullCount = 0; // 플레이어가 닿은(흡수한) 해골(영혼) 수

    // 퍼즐2
    private EscapePasswordPuzzleController _escapePasswordPuzzleController;

    [Header("탈출")]
    [SerializeField] private Portal escapePortal; // 탈출 포탈
    [SerializeField] private InnerMonsterSpinalCord innerMonsterSpinalCord; // 괴물 척수(등 부분)
    public InnerMonsterSpinalCord InnerMonsterSpinalCord => innerMonsterSpinalCord;
    public Portal EscapePortal => escapePortal; // 탈출 포탈
    public Vector3 originalPos { get; set; } // 백룸 들어오기 전 위치

    [Header("기타")]
    public Image waterSurfaceImg; // 수면 이미지 - 가라앉은 해골에서 사용

    public bool isPlayingBackroom { get; private set; } = false; // 백룸 플레이 중 여부
    private Door[] _allDoors; // 모든 문 배열

    public static BackroomManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            _escapePasswordPuzzleController = GetComponent<EscapePasswordPuzzleController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        TriggerDetector.OnBackroomTriggerEntered += ExecuteTriggerEvent;
    }

    private void OnDisable()
    {
        TriggerDetector.OnBackroomTriggerEntered -= ExecuteTriggerEvent;
    }

    private void Start()
    {
        _bigSkulls = totalBackroom.GetComponentsInChildren<BigSkull>(true);
        _sunkenSkullPoints = totalBackroom.GetComponentsInChildren<SunkenSkullPoint>(true);

        for (int i = 0; i < _bigSkulls.Length; i++)
        {
            _bigSkulls[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < _sunkenSkullPoints.Length; i++)
        {
            _sunkenSkullPoints[i].gameObject.SetActive(false);
        }

        SetAppearLongCorridorDoor(false);

        waterSurfaceImg.gameObject.SetActive(false);

        _allDoors = totalBackroom.GetComponentsInChildren<Door>(true);
    }

    /// <summary>
    /// 백룸 진입
    /// </summary>
    public void EnterBackroom()
    {
        totalBackroom.SetActive(true); // 백룸 활성화

        SetupBackroom(); // 백룸 구성(백룸 진입할 때마다 랜덤)
        SelectRandomEntity(); // 현재 백룸에 나올 엔티티 랜덤 선택
        _escapePasswordPuzzleController.SetUpPuzzle(); // 비밀번호 퍼즐 구성

        // 모든 문 닫힌 것으로 초기화
        foreach (var door in _allDoors)
        {
            door.SetDoorOpenState(false);
        }

        ResetPuzzle1(); // 퍼즐 1 초기화
        _escapePasswordPuzzleController.ResetPuzzle(); // 비밀번호 퍼즐 초기화

        MoveToStartPos(); // 시작 위치로 이동

        isPlayingBackroom = true;
    }

    /// <summary>
    /// 시작 위치로 이동
    /// </summary>
    public void MoveToStartPos()
    {
        SubmarineInGameManager.instance.player.GetComponent<PlayerMove>().PlayerTeleport(startPos.position, startPos.rotation); // 시작 위치로 이동
    }

    /// <summary>
    /// 현재 백룸에 나올 엔티티 랜덤 선택
    /// </summary>
    private void SelectRandomEntity()
    {
        _currentEntityId = Random.Range(0, 2);
        switch (_currentEntityId)
        {
            case 0:
                _selectedEntities = _bigSkulls;
                break;
            case 1:
                _selectedEntities = _sunkenSkullPoints;
                break;
        }

        for (int i = 0; i < _selectedEntities.Length; i++)
        {
            _selectedEntities[i].gameObject.SetActive(true);

            if (_currentEntityId == 1)
            {
                _selectedEntities[i].GetComponent<Collider>().enabled = true;
            }
        }
    }

    /// <summary>
    /// 백룸 구성
    /// </summary>
    public void SetupBackroom()
    {
        // 1. 현재 맵 구성 & 입구들 결정
        List<RoomType> currentComp = _map1Composition;

        // 2. 현재 맵 구성 셔플
        ShuffleList(currentComp);

        // 3. 좌/우 방 풀(Pool) 생성
        List<Portal> leftRoomPool = new List<Portal>();
        List<Portal> rightRoomPool = new List<Portal>();

        // 4. 왼쪽 풀, 오른쪽 풀에 방 종류 할당 로직
        // allRoomPortals에서 해당 타입의 방을 찾아 리스트에 담음
        foreach (RoomType type in currentComp)
        {
            // 아직 뽑히지 않은 방 중 해당 타입 찾기
            Portal room = allRandomRoomPortals.Find(p =>
                p.roomType == type &&
                !leftRoomPool.Contains(p) &&
                !rightRoomPool.Contains(p));

            if (room != null)
            {
                // 좌/우 배분 규칙
                if (type == RoomType.CornerLeft || type == RoomType.CornerLeftWithDoor)
                {
                    leftRoomPool.Add(room);
                }
                else if (type == RoomType.CornerRight || type == RoomType.CornerRightWithDoor)
                {
                    rightRoomPool.Add(room);
                }
                else
                {
                    // 공통 방(Blocked 등)은 왼쪽부터 우선 배분
                    if (leftRoomPool.Count < leftEntrances.Length) leftRoomPool.Add(room);
                    else rightRoomPool.Add(room);
                }
            }
        }

        // 5. 왼쪽 방, 오른쪽 방을 각 입구와 연결 (Entrance <-> Room)
        ConnectPortals(leftEntrances, leftRoomPool);
        ConnectPortals(rightEntrances, rightRoomPool);

        Debug.Log($"Backrooom Map Setup Complete!");
    }

    /// <summary>
    /// 포탈 쌍방향 연결 함수
    /// </summary>
    /// <param name="entrances"></param>
    /// <param name="rooms"></param>
    private void ConnectPortals(Portal[] entrances, List<Portal> rooms)
    {
        int count = Mathf.Min(entrances.Length, rooms.Count);
        for (int i = 0; i < count; i++)
        {
            entrances[i].SetDestination(rooms[i]);
            rooms[i].SetDestination(entrances[i]);
        }
    }

    /// <summary>
    /// 리스트 셔플 알고리즘 (Fisher-Yates)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 백룸 탈출
    /// </summary>
    public void EscapeBackroom(PlayerMove playerMove)
    {
        isPlayingBackroom = false;

        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 포커스 상태로 변경
        // SubmarineInGameManager.instance.SetFocus(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(FXManager.instance.fadeImage.DOFade(1f, 1.5f)); // 화면이 완전히 검게 변함

        // 잠시 정적 (완전 암전 상태)
        seq.AppendInterval(0.5f);

        // 4. 백룸 탈출 및 화면 밝아지기 (Fade Out)
        seq.AppendCallback(() =>
        {
            for (int i = 0; i < _selectedEntities.Length; i++)
            {
                _selectedEntities[i].gameObject.SetActive(false);
            }
            totalBackroom.SetActive(false); // 전체 백룸 비활성화

            // 백룸 들어오기 전 위치로 이동 & 괴물 등을 바라보도록 회전
            Quaternion rotation = Quaternion.identity;
            Vector3 direction = innerMonsterSpinalCord.transform.parent.position - originalPos;
            if (direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(direction);
            }
            playerMove.PlayerTeleport(originalPos, rotation);
            SubmarineInGameManager.instance.SetCameraControllerEnable(true); // 플레이어 카메라 컨트롤러 활성화
        });

        // 잠시 정적 (완전 암전 상태)
        seq.AppendInterval(1f);

        seq.Append(FXManager.instance.fadeImage.DOFade(0f, 1f)); // 다시 밝아짐

        seq.OnComplete(() =>
        {
            innerMonsterSpinalCord.CompleteExtractingBioData(); // 생체 데이터 추출 완료
        });
    }

    /// <summary>
    /// 트리거 이벤트 실행
    /// </summary>
    private void ExecuteTriggerEvent(TriggerType triggerType, GameObject triggerObj)
    {
        switch (triggerType)
        {
            case TriggerType.LongCorridorEvent:
                if (!isLongCorridorTargetPortalChanged)
                {
                    // 문 생기게 하기
                    SetAppearLongCorridorDoor(true);

                    if (longCorridorPortal.targetPortal == mazeRoomExitPortal)
                    {
                        triggerObj.transform.GetChild(0).gameObject.SetActive(false); // 연기 끄기
                        longCorridorPortal.targetPortal = puzzleRoom1Portal;
                    }
                    else
                    {
                        triggerObj.transform.GetChild(0).gameObject.SetActive(true); // 연기 켜기
                        longCorridorPortal.targetPortal = mazeRoomExitPortal;
                    }

                    isLongCorridorTargetPortalChanged = true;
                }
                break;
            case TriggerType.Puzzle1_ApproachingSkull:
                if (_approachingSkullCount < 4)
                {
                    _approachingSkullCount++;
                    triggerObj.transform.GetChild(1).gameObject.SetActive(false); // 빛 끄기
                    triggerObj.transform.GetChild(2).gameObject.SetActive(false); // 파티클 끄기
                    triggerObj.GetComponent<TriggerDetector>().enabled = false; // 트리거 끄기 (끝났으므로)

                    if (_approachingSkullCount == 4)
                    {
                        largeCenterRoomDoor.isAdditionalLocked = false;
                        largeCenterRoomDoorLight.color = new Color(125f / 255f, 1f, 1f);
                    }
                }
                break;
        }
    }

    private void ResetPuzzle1()
    {
        _approachingSkullCount = 0; // 모은 개수 초기화
        foreach (var skullSpiritTrigger in skullSpiritTriggers)
        {
            skullSpiritTrigger.transform.GetChild(1).gameObject.SetActive(true); // 빛 켜기
            skullSpiritTrigger.transform.GetChild(2).gameObject.SetActive(true); // 파티클 켜기
            skullSpiritTrigger.enabled = true; // 트리거 켜기
        }
        largeCenterRoomDoor.isAdditionalLocked = true; // 다시 잠금으로 설정
        largeCenterRoomDoorLight.color = Color.red; // 조명 다시 빨간색으로 변경
    }

    public void SetAppearLongCorridorDoor(bool isAppear)
    {
        if (isAppear)
        {
            longCorridorDoorNotExistWall.SetActive(false);
            longCorridorDoorWall.SetActive(true);
        }
        else
        {
            longCorridorDoorWall.SetActive(false);
            longCorridorDoorNotExistWall.SetActive(true);
        }
    }
}