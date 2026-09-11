using System.Collections;
using System.Collections.Generic;
using DeepSeaMonsterStates;
using UnityEngine;

public class DashAttack : DeepSeaMonsterAttackBase
{
    public override Vector3[] SpawnDirections => new Vector3[] { Vector3.forward, Vector3.back };
    public override float SpawnDistance => 30f;
    public override float MoveSpeed => 20f;
    public override float DamageAmount => 20f;
    public override float MoveTimeout => moveTimeout;
    public override float AttackDistance => 40f;
    public override string AnimationTriggerName => "attack2";

    public Vector3 moveDir;
    public float slowMoveTime = 1f;
    public float slowSpeed = 5f;
    public float moveTimeout;

    public DashAttack(DeepSeaMonsterController monster) : base(monster)
    {
        moveTimeout = slowMoveTime + (AttackDistance - (slowMoveTime * slowSpeed)) / MoveSpeed;
    }

    public override void OnSpawned()
    {
        moveTimer = 0f;

        // 스폰되자마자 괴물을 바라보도록 카메라 회전 (회전하는 동안만 고정이고, 이후 조작 자유)
        monster.CameraController.RotateToTargetPos(monster.transform.position, 1f);

        monster.DeepSeaPlayerMove.SetSpeedMultiplier(0.1f);

        // // 스폰 당시 플레이어 위치로 직선 이동할 수 있도록, 스폰하자마자 이동 방향 확정
        // moveDir = (playerTransform.position - monster.transform.position).normalized;

        // // 상하 이동 불가
        // monster.DeepSeaPlayerMove.CanVerticalMove = false;
    }

    public override bool UpdateMovement()
    {
        // 이동
        moveDir = (playerTransform.position - monster.transform.position).normalized;
        Vector3 targetChargePos;
        if (moveTimer <= slowMoveTime)
            targetChargePos = rb.position + moveDir * MoveSpeed * Time.fixedDeltaTime;
        else
            targetChargePos = rb.position + moveDir * slowSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetChargePos);

        moveTimer += Time.fixedDeltaTime; // 타이머 증가

        // if (moveTimer > slowMoveTime)
        // {
        //     if (!monster.DeepSeaPlayerMove.CanMove)
        //         monster.DeepSeaPlayerMove.CanMove = true;
        // }

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
        Debug.Log("대시 공격 성공!");
        monster.DamagePlayer(DamageAmount); // 대미지

        // // 상하 이동 다시 가능으로 설정
        // monster.DeepSeaPlayerMove.CanVerticalMove = true;

        monster.DeepSeaPlayerMove.SetSpeedMultiplier(1f);

        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }

    public override void OnAttackMiss()
    {
        Debug.Log("대시 공격 실패...");

        // // 상하 이동 다시 가능으로 설정
        // monster.DeepSeaPlayerMove.CanVerticalMove = true;

        monster.DeepSeaPlayerMove.SetSpeedMultiplier(1f);

        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }
}
