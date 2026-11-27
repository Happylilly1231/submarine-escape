using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DeepSeaMonsterAttackBase
{
    protected DeepSeaMonsterController monster; // 괴물 FSM 컨트롤러

    public abstract Vector3 SpawnOffset { get; } // 처음 나타나는 위치 오프셋 (플레이어 BasePosition 기준)
    public abstract Vector3 TargetOffset { get; } // 목표 위치 오프셋 (플레이어 BasePosition 기준)
    public abstract float MoveSpeed { get; } // 목표까지 이동 속도
    public abstract bool CanAttackFromBehind { get; } // 뒤에서 공격 가능 여부
    public abstract float DamageAmount { get; } // 대미지 양
    public virtual bool LookAtPlayerWhileMoving => true; // 이동 중에 회전할 지 여부
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="monster">괴물 FSM 컨트롤러</param>
    public DeepSeaMonsterAttackBase(DeepSeaMonsterController monster)
    {
        this.monster = monster;
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
    /// 공격 성공 시
    /// </summary>
    public abstract void OnAttackSuccess();

    /// <summary>
    /// 공격 실패 시
    /// </summary>
    public abstract void OnAttackMiss();

    // 현재는 안 씀
    /// <summary>
    /// 이동 업데이트 (Rigidbody 이동이므로 FixedUpdate에서 실행됨)
    /// </summary>
    /// <returns>이동 완료 여부 반환</returns>
    public abstract bool UpdateMovement();
}
