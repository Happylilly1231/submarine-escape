using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform frontTransform; // 문 앞 트랜스폼
    [SerializeField] private Transform backTransform; // 문 뒤 트랜스폼
    [SerializeField] private Transform centerTransform; // 중앙 트랜스폼

    public Vector3 frontPos; // 문 앞 위치
    public Vector3 backPos; // 문 뒤 위치
    public Vector3 centerPos; // 중앙 위치

    public bool isOpened = false; // 열려있는지 변수
    private Transform _doorAxis;
    private float _openDuration = 1f; // 여는 시간
    private bool _isMoving = false;
    public bool isLocked;
    public bool isAdditionalLocked = false; // 추가적으로 잠금됨
    public bool isRepairNeed = false; // 수리 필요 여부

    public static event Action OnDoorOpenStateChanged; // 문이 열리고 닫힐 때 이벤트
    public static event Action<Door> OnDoorClosed; // 문이 닫힐 때 이벤트(문을 인자로 넘겨줌)

    private OcclusionPortal _occlusionPortal;

    private Tween _doorTween; // 현재 실행 중인 트윈을 저장할 변수

    private void Awake()
    {
        _occlusionPortal = GetComponent<OcclusionPortal>();
        _doorAxis = transform.parent; // 문 회전 축
    }

    void Start()
    {
        frontPos = frontTransform.position;
        backPos = backTransform.position;
        centerPos = centerTransform.position;
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    /// <returns></returns>
    public string GetInteractText()
    {
        if (_isMoving) return "";
        if (SubmarineInGameManager.instance.CurrentAlertArea == AlertArea.MachinerySpace) return ""; // 기계실 경보 발생돼서 기계실에 갇힌 경우에는 문 열 수 없음
        if (isRepairNeed) return "Broken (Needs Repair)";
        if (isLocked) return "Locked";
        if (isAdditionalLocked) return "Still Locked";
        return isOpened ? "close [E]" : "open [E]";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 문을 열거나 닫음
    /// </summary>
    public void Interact()
    {
        if (_isMoving || isLocked || isAdditionalLocked || isRepairNeed || SubmarineInGameManager.instance.CurrentAlertArea == AlertArea.MachinerySpace) return;

        if (!isOpened)
        {
            // 플레이어 위치에 따라 방향 결정 후 열기
            float targetAngle = GetTargetAngleBasedOnPlayer();
            OpenDoor(targetAngle);
        }
        else
        {
            // 닫기 (0도로 복귀)
            CloseDoor();
        }
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    private float GetTargetAngleBasedOnPlayer()
    {
        // 플레이어의 현재 위치 가져오기
        //Vector3 playerPos = SubmarineInGameManager.instance.player.transform.position;
        Vector3 playerPos = PlayerManager.Instance.playerMove.transform.position;

        // 플레이어와 frontPoint 사이의 거리 vs backPoint 사이의 거리 비교
        float distToFront = Vector3.Distance(playerPos, frontPos);
        float distToBack = Vector3.Distance(playerPos, backPos);

        // 앞에서 보면 -90도(뒤로 밀기), 뒤에서 보면 90도(앞으로 밀기)
        return (distToFront < distToBack) ? -90f : 90f;
    }

    public virtual void OpenDoor(float angle)
    {
        _isMoving = true;

        // 탈출실 문을 연 경우
        if (gameObject.CompareTag("EscapeRoomDoor"))
        {
            // SubmarineInGameManager.instance.hasEverOpenedEscapeDoor = true; // 탈출실 문 한번이라도 열었음으로 설정(이후에 조종실에서 경보가 울려도 탈출실에 계속 있음)
            SubmarineInGameManager.instance.AlertOn(AlertArea.EscapeRoom); // 경보 발생

        }
        // 기계실 문을 연 경우
        else if (gameObject.CompareTag("MachinerySpaceDoor"))
        {
            // 비상 폐쇄 중인 상태 & 외부에서 열었을 때만 -> 경보 발생
            if (SubmarineInGameManager.instance.machinerySpaceDoorRepairController.IsEmergencyLockdown) // 비상 폐쇄 중인지 검사
            {
                Vector3 playerPos = SubmarineInGameManager.instance.player.transform.position;

                Vector2 playerXZ = new Vector2(playerPos.x, playerPos.z);
                Vector2 frontXZ = new Vector2(frontPos.x, frontPos.z);
                Vector2 backXZ = new Vector2(backPos.x, backPos.z);

                // 평면 거리 계산 (제곱 거리 사용)
                float frontSqrDist = (frontXZ - playerXZ).sqrMagnitude;
                float backSqrDist = (backXZ - playerXZ).sqrMagnitude;

                if (frontSqrDist < backSqrDist) // 외부에서 열었을 때만
                    SubmarineInGameManager.instance.AlertOn(AlertArea.MachinerySpace); // 기계실 문 경보 발생
            }
        }

        _occlusionPortal.open = true;

        _doorTween = _doorAxis.DOLocalRotate(new Vector3(0, angle, 0), _openDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isOpened = true;
                _isMoving = false;
                _doorTween = null;
                OnDoorOpenStateChanged?.Invoke();
            });
    }

    public void CloseDoor()
    {
        // 1. 이미 실행 중인 트윈이 있다면 즉시 종료
        if (_doorTween != null && _doorTween.IsActive())
        {
            _doorTween.Kill();
        }

        _isMoving = true;

        _doorTween = _doorAxis.DOLocalRotate(Vector3.zero, _openDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isOpened = false;
                _isMoving = false;
                _doorTween = null;

                // 탈출실 문을 닫은 경우
                if (gameObject.CompareTag("EscapeRoomDoor"))
                {
                    // 내부 괴물의 현재 위치 가져오기
                    Vector3 innerMonsterPos = SubmarineInGameManager.instance.innerMonsterTransform.position;

                    // 내부 괴물과 frontPoint 사이의 거리 vs backPoint 사이의 거리 비교
                    float distToFront = Vector3.Distance(innerMonsterPos, frontPos);
                    float distToBack = Vector3.Distance(innerMonsterPos, backPos);

                    Debug.Log(GetTargetAngleBasedOnPlayer());

                    // 문을 닫았는데 플레이어가 탈출실 안쪽으로 들어온 경우(문 앞과 가까운 경우 = 탈출실 안쪽에 있는 경우)
                    if (GetTargetAngleBasedOnPlayer() == -90f)
                    {
                        Debug.Log(distToBack + " / " + distToFront);
                        if (distToFront < 5f && distToFront < distToBack) // 괴물이 같이 탈출실 안에 있는 경우 -> 즉사 
                        {
                            GameManager.instance.GameOver(EEndingType.MonsterDeath); // 괴물에게 죽음
                            return;
                        }
                        // else // 괴물 없이 혼자 무사히 탈출실에 들어와 문을 닫은 경우
                        // {
                        //     GameManager.instance.GameClear(); // 탈출 성공
                        //     return;
                        // }
                    }
                }

                OnDoorOpenStateChanged?.Invoke();
                OnDoorClosed?.Invoke(this);
                _occlusionPortal.open = false;
            });
    }

    /// <summary>
    /// 문 열린 여부 설정 (초기화할 때 사용, 열리거나 닫히는 모션 없이 즉시 설정)
    /// </summary>
    /// <param name="isOpen">열린 여부</param>
    public void SetDoorOpenState(bool isOpen)
    {
        if (_occlusionPortal == null)
            return;

        isOpened = isOpen;
        _occlusionPortal.open = isOpen;
        _doorAxis.localRotation = Quaternion.identity;

        OnDoorOpenStateChanged?.Invoke();
        if (!isOpened)
            OnDoorClosed?.Invoke(this);
    }
}
