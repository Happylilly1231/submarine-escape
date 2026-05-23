using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMonsterStates
{
    /// <summary>
    /// 내부 괴물 - 공격 상태 
    /// <para>- 플레이어를 공격한다.</para>
    /// <para>- 공격 종류: 근접 - 때리기 공격, 두 번 할퀴기 공격 / 원거리 - 점프 공격, 던지기 공격</para>
    /// <para>- 플레이어와의 거리에 따라 공격 패턴(근접/원거리)을 선택한다.</para>
    /// <para>- 단, 원거리 공격 거리를 벗어났을 시 추적 상태로 전환된다.</para>
    /// <para>- 선택된 공격 패턴에서 랜덤으로 하나의 공격이 선택되고 해당 애니메이션이 진행된다.</para>
    /// <para>- 실제 공격과 공격 쿨타임 시작은 애니메이션 이벤트로 실행된다.(InnerMonsterController 코드)</para>
    /// </summary>
    public class AttackState : IState<InnerMonsterController>
    {
        private float _attackChaseSpeed = 5f; // 공격할 때 추적 속도
        private float _stopDistance = 2f; // 멈춤 거리
        private float _resumeDistance = 2.3f; // 다시 움직이기 시작할 거리
        private bool _isClose; // 가까운지 여부
        private float targetWeight;

        public void Enter(InnerMonsterController owner)
        {
            AttackCurrentPattern(owner); // 현재 패턴 공격(근접/원거리)

            // 괴물 난이도에 따른 설정
            if (GameManager.instance.CurrentDifficulty == Difficulty.Easy)
            {
                owner.CanMove(false); // 이동 정지
            }
            else
            {
                owner.CanMove(true); // 이동
                owner.Nav.speed = _attackChaseSpeed; // 공격할 때 추적 속도로 속도 변경

                // 점프 공격일 때는 하체 레이어 비활성화, 그 외 공격은 하체 레이어 활성화 (하체 움직임 추가해야 하므로)
                if (owner.currentAttackType == EAttackType.JumpAttack)
                    owner.Animator.SetLayerWeight(owner.lowerBodyLayerIndex, 0f);
                else
                    owner.Animator.SetLayerWeight(owner.lowerBodyLayerIndex, 1f);

                // Debug.Log("Weight: " + owner.Animator.GetLayerWeight(owner.lowerBodyLayerIndex) + " / " + owner.currentAttackType);
            }

            owner.monsterEyeRenderer.material = owner.redEyeMaterial; // 눈 색 빨간색으로 변경
            owner.ChangeFovCenter(true); // 시야각 중심 위치를 그냥 트랜스폼으로 변경
            owner.Nav.updateRotation = false; // 회전 수동으로 변경 - NavMeshAgent의 기본 회전 사용 X(너무 느림)
            owner.ChangeMonsterModelCenter(true); // 몬스터 모델 중심 변경
        }

        public void Update(InnerMonsterController owner)
        {
            // 공격 도중 플레이어 완전 괴물화 시 -> 바로 순찰 상태로 전환 
            if (owner.IsPlayerMutationCompeleted)
            {
                owner.Animator.Play("Walk");
                owner.ChangeState(new PatrolState());
                return;
            }

            // 현재 남은 거리 - 경로 계산 중이 아닐 때만 남은 거리로 비교하고, 계산 중일 때는 직접 거리 구해 사용
            float currentDistance = owner.Nav.pathPending
                ? Vector3.Distance(owner.transform.position, owner.PlayerTransform.position)
                : owner.Nav.remainingDistance;

            owner.LookAtTarget(owner.PlayerTransform.position); // 현재 플레이어 위치를 바라보도록 회전

            if (GameManager.instance.CurrentDifficulty == Difficulty.Easy)
            {
                // 점프 중 -> 플레이어를 향해 이동
                if (owner.IsJumping)
                {
                    if (owner.DistToPlayer <= _stopDistance)
                        owner.CanMove(false);
                    else
                        owner.CanMove(true);

                    owner.Nav.SetDestination(owner.PlayerTransform.position);
                }
            }
            else
            {
                owner.Nav.SetDestination(owner.PlayerTransform.position);

                if (owner.currentAttackType == EAttackType.JumpAttack)
                {
                    if (owner.IsJumping)
                    {
                        // 점프 중 이동 로직은 별도 처리
                        owner.CanMove(currentDistance > _stopDistance);
                    }
                }
                else
                {
                    // 거리에 따른 이동 및 가중치 판단
                    if (!_isClose && owner.CanDetect() && currentDistance <= _stopDistance)
                    {
                        _isClose = true;
                        owner.CanMove(false);
                        owner.StopPlaying();
                        targetWeight = 0f; // 가까우면 하체 레이어 끔 (Idle 권장)
                    }
                    else if (_isClose && currentDistance > _resumeDistance)
                    {
                        _isClose = false;
                        owner.CanMove(true);
                        AudioManager.Instance.PlaySoundSafe(owner.audioSource, owner.chaseSound, 3f);
                        targetWeight = 1f; // 멀면 하체 레이어 켬 (Chase)
                    }

                    // 최종 가중치를 부드럽게 적용 (깜빡임 방지 핵심)
                    float currentWeight = owner.Animator.GetLayerWeight(owner.lowerBodyLayerIndex);
                    owner.Animator.SetLayerWeight(owner.lowerBodyLayerIndex,
                        Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * 10f));
                }
            }
        }

        public void Exit(InnerMonsterController owner)
        {
            owner.Animator.SetLayerWeight(owner.lowerBodyLayerIndex, 0f);
            owner.ChangeFovCenter(false); // 시야각 중심 위치를 머리 위치로 변경
            owner.monsterEyeRenderer.material = owner.originalEyeMaterial; // 내부 괴물의 눈 머티리얼 원래 머티리얼(하얀색)로 변경
            owner.Nav.updateRotation = true; // 회전 자동으로 변경
        }

        // 현재 패턴 공격(근접/원거리)
        void AttackCurrentPattern(InnerMonsterController monster)
        {
            if (monster.DistToPlayer <= monster.MeleeAttackDistance) // 근접 공격 거리 내(소등 상태 -> 원거리 공격 불가(원거리와 근접 공격 거리가 동일하므로 근접 공격 선택되거나 이 함수가 실행 되기 직전에 플레이어가 공격 거리에서 벗어난다고 하더라도 else 문으로 가므로 순찰 상태로 전환됨))
                MeleePattern(monster); // 근접 공격
            else if (monster.DistToPlayer <= monster.RangeAttackDistance) // 원거리 공격 거리 내
                RangePattern(monster); // 원거리 공격
            else // 공격 불가
                monster.ChangeState(new ChaseState()); // 추적 상태로 전환
        }

        // 근접 공격
        void MeleePattern(InnerMonsterController monster)
        {
            int r = Random.Range(0, 2);

            switch (r)
            {
                case 0:
                    monster.currentAttackType = EAttackType.HitAttack;
                    monster.Animator.SetInteger("attackType", 0);
                    monster.Animator.SetTrigger("Attack");
                    break;

                case 1:
                    monster.currentAttackType = EAttackType.DoubleClawAttack;
                    monster.Animator.SetInteger("attackType", 1);
                    monster.Animator.SetTrigger("Attack");
                    break;
            }
        }

        // 원거리 공격
        void RangePattern(InnerMonsterController monster)
        {
            int r = Random.Range(0, 2);

            switch (r)
            {
                case 0:
                    monster.currentAttackType = EAttackType.ThrowAttack;
                    monster.Animator.SetInteger("attackType", 2);
                    monster.Animator.SetTrigger("Attack");
                    break;

                case 1:
                    monster.currentAttackType = EAttackType.JumpAttack;
                    monster.Animator.SetInteger("attackType", 3);
                    monster.Animator.SetTrigger("Attack");
                    monster.StopPlaying();
                    break;
            }
        }
    }
}
