using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BigSkull : BackroomEntity
{
    [Header("설정")]
    [SerializeField] private Transform[] patrolPoints; // A, B 포인트
    [SerializeField] private AudioSource roarSoundSource;
    [SerializeField] private AudioSource chargingSoundSource; // 점점 커질 소리
    [SerializeField] private Transform eyePosTransform; // 눈 높이
    [SerializeField] private RoomType currentRoomType; // 현재 방 종류

    private Transform _playerTransform;
    private Transform _meshObjTransform; // 실제 메시 오브젝트 (플레이어를 바라보도록 회전할 때만 사용)
    private float normalSpeed = 2f;
    private Vector3 targetPosition;

    private float minWaitTime = 10f;
    private float maxWaitTime = 20f;

    private float chargeTimer = 0f;
    private bool isCharging = false;

    private void Awake()
    {
        _meshObjTransform = transform.GetChild(0); // 메시 오브젝트 가져오기
    }

    private void OnEnable()
    {
        Portal.OnRoomExit += StopSpawning;
        Portal.OnRoomEnter += StartSpawning;
    }

    private void OnDisable()
    {
        Portal.OnRoomExit -= StopSpawning;
        Portal.OnRoomEnter -= StartSpawning;
    }

    void Start()
    {
        _playerTransform = SubmarineInGameManager.instance.player.transform; // 플레이어 트랜스폼 가져오기
        SetShow(false); // 안 보이게
    }

    /// <summary>
    /// 보이는 여부 설정 (gameObject를 비활성화하면 코루틴 자체가 꺼지기 때문)
    /// </summary>
    /// <param name="isShow">보이는 여부</param>
    private void SetShow(bool isShow)
    {
        _meshObjTransform.gameObject.SetActive(isShow); // 메시 오브젝트 활성화 여부 설정
    }

    /// <summary>
    /// 스폰 시작 함수
    /// </summary>
    public void StartSpawning(RoomType roomType)
    {
        if (roomType != currentRoomType)
            return;

        Debug.Log("Start Spawning");
        StartCoroutine(SpawnLoopCoroutine());
    }

    /// <summary>
    /// 스폰 중지(종료) 함수
    /// </summary>
    public void StopSpawning(RoomType roomType)
    {
        if (roomType != currentRoomType)
            return;

        Debug.Log("Stop Spawning");
        // 플레이어가 나갔으므로 현재 활동 중인 해골도 강제로 끔
        StopAllCoroutines();
        SetShow(false); // 안 보이게
        roarSoundSource.Stop();
        chargingSoundSource.Stop();
    }

    /// <summary>
    /// 플레이어가 거대 해골을 바라보도록 하는 함수
    /// </summary>
    /// <returns>연출 시퀀스</returns>
    private Sequence ViewBigSkullSequence()
    {
        SubmarineInGameManager.instance.playerInput.DeactivateInput(); // 액션 비활성화
        Vector3 startViewPos = new Vector3(transform.position.x, _playerTransform.position.y, transform.position.z); // y는 플레이어와 동일하게(몸이 기울어지지 않게)

        Sequence seq = DOTween.Sequence();

        // 1. 플레이어가 해골 쪽 바라보도록 좌우 회전
        seq.Append(_playerTransform.DOLookAt(startViewPos, 1f));

        // 2. 카메라가 해골 눈 제대로 마주치도록 상하 회전
        seq.AppendCallback(() =>
        {
            FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 포커스 상태로 변경
            // SubmarineInGameManager.instance.SetFocus(true); // 포커스
            Vector3 lookEyeDir = (eyePosTransform.position - Camera.main.transform.position).normalized; // 카메라가 해골 눈높이 바라보는 방향 계산
            Quaternion targetRotation = Quaternion.LookRotation(lookEyeDir); // 해당 방향을 바라보기 위한 쿼터니언을 오일러 각으로 변환
            float targetY = targetRotation.eulerAngles.y; // y만 목표 회전값으로 설정
            Camera.main.transform.DORotate(new Vector3(Camera.main.transform.eulerAngles.x, targetY, Camera.main.transform.eulerAngles.z), 0.5f).SetEase(Ease.OutQuad); // DOTween으로 Y축만 회전 (현재 X, Z는 유지)
        });

        // 카메라 회전하는 시간만큼 대기 (위에 회전 명령어는 AppendCallback에서 실행되었으므로 기다려주지 않기 때문)
        seq.AppendInterval(0.5f);

        // 완료 시 -> 포커스 해제
        seq.OnComplete(() =>
        {
            FocusManager.Instance.PopFocusState(); // 이전 포커스 복구
            // SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제 
        });

        return seq; // 연출 시퀀스 반환
    }

    /// <summary>
    /// 스폰 루프 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnLoopCoroutine()
    {
        while (true)
        {
            yield return new WaitUntil(() => !GameManager.instance.IsPausing);

            // 시작 위치, 목표 위치 설정 (먼 곳에서 시작)
            SetupSpawnPosAndTargetPos();

            // 굉음 발생 후 출현
            roarSoundSource.Play();
            SetShow(true); // 보이게
            Debug.Log("출현");

            // 처음에 해골이 플레이어 바라보도록 함 (메시 오브젝트를 회전)
            Vector3 targetPos = new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z);
            _meshObjTransform.LookAt(targetPos);

            // 플레이어가 해골 바라보게 하는 연출 시퀀스를 실행하고 종료될 때까지 대기
            yield return ViewBigSkullSequence().WaitForCompletion();

            bool reached = false;
            // 목표 위치에 도달하지 않은 동안 -> 목표 위치로 이동 / 차징
            while (!reached)
            {
                yield return new WaitUntil(() => !GameManager.instance.IsPausing);

                // 차징 여부 판단 (플레이어가 해골을 바라보고 있는지 여부)
                bool canCharge = CheckCanCharge();

                if (canCharge) // 차징 가능하면
                {
                    if (!isCharging) // 차징 중 아니었다면 -> 차징 시작
                    {
                        isCharging = true; // 차지 중으로 설정
                        chargeTimer = 0f; // 차징 타이머 0으로 초기화
                        chargingSoundSource.volume = 0f;
                        chargingSoundSource.Play(); // 차징 소리 시작
                    }

                    if (chargeTimer >= 2f) // 차징 2초 이상이면 -> 차징 완료 => 돌진
                    {
                        chargingSoundSource.Stop(); // 차징 소리 중단
                        StartCoroutine(DashCoroutine()); // 돌진
                        yield break; // 현재 스폰 루프 코루틴 종료
                    }

                    chargeTimer += Time.deltaTime; // 차징 타이머 증가
                    chargingSoundSource.volume = Mathf.Lerp(0f, 1f, chargeTimer / 2f); // 볼륨 2초에 가까워질수록 점점 커지게
                }
                else // 차징 불가능하면
                {
                    if (isCharging) // 차징 중이었다면 -> 차징 끝
                    {
                        isCharging = false; // 차징 아님 중으로 설정
                        chargingSoundSource.Stop(); // 차징 소리 중단
                    }

                    // 목표 위치로 이동
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, normalSpeed * Time.deltaTime);

                    // 플레이어 바라보도록 회전 (메시 오브젝트를 회전)
                    Vector3 dirToPlayer = (_playerTransform.position - _meshObjTransform.position).normalized;
                    if (dirToPlayer != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
                        _meshObjTransform.rotation = Quaternion.Slerp(_meshObjTransform.rotation, lookRotation, Time.deltaTime * 5f);
                    }
                }

                // 목표 위치와 매우 가까워지면 -> 도달
                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    reached = true; // 도달함으로 설정
                    SetShow(false); // 안 보이게
                    Debug.Log("해골이 목표 지점에 도달하여 사라졌습니다.");
                }

                yield return null;
            }

            // 목표 도달 후 안 보이는 상태로 랜덤 시간 대기 -> 이후 다시 while문 처음으로 돌아가 재등장
            SetShow(false); // 안 보이게
            float waitTime = Random.Range(minWaitTime, maxWaitTime); // 랜덤 대기 시간
            yield return new WaitForSeconds(waitTime); // 해당 시간만큼 대기
        }
    }

    /// <summary>
    /// 시작 위치와 목표 위치 설정
    /// </summary>
    private void SetupSpawnPosAndTargetPos()
    {
        // 1. 플레이어와 더 먼 포인트 찾기
        float distA = Vector3.Distance(_playerTransform.position, patrolPoints[0].position);
        float distB = Vector3.Distance(_playerTransform.position, patrolPoints[1].position);

        int startIdx = distA > distB ? 0 : 1;
        int endIdx = startIdx == 0 ? 1 : 0;

        transform.position = patrolPoints[startIdx].position;
        targetPosition = patrolPoints[endIdx].position;
    }

    /// <summary>
    /// 차징 가능 여부 판단 (플레이어가 해골을 바라보고 있는지 여부)
    /// </summary>
    /// <returns></returns>
    private bool CheckCanCharge()
    {
        // // 1. 해골이 플레이어를 볼 수 있는지(장애물이 없는지) 체크
        // Vector3 eyePos = eyePosTransform.position; // 해골 눈높이
        // Vector3 playerPos = _playerTransform.position + _playerTransform.up * 1f; // 플레이어 위치 (바닥으로 인식되지 않게 바닥에서 좀 띄움)
        // Vector3 dirToPlayer = (playerPos - eyePos).normalized; // 해골이 플레이어를 바라보는 방향
        // float distToPlayer = Vector3.Distance(eyePos, playerPos); // 해골과 플레이어 사이의 거리
        // bool canSeePlayer = false;
        // // 모든 물체와 충돌할 수 있도록 레이를 쏘되, 거리는 플레이어까지만 제한
        // Debug.DrawRay(eyePos, dirToPlayer * (distToPlayer + 0.5f), Color.red);
        // if (Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, distToPlayer + 0.5f))
        // {
        //     // 쏜 레이가 가장 먼저 부딪힌 것이 플레이어인지 검사
        //     if (hit.transform.gameObject.CompareTag("Player"))
        //     {
        //         Debug.Log("플레이어를 보고 있는 중...");
        //         canSeePlayer = true;
        //     }
        // }

        // 2. 플레이어가 해골을 보고 있는지 체크
        Vector3 dirToSkull = (transform.position - _playerTransform.position).normalized; // 플레이어가 해골을 바라보는 방향
        float angle = Vector3.Angle(_playerTransform.forward, dirToSkull); // 플레이어 앞 방향(바라보는 방향) 벡터와 플레이어가 해골을 바라보는 방향의 각도 차이 계산
        bool playerNotLooking = angle > 60f; // 각도가 60도 초과면 플레이어가 해골을 보고 있지 않는 중으로 설정

        // return canSeePlayer && playerNotLooking; // 돌진 가능 여부: 해골이 플레이어를 바라볼 수 있는지 & 플레이어가 해골을 바라보고 있는지 여부
        return playerNotLooking; // 플레이어가 해골을 바라보고 있는지 여부
    }

    /// <summary>
    /// 돌진 코루틴
    /// </summary>
    private IEnumerator DashCoroutine()
    {
        // 모든 액션 비활성화 (포커스를 하면 플레이어 회전 시 카메라가 플레이어를 따라 움직이지 않아서 액션 비활성화를 먼저 해줌)
        SubmarineInGameManager.instance.playerInput.DeactivateInput();
        Vector3 viewPos = new Vector3(transform.position.x, _playerTransform.position.y, transform.position.z);

        // 해골이 플레이어 바라보도록 함 (메시 오브젝트를 회전)
        Vector3 targetPos = new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z);
        _meshObjTransform.LookAt(targetPos);

        // 연출
        yield return ViewBigSkullSequence().WaitForCompletion(); // 플레이어가 해골 바라보도록 함

        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 포커스 상태로 변경
        // SubmarineInGameManager.instance.SetFocus(true); // 포커스
        Vector3 playerFrontPos = _playerTransform.position + _playerTransform.forward * 2.5f + _playerTransform.up * 1f;

        Sequence seq = DOTween.Sequence();
        roarSoundSource.Play();
        seq.Append(transform.DOMove(playerFrontPos, 0.5f)); // 해골이 플레이어 앞을 향해 빠르게 이동
        seq.Append(Camera.main.transform.DOShakePosition(0.2f, 0.3f, 10, 90f)); // 카메라 흔들림
        seq.Append(FXManager.instance.fadeImage.DOFade(1f, 3f)); // 페이드 인
        // 완료 시 -> 플레이어 죽이기
        seq.OnComplete(() =>
        {
            SetShow(false); // 안 보이게
            roarSoundSource.Stop();
            KillPlayer(); // 플레이어 죽이기
        });
    }
}
