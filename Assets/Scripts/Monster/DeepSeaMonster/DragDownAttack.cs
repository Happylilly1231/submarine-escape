using System.Collections;
using System.Collections.Generic;
using DeepSeaMonsterStates;
using UnityEngine;

public class DragDownAttack : DeepSeaMonsterAttackBase
{
    public override Vector3[] SpawnDirections => new Vector3[] { Vector3.down };
    public override float SpawnDistance => 20f;
    public override float MoveSpeed => 10f;
    public override float DamageAmount => 20f;
    public override float MoveTimeout => AttackDistance / MoveSpeed;
    public override float AttackDistance => 30f;
    public override string AnimationName => "attack5";

    public Vector3 moveDir;


    public DragDownAttack(DeepSeaMonsterController monster) : base(monster)
    {
    }

    public override void OnSpawned()
    {
        moveTimer = 0f;

        monster.DeepSeaPlayerMove.SetSpeedMultiplier(0.3f);
    }

    public override bool UpdateMovement()
    {
        if (Vector3.Distance(monster.transform.position, playerTransform.position) < 5f)
        {
            Debug.Log("끌어내리기 공격 범위 내");
            return true;
        }

        // 추적 이동
        moveDir = (playerTransform.position - monster.transform.position).normalized;
        Vector3 targetChargePos = rb.position + moveDir * MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetChargePos);

        // 3. 회전 보정 (진행 방향을 바라보도록)
        if (moveDir != Vector3.zero)
        {
            // 이동 방향을 향하는 목표 회전값 계산
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);

            // 즉시 돌리고 싶다면:
            // rb.MoveRotation(targetRotation);

            float rotateSpeed = 10f; // 회전 속도 변수
            Quaternion nextRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotateSpeed);
            rb.MoveRotation(nextRotation);
        }

        moveTimer += Time.fixedDeltaTime; // 타이머 증가

        return moveTimer >= MoveTimeout; // 이동 완료 여부 반환
    }

    public override void StartAttackAnimation()
    {

    }

    public override void UpdateAttack()
    {

    }

    public override void OnAttackSuccess()
    {
        Debug.Log("끌어내리기 공격 성공!");
        monster.DeepSeaPlayerMove.SetSpeedMultiplier(1f);
        monster.DamagePlayer(DamageAmount); // 대미지
        monster.ChangeState(new DragDownState()); // 끌어내리기 상태로 전환
    }

    public override void OnAttackMiss()
    {
        Debug.Log("끌어내리기 공격 실패...");
        monster.DeepSeaPlayerMove.SetSpeedMultiplier(1f);
        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }
}
