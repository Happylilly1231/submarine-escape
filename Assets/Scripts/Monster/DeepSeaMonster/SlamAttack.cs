using System.Collections;
using System.Collections.Generic;
using DeepSeaMonsterStates;
using UnityEngine;

public class SlamAttack : DeepSeaMonsterAttackBase
{
    public override Vector3 SpawnOffset => CalculateSpawnPos();
    public override Vector3 TargetOffset => new Vector3(0f, 0f, 30f);
    public override float MoveSpeed => 10f;
    public override bool ShouldStop => true;
    public override float DamageAmount => 30f;

    private float _circleRadius = 20f;
    private float _spawnDistance = 40f;
    private float _heightOffset = 10f;

    public SlamAttack(DeepSeaMonsterController monster) : base(monster)
    {
    }

    private Vector3 CalculateSpawnPos()
    {
        // 플레이어 위쪽 멀리 떨어진 원 둘레에서 나오도록 함
        Vector2 randomCircle = Random.insideUnitCircle.normalized; // 정규화를 해서 무조건 원 둘레 상에서만 나오도록 함
        float circleX = randomCircle.x * _circleRadius;
        float circleY = randomCircle.y * _circleRadius + _heightOffset;
        return new Vector3(circleX, circleY, _spawnDistance);
    }

    public override void OnSpawned()
    {

    }

    public override void OnReachedToTarget()
    {
        monster.animator.SetTrigger("attack2");
    }

    public override void OnAnimationAttack()
    {
        monster.audioSource.PlayOneShot(monster.shockwaveSound);

        // 충격파 날리기
        monster.LaunchShockwave(Quaternion.Euler(new Vector3(0f, 0f, 90f)), 1f, OnAttackSuccess, OnAttackMiss);
    }

    public override void OnAttackSuccess()
    {
        Debug.Log("공격 성공 - 대미지");
        monster.DamagePlayer(DamageAmount); // 대미지
        monster.underwaterPlayerMove.ApplyKnockback(); // 넉백 적용
        monster.ChangeState(new RetreatState());
    }

    public override void OnAttackMiss()
    {
        Debug.Log("공격 실패");
        monster.ChangeState(new RetreatState());
    }

    public override bool UpdateMovement()
    {
        return true;
    }
}
