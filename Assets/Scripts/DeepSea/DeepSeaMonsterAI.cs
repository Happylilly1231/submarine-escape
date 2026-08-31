using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MonsterState
{
    Spawning,
    Charging,
    Stopped,   // 벽/지형 감지 시 제자리 정지 상태
    Escaping,
    Grabbing
}

[RequireComponent(typeof(Rigidbody))]
public class DeepSeaMonsterAI : MonoBehaviour
{
    [Header("=== Target & Camera ===")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform mainCameraTransform;
    [Header("=== Target Settings ===")]
    [SerializeField] private float playerHeightOffset = 1.5f; // 캐릭터 높이의 절반 정도
    [Header("=== Spawn Safety Settings ===")]
    [SerializeField] private float monsterRadius = 1.5f; // 괴물의 대략적인 몸집 반지름 (바위 내부 스폰 방지)

    [Header("=== Spawn & Charge Settings ===")]
    [SerializeField] private float spawnDistance = 25f;      // 플레이어 기준 스폰 거리
    [SerializeField] private float chargeSpeed = 20f;         // 돌진 속도
    [SerializeField] private float chargeDuration = 3.5f;     // 돌진 유지 시간
    [SerializeField] private float escapeSpeed = 15f;         // 이탈/퇴각 속도
    [SerializeField] private float reAttackDelay = 4f;        // 재공격 대기 시간

    [Header("=== Obstacle Detection (Raycast) ===")]
    [SerializeField] private float raycastDistance = 2.5f;    // 전방 장애물 감지 거리
    [SerializeField] private LayerMask obstacleLayer;         // 지형 및 벽 레이어

    [Header("=== QTE (Space 연타) Settings ===")]
    [SerializeField] private int requiredSpacePresses = 10;
    [SerializeField] private float qteTimeLimit = 3.0f;
    [SerializeField] private float cameraShakeIntensity = 0.3f;

    private MonsterState currentState = MonsterState.Spawning;
    private Vector3 chargeDirection;
    private Vector3 targetWorldPos; // 스폰 순간 고정된 목표 위치

    private int currentSpacePresses = 0;
    private float qteTimer = 0f;
    private Vector3 originalCamLocalPos;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        EnsureTargetReferences();

        if (mainCameraTransform != null)
            originalCamLocalPos = mainCameraTransform.localPosition;

        StartCoroutine(MonsterLoopRoutine());
    }

    private void EnsureTargetReferences()
    {
        if (playerTransform == null && Camera.main != null)
            playerTransform = Camera.main.transform.parent != null ? Camera.main.transform.parent : Camera.main.transform;

        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (currentState == MonsterState.Grabbing)
        {
            HandleGrabbingAndQTE();
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case MonsterState.Charging:
                // 1. 전방 지형 감지 시 즉시 멈춤 (관통 방지)
                if (Physics.Raycast(rb.position, chargeDirection, raycastDistance, obstacleLayer))
                {
                    Debug.Log("벽 감지! 그 자리에서 정지합니다.");
                    currentState = MonsterState.Stopped;
                    break;
                }

                Vector3 targetChargePos = rb.position + chargeDirection * chargeSpeed * Time.fixedDeltaTime;
                rb.MovePosition(targetChargePos);
                break;

            case MonsterState.Stopped:
                // 이동하지 않고 그 자리에 고정 (MovePosition 호출 안함)
                break;

            case MonsterState.Escaping:
                Vector3 targetEscapePos = rb.position + transform.forward * escapeSpeed * Time.fixedDeltaTime;
                rb.MovePosition(targetEscapePos);
                break;

            case MonsterState.Grabbing:
                if (mainCameraTransform != null)
                {
                    Vector3 grabPos = mainCameraTransform.position + mainCameraTransform.forward * 1.5f;
                    Quaternion grabRot = Quaternion.LookRotation(-mainCameraTransform.forward);

                    rb.MovePosition(grabPos);
                    rb.MoveRotation(grabRot);
                }
                break;
        }
    }

