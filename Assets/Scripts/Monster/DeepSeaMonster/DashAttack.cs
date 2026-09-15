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
    public override float MoveTimeout => AttackDistance / MoveSpeed;
    public override float AttackDistance => 40f;
    public override string AnimationTriggerName => "attack2";

    public Vector3 moveDir;

    public DashAttack(DeepSeaMonsterController monster) : base(monster)
    {
    }

    public override void OnSpawned()
    {
        moveTimer = 0f;

        // 스폰되자마자 괴물을 바라보도록 카메라 회전 (회전하는 동안만 고정이고, 이후 조작 자유)
        monster.CameraController.RotateToTargetPos(monster.transform.position, 1f);
    }

    public override bool UpdateMovement()
    {
        // 이동
        moveDir = (playerTransform.position - monster.transform.position).normalized;
        Vector3 targetChargePos = rb.position + moveDir * MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetChargePos);

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
        Debug.Log("대시 공격 성공!");
        monster.DamagePlayer(DamageAmount); // 대미지
        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }

    public override void OnAttackMiss()
    {
        Debug.Log("대시 공격 실패...");
        monster.ChangeState(new RetreatState()); // 퇴각 상태로 전환
    }
}
