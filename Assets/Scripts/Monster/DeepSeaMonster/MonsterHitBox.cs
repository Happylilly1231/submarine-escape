using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterHitBox : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer; // Player 레이어
    [SerializeField] private float radius = 3f;   // 공격 판정 반지름

    private Vector3 lastPos;
    private bool isActive = false;
    private bool hasHit = false; // 타격 성공 여부 플래그

    private System.Action onHitSuccess; // 피격 성공 시 콜백
    private System.Action onHitMiss;

    // 공격 시작 시 성공/실패 콜백을 함께 전달받음
    public void EnableHitbox(System.Action onSuccess = null, System.Action onMiss = null)
    {
        onHitSuccess = onSuccess;
        onHitMiss = onMiss;

        hasHit = false; // 플래그 초기화
        lastPos = transform.position;
        isActive = true;

        // 켜지는 첫 프레임에 이미 플레이어와 겹쳐 있는지 제자리 검사
        Collider[] initialHits = Physics.OverlapSphere(transform.position, radius, targetLayer);
        foreach (var col in initialHits)
        {
            if (col.transform.IsChildOf(transform.root)) continue;

            ProcessHit();
            return;
        }
    }

    // 공격 종료 시점에 맞춘 적이 없다면 실패 콜백 실행
    public void DisableHitbox()
    {
        if (!isActive) return;

        // active 상태 동안 한 번도 맞추지 못했다면 실패 처리
        if (!hasHit)
        {
            onHitMiss?.Invoke();
        }

        isActive = false;
        onHitSuccess = null;
        onHitMiss = null;
    }

    private void FixedUpdate()
    {
        if (!isActive || hasHit) return;

        Vector3 currentPos = transform.position;
        Vector3 dir = currentPos - lastPos;
        float dist = dir.magnitude;

        if (dist > 0f)
        {
            // 경로 전체 훑기 (SphereCast)
            if (Physics.SphereCast(lastPos, radius, dir.normalized, out RaycastHit hit, dist, targetLayer))
            {
                if (hit.collider.transform.IsChildOf(transform.root)) return;

                ProcessHit();
            }
        }

        lastPos = currentPos;
    }

    private void ProcessHit()
    {
        hasHit = true;
        Debug.Log("<color=red>머리 공격 성공!</color>");

        onHitSuccess?.Invoke();
        DisableHitbox();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
