using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class AttackState : IState<DeepSeaMonsterController>
    {
        private bool _isReached = false;
        private bool _isStopped = false;

        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(true);

            if (owner.IsExecutingFinalPattern) // 최종 패턴일 때
            {
                owner.CurrentFinalPatternCount++;
                if (owner.CurrentFinalPatternCount == 1 || owner.CurrentFinalPatternCount == owner.MaxFinalPatternCount)
                {
                    owner.currentPattern = new SwallowAttack(owner);
                }
                else
                {
                    owner.currentPattern = owner.AllPatternList[Random.Range(0, owner.AllPatternList.Count)];
                }
            }
            else
            {
                // 현재 패턴 결정
                if (owner.IsAmbush)
                    owner.currentPattern = new AmbushAttack(owner);
                else
                    owner.currentPattern = owner.AllPatternList[Random.Range(0, owner.AllPatternList.Count)];
            }

            Debug.Log("현재 공격 패턴: " + owner.currentPattern);

            // 스폰 위치에 스폰
            Vector3 worldOffset = owner.PlayerTransform.rotation * owner.currentPattern.SpawnOffset;
            Vector3 spawnPos = owner.underwaterPlayerMove.BasePosition + worldOffset;
            owner.transform.position = spawnPos;

            // 스폰 위치에서 플레이어를 향하는 방향 벡터 계산
            Vector3 dirToPlayer = owner.underwaterPlayerMove.BasePosition - spawnPos;
            dirToPlayer.y = 0f;
            // 방향 벡터가 0이 아닐 때만 회전 적용 (플레이어와 정확히 겹쳤을 때 에러 방지)
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
                owner.transform.rotation = lookRot;
            }

            owner.currentPattern.OnSpawned();
        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            // if (_isStopped) return;

            Vector3 worldOffset = owner.PlayerTransform.rotation * owner.currentPattern.TargetOffset;
            Vector3 targetPos = owner.underwaterPlayerMove.BasePosition + worldOffset;

            Vector3 toTarget = targetPos - owner.Rb.position;
            float distance = toTarget.magnitude;
            Debug.Log(distance);

            if (distance <= 0.5f)
            {
                owner.Rb.MovePosition(targetPos);


                if (!_isReached)
                {
                    _isReached = true;

                    // 도착 지점(targetPos)에서 플레이어의 BasePosition을 바라보도록 Y축 회전
                    Vector3 dirToBase = owner.underwaterPlayerMove.BasePosition - targetPos;
                    dirToBase.y = 0f;
                    if (dirToBase.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(dirToBase);
                        owner.transform.rotation = lookRot;
                        owner.Rb.MoveRotation(lookRot); // Rigidbody 사용 중이므로 함께 동기화
                    }

                    owner.currentPattern.OnReachedToTarget();
                }

                // if (owner.currentPattern.ShouldStop)
                //     _isStopped = true;
                return;
            }

            Vector3 moveDir = toTarget.normalized;
            owner.Rb.MovePosition(owner.Rb.position + moveDir * owner.currentPattern.MoveSpeed * Time.fixedDeltaTime);
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            owner.currentPattern = null;

            // 최종 패턴 예약 중 -> 현재 상태가 끝났으므로 최종 패턴 실행
            if (owner.IsFinalPatternPending)
            {
                owner.StartFinalPattern();
            }
        }
    }
}