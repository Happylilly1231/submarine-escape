using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DeepSeaMonsterAttackBase
{
    protected DeepSeaMonsterController monster;
    protected Transform playerTransform;
    protected Rigidbody rb;
    protected float moveTimer;

    // public abstract Vector3[] SpawnDirections { get; }
    // public abstract float SpawnDistance { get; }
    // public abstract float MoveTimeout { get; }
    // public abstract float AttackDistance { get; }
    // public abstract string AnimationTriggerName { get; }

    public abstract Vector3 SpawnOffset { get; }
    public abstract Vector3 TargetOffset { get; }
    public abstract float MoveSpeed { get; }
    public abstract bool ShouldStop { get; }
    public abstract float DamageAmount { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="monster"></param>
    public DeepSeaMonsterAttackBase(DeepSeaMonsterController monster)
    {
        this.monster = monster;
        rb = monster.Rb;
        playerTransform = monster.PlayerTransform;
    }

    /// <summary>
    /// 나타났을 때 (공격 상태 시작)
    /// </summary>
    public abstract void OnSpawned();

    /// <summary>
    /// 목표 위치 도달 시
    /// </summary>
    public abstract void OnReachedToTarget();

    /// <summary>
    /// 애니메이션의 공격 타이밍
    /// </summary>
    public abstract void OnAnimationAttack();

    /// <summary>
    /// 이동 업데이트 (Rigidbody 이동이므로 FixedUpdate에서 실행됨)
    /// </summary>
    /// <returns>이동 완료 여부 반환</returns>
    public abstract bool UpdateMovement();

    /// <summary>
    /// 공격 성공 시
    /// </summary>
    public abstract void OnAttackSuccess();

    /// <summary>
    /// 공격 실패 시
    /// </summary>
    public abstract void OnAttackMiss();
}
