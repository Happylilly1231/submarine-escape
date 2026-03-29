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
    //private float _openAngle = -90f; // 목표 회전각
    private float _openDuration = 1f; // 여는 시간
    private bool _isMoving = false;
    public bool isLocked;

    public static event Action OnDoorOpenStateChanged; // 문이 열리고 닫힐 때 이벤트
    public static event Action<Door> OnDoorClosed; // 문이 닫힐 때 이벤트(문을 인자로 넘겨줌)

    private OcclusionPortal _occlusionPortal;

    private void Awake()
    {
        _occlusionPortal = GetComponent<OcclusionPortal>();
    }

    void Start()
    {
        frontPos = frontTransform.position;
        backPos = backTransform.position;
        centerPos = centerTransform.position;
        _doorAxis = transform.parent; // 문 회전 축
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    /// <returns></returns>
    public string GetInteractText()
    {
        if (_isMoving || isLocked) return "";
        return isOpened ? "close [E]" : "open [E]";
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 문을 열거나 닫음
    /// </summary>
    public void Interact()
    {
        if (_isMoving || isLocked) return;

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
        Vector3 playerPos = SubmarineInGameManager.instance.player.transform.position;

        // 플레이어와 frontPoint 사이의 거리 vs backPoint 사이의 거리 비교
        float distToFront = Vector3.Distance(playerPos, frontPos);
        float distToBack = Vector3.Distance(playerPos, backPos);

        // 앞에서 보면 -90도(뒤로 밀기), 뒤에서 보면 90도(앞으로 밀기)
        return (distToFront < distToBack) ? -90f : 90f;
    }

    private void OpenDoor(float angle)
    {
        _isMoving = true;

        // 탈출실 문을 연 경우
        if (gameObject.CompareTag("EscapeRoomDoor"))
        {
            SubmarineInGameManager.instance.hasEverOpenedEscapeDoor = true; // 탈출실 문 한번이라도 열었음으로 설정(이후에 조종실에서 경보가 울려도 탈출실에 계속 있음)
            SubmarineInGameManager.instance.AlertOn(); // 경보 발생
        }

        _occlusionPortal.open = true;

        _doorAxis.DOLocalRotate(new Vector3(0, angle, 0), _openDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isOpened = true;
                _isMoving = false;
                OnDoorOpenStateChanged?.Invoke();
            });
    }

    private void CloseDoor()
    {
        _isMoving = true;

        _doorAxis.DOLocalRotate(Vector3.zero, _openDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isOpened = false;
                _isMoving = false;

                // 탈출실 문을 닫은 경우
                if (gameObject.CompareTag("EscapeRoomDoor"))
                {
                    // 발사해서 심해 괴물 맞추기 한 번이라도 성공한 경우
                    if (SubmarineInGameManager.instance.IsFireSuccess)
                    {
                        // 내부 괴물의 현재 위치 가져오기
                        Vector3 innerMonsterPos = SubmarineInGameManager.instance.innerMonsterTransform.position;

                        // 내부 괴물과 frontPoint 사이의 거리 vs backPoint 사이의 거리 비교
                        float distToFront = Vector3.Distance(innerMonsterPos, frontPos);
                        float distToBack = Vector3.Distance(innerMonsterPos, backPos);

                        // 문을 닫았는데 플레이어가 탈출실 안쪽으로 들어온 경우(문 뒤와 가까운 경우 = 탈출실 안쪽에 있는 경우)
                        if (GetTargetAngleBasedOnPlayer() == 90f)
                        {
                            if (distToBack < 5f && distToBack < distToFront) // 괴물이 같이 탈출실 안에 있는 경우 -> 즉사 
                            {
                                GameManager.instance.GameOver(EEndingType.MonsterDeath); // 괴물에게 죽음
                                return;
                            }
                            else // 괴물 없이 혼자 무사히 탈출실에 들어와 문을 닫은 경우
                            {
                                GameManager.instance.GameClear(); // 탈출 성공
                                return;
                            }
                        }
                    }
                }

                OnDoorOpenStateChanged?.Invoke();
                OnDoorClosed?.Invoke(this);
                _occlusionPortal.open = false;
            });
    }

    // public void ToggleDoor()
    // {
    //     StopAllCoroutines();
    //     StartCoroutine(ToggleDoorCoroutine());
    // }

    // IEnumerator ToggleDoorCoroutine()
    // {
    //     float time = 0f;
    //     Quaternion startRot = _doorAxis.localRotation; // 시작 각도(현재 각도)
    //     float targetRotY = isOpened ? 0f : _openAngle; // 열려있는지 여부에 따라 닫거나 열기
    //     Quaternion endRot = Quaternion.Euler(0, targetRotY, 0); // 끝 각도(목표 각도)

    //     // 부드럽게 회전
    //     while (time < _openDuration)
    //     {
    //         time += Time.deltaTime;
    //         float t = Mathf.SmoothStep(0, 1, time / _openDuration);
    //         _doorAxis.localRotation = Quaternion.Slerp(startRot, endRot, t); // 부드럽게 회전
    //         yield return null;
    //     }
    //     _doorAxis.localRotation = endRot;

    //     isOpened = !isOpened;
    //     OnDoorOpenStateChanged?.Invoke();

    //     if (!isOpened)
    //         OnDoorClosed?.Invoke(this);
    // }
}
