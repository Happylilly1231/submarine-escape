using System.Collections;
using System.Collections.Generic;
using DeepSeaMonsterStates;
using DG.Tweening;
using UnityEngine;

public class SwallowAttack : DeepSeaMonsterAttackBase
{
    public override Vector3 SpawnOffset => new Vector3(0f, 0f, 10f);
    public override Vector3 TargetOffset => new Vector3(0f, 0f, 9.5f);
    public override float MoveSpeed => 15f;
    public override bool ShouldStop => true;
    public override float DamageAmount => 100f;

    public SwallowAttack(DeepSeaMonsterController monster) : base(monster)
    {
    }

    public override void OnSpawned()
    {
        FocusManager.Instance.PushFocusState(GameFocusState.DeepSeaSwallow);
        Camera.main.transform.DORotateQuaternion(monster.PlayerTransform.rotation, 0.2f).SetEase(Ease.OutExpo); // 정면 보기

        // 모두 약화 시 -> 입에 약점 2개 랜덤으로 생성
        if (monster.IsWeakenFully)
            monster.energyGunController.BeginSwallowWeakPoints();
    }

    public override void OnReachedToTarget()
    {
        monster.animator.SetTrigger("SwallowAttack");
        monster.audioSource.PlayOneShot(monster.bigAttackSound);
    }

    public override void OnAnimationAttack()
    {
        monster.audioSource.volume = 0f;
        monster.StartCoroutine(monster.GameOverSequenceCoroutine());
    }

    public override void OnAttackSuccess()
    {

    }

    public override void OnAttackMiss()
    {
        FocusManager.Instance.PopFocusState();
        if (monster.CurrentFinalPatternCount == monster.MaxFinalPatternCount)
        {
            monster.ClearFinalPattern(); // 최종 패턴 클리어
        }
        else
        {
            monster.ChangeState(new RetreatState());
        }
    }

    public override bool UpdateMovement()
    {
        return true;
    }
}
