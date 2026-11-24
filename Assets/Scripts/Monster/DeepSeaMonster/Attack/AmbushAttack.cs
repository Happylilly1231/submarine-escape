using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class AmbushAttack : DeepSeaMonsterAttackBase
{
    public override Vector3 SpawnOffset => new Vector3(0f, 0f, 10f);
    public override Vector3 TargetOffset => new Vector3(0f, 0f, 9.5f);
    public override float MoveSpeed => 15f;
    public override bool CanAttackFromBehind => false;
    public override float DamageAmount => 100f;

    public AmbushAttack(DeepSeaMonsterController monster) : base(monster)
    {
    }

    public override void OnSpawned()
    {
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence);
        Camera.main.transform.DORotateQuaternion(monster.PlayerTransform.rotation, 0.2f).SetEase(Ease.OutExpo); // 정면 보기
    }

    public override void OnReachedToTarget()
    {
        monster.animator.SetTrigger("attack5");
        monster.audioSource.PlayOneShot(monster.bigAttackSound);
    }

    public override void OnAnimationAttack()
    {
        monster.audioSource.volume = 0f;
        GameManager.instance.GameOver(EEndingType.MonsterDeath);
    }

    public override void OnAttackSuccess()
    {

    }

    public override void OnAttackMiss()
    {

    }

    public override bool UpdateMovement()
    {
        return true;
    }
}
