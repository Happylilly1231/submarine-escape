using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DeepSeaMonsterAttackBase
{
    protected DeepSeaMonsterController monster;
    protected Transform playerTransform;
    protected Rigidbody rb;
    protected float moveTimer;

    public abstract Vector3[] SpawnDirections { get; }
    public abstract float SpawnDistance { get; }
    public abstract float MoveSpeed { get; }
    public abstract float MoveTimeout { get; }
    public abstract float AttackDistance { get; }
    public abstract float DamageAmount { get; }
    public abstract string AnimationTriggerName { get; }

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

    public abstract void OnSpawned();

    /// <summary>
    /// 이동 업데이트 (Rigidbody 이동이므로 FixedUpdate에서 실행됨)
    /// </summary>
    /// <returns>이동 완료 여부 반환</returns>
    public abstract bool UpdateMovement();

    public abstract void StartAttackAnimation();
    public abstract void UpdateAttack();
    public abstract void OnAttackSuccess();
    public abstract void OnAttackMiss();
}