    private void HandleGrabbingAndQTE()
    {
        if (mainCameraTransform != null)
        {
            Vector3 randomShake = Random.insideUnitSphere * cameraShakeIntensity;
            mainCameraTransform.localPosition = originalCamLocalPos + randomShake;
        }

        qteTimer -= Time.deltaTime;

        bool isSpacePressedThisFrame = false;
        if (Keyboard.current != null)
        {
            isSpacePressedThisFrame = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (isSpacePressedThisFrame)
        {
            currentSpacePresses++;

            if (currentSpacePresses >= requiredSpacePresses)
            {
                ReleasePlayerSuccess();
                return;
            }
        }

        if (qteTimer <= 0f)
        {
            OnQTEFailed();
        }
    }

    private IEnumerator MonsterLoopRoutine()
    {
        while (true)
        {
            EnsureTargetReferences();

            // 1. 레이캐스트 검사 기반 스폰 위치 선택
            bool spawnSuccess = TrySpawnFromClearDirection();

            if (!spawnSuccess)
            {
                // 6방향 모두 막힌 경우 스폰 실패 -> 투명 상태 유지 및 일정시간 후 재시도
                SetMonsterVisible(false);
                Debug.LogWarning("모든 스폰 방향에 장애물이 감지되었습니다. 잠시 후 재시도합니다.");
                yield return new WaitForSeconds(reAttackDelay);
                continue;
            }

            currentState = MonsterState.Spawning;
            yield return new WaitForSeconds(0.5f);

            // 2. 돌진 시작
            currentState = MonsterState.Charging;
            chargeDirection = (targetWorldPos - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(chargeDirection);
            rb.MoveRotation(targetRotation);

            float timer = 0f;
            while (timer < chargeDuration && (currentState == MonsterState.Charging || currentState == MonsterState.Stopped))
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // 3. 돌진 성공 후 통과 / 벽에 막혀 정지 상태인 경우 -> 이탈 및 대기
            if (currentState == MonsterState.Charging || currentState == MonsterState.Stopped || currentState == MonsterState.Escaping)
            {
                // 장애물에 걸려 멈췄던 경우라면 관통해서 나가기보다 바로 그 자리에서 사라지게 처리
                if (currentState == MonsterState.Charging)
                {
                    currentState = MonsterState.Escaping;
                    yield return new WaitForSeconds(2f);
                }

                SetMonsterVisible(false);
                yield return new WaitForSeconds(reAttackDelay);
            }
            // 4. 물려서 QTE 진행 중인 경우
            else if (currentState == MonsterState.Grabbing)
            {
                while (currentState == MonsterState.Grabbing)
                {
                    yield return null;
                }

                SetMonsterVisible(true);
                currentState = MonsterState.Escaping;
                yield return new WaitForSeconds(3f);
                SetMonsterVisible(false);
                yield return new WaitForSeconds(reAttackDelay);
            }
        }
    }

    private bool TrySpawnFromClearDirection()
    {
        if (playerTransform == null) return false;

        targetWorldPos = playerTransform.position + Vector3.up * playerHeightOffset;

        // 월드 절대 6방향
        List<Vector3> directions = new List<Vector3>
    {
        Vector3.forward,
        Vector3.back,
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down
    };

        // 무작위 셔플
        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 temp = directions[i];
            int randomIndex = Random.Range(i, directions.Count);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        foreach (Vector3 dir in directions)
        {
            Vector3 candidateSpawnPos = targetWorldPos + (dir * spawnDistance);

            // [핵심 1] 스폰 예정 지점 '자체'가 이미 바위/벽 내부인지 구체(Sphere)로 검사
            if (Physics.CheckSphere(candidateSpawnPos, monsterRadius, obstacleLayer))
            {
                // 이 위치는 바위 속이므로 패스!
                continue;
            }

            Vector3 rayDirection = (targetWorldPos - candidateSpawnPos).normalized;

            // [핵심 2] 얇은 선 대신 괴물 몸집 크기(SphereCast)로 돌진 경로 상 장애물 검사
            if (!Physics.SphereCast(candidateSpawnPos, monsterRadius, rayDirection, out RaycastHit hit, spawnDistance, obstacleLayer))
            {
                // 스폰 위치도 안전하고, 플레이어까지 돌진하는 길목에 괴물 몸통이 걸릴 장애물도 없음!
                rb.position = candidateSpawnPos;
                rb.rotation = Quaternion.LookRotation(rayDirection);
                transform.position = candidateSpawnPos;
                transform.rotation = Quaternion.LookRotation(rayDirection);

                SetMonsterVisible(true);
                Debug.Log($"안전한 스폰 위치 발견: {dir} 방향");
                return true;
            }
        }

        // 6방향 모두 막힘
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((currentState == MonsterState.Charging || currentState == MonsterState.Stopped) &&
            (other.CompareTag("Player") || other.transform == playerTransform))
        {
            currentState = MonsterState.Grabbing;
            currentSpacePresses = 0;
            qteTimer = qteTimeLimit;
            Debug.Log("괴물에게 물렸습니다! Space 연타 시작!");
        }
    }

    private void ReleasePlayerSuccess()
    {
        ResetCameraPosition();

        Quaternion escapeRot = Quaternion.LookRotation(transform.position - playerTransform.position);
        rb.MoveRotation(escapeRot);

        currentState = MonsterState.Escaping;
    }

    private void OnQTEFailed()
    {
        currentSpacePresses = 0;
        qteTimer = qteTimeLimit;
    }

    private void ResetCameraPosition()
    {
        if (mainCameraTransform != null)
        {
            mainCameraTransform.localPosition = originalCamLocalPos;
        }
    }

    private void SetMonsterVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position;
        Vector3 dir = chargeDirection != Vector3.zero ? chargeDirection : transform.forward;
        Gizmos.DrawRay(origin, dir * raycastDistance);
    }
}