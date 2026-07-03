using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerCutscene : MonoBehaviour
{
    [Header("플레이어 연출 설정")]
    public Animator animator;
    public Transform walkTargetPos;

    /// <summary>
    /// 괴물 컷씬 스크립트에서 플레이어의 행동을 시작시키기 위해 호출
    /// </summary>
    public IEnumerator PlayPlayerSequence()
    {
        animator.SetBool("isStand", true);
        yield return new WaitForSeconds(0.5f);

        while (Vector3.Distance(transform.position, walkTargetPos.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                walkTargetPos.position,
                0.5f * Time.deltaTime
            );
            yield return null;
        }
    }

    /// <summary>
    /// 괴물의 공격 타이밍에 맞춰 피격 사망 애니메이션 및 연출을 실행하는 함수
    /// </summary>
    public void TakeMonsterAttack()
    {
        animator.SetBool("death", true);
    }
}
