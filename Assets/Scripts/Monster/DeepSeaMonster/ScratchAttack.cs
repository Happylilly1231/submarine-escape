using System.Collections;
using System.Collections.Generic;
using DeepSeaMonsterStates;
using UnityEngine;

public class ScratchAttack : DeepSeaMonsterAttackBase
{
    public override Vector3[] SpawnDirections => new Vector3[] { Vector3.up };
    public override float SpawnDistance => 20f;
    public override float MoveSpeed => 20f;
    public override float DamageAmount => 20f;
    public override float MoveTimeout => AttackDistance / MoveSpeed;
    public override float AttackDistance => 40f;
    public override string AnimationTriggerName => "attack1";

    public Vector3 moveDir;
    public float dashTimer;


    public ScratchAttack(DeepSeaMonsterController monster) : base(monster)
    {
    }

    public override void OnSpawned()
    {
        moveTimer = 0f;

        // 스폰되자마자 괴물을 바라보도록 카메라 회전 (회전하는 동안만 고정이고, 이후 조작 자유)
        monster.CameraController.RotateToTargetPos(monster.transform.position, 1f);

        monster.DeepSeaPlayerMove.SetSpeedMultiplier(0.1f);

        // // 스폰 당시 플레이어 위치로 직선 이동할 수 있도록, 스폰하자마자 이동 방향 확정
        // moveDir = (playerTransform.position - monster.transform.position).normalized;
    }

    public override bool UpdateMovement()
    {
        if (Vector3.Distance(monster.transform.position, playerTransform.position) < 5f)
        {
            Debug.Log("할퀴기 공격 범위 내");
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
        Debug.Log("할퀴기 공격 성공!");
        monster.DeepSeaPlayerMove.SetSpeedMultiplier(1f);
        monster.DamagePlayer(DamageAmount); // 대미지
        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }

    public override void OnAttackMiss()
    {
        Debug.Log("할퀴기 공격 실패...");
        monster.DeepSeaPlayerMove.SetSpeedMultiplier(1f);
        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }
}
