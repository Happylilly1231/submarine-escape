using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class AttackState : IState<DeepSeaMonsterController>
    {
        private bool _isReached = false;
        Vector3 currentTargetOffset;
        float currentSpeed;

        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(true); // 괴물 외형 활성화

            // 현재 공격 패턴 설정
            DecideCurrentAttackPattern(owner);

            // 현재 공격 패턴의 스폰 위치에 괴물 스폰
            Vector3 currentSpawnOffset = owner.currentPattern.SpawnOffset;
            if (owner.currentPattern.CanAttackFromBehind) // 뒤에서 가능한 공격이면
            {
                if (!owner.IsExecutingFinalPattern && Random.value > 0.5f) // 이 확률로 뒤에서 나오게 오프셋의 z 반전
                {
                    Debug.Log("플레이어 뒤에서 공격!");
                    owner.IsBehindPlayer = true;
                    currentSpawnOffset.z = -currentSpawnOffset.z;
                }
            }
            Vector3 worldOffset = owner.PlayerTransform.rotation * currentSpawnOffset; // 플레이어 로컬 기준 오프셋을 월드 기준 오프셋으로 변환
            Vector3 spawnPos = owner.underwaterPlayerMove.BasePosition + worldOffset; // 스폰 위치 월드 좌표
            owner.transform.position = spawnPos; // 스폰 위치에 괴물 위치

            if (owner.currentPattern.LookAtPlayerWhileMoving)
            {
                // 기본적으로 플레이어를 바라보는 방향으로 회전 적용
                Vector3 dirToPlayer = owner.underwaterPlayerMove.BasePosition - spawnPos; // 스폰 위치에서 플레이어를 향하는 방향 벡터 계산
                // dirToPlayer.y = 0f;
                if (dirToPlayer.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
                    owner.transform.rotation = lookRot;
                }
            }

            currentTargetOffset = owner.currentPattern.TargetOffset;
            currentSpeed = owner.currentPattern.MoveSpeed;
            if (owner.IsBehindPlayer)
            {
                currentTargetOffset.z = -currentTargetOffset.z;
                currentSpeed /= 2f;
            }

            owner.currentPattern.OnSpawned();
        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        /// <summary>
        /// <para>스폰 위치에서 목표 위치까지 이동 (목표 위치를 플레이어의 BasePosition 기준으로 계속 갱신(플레이어가 기본적으로 상승하기 때문에 괴물을 화면에 계속 보일 수 있게 하기 위해))</para>
        /// <para>목표 위치까지 이동 완료했어도 계속 목표 위치를 갱신해서 따라서 이동(도달해서 공격을 하면서도 괴물이 플레이어를 따라가야 공격이 성공하고 화면에 들어오게 하기 위해)</para>
        /// </summary>
        /// <param name="owner"></param>
        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            // 목표 위치를 플레이어의 BasePosition 기준으로 계속 갱신
            Vector3 worldOffset = owner.PlayerTransform.rotation * currentTargetOffset; // 오프셋을 월드 기준으로 변환
            Vector3 targetPos = owner.underwaterPlayerMove.BasePosition + worldOffset; // 플레이어의 BasePosition에 월드 오프셋 더한 것을 목표 위치로 갱신

            // 이동 방향과 남은 거리 계산
            Vector3 toTarget = targetPos - owner.Rb.position;
            float distance = toTarget.magnitude;

            // 목표 위치에 거의 도달했을 때
            if (distance <= 0.5f)
            {
                // 목표 위치로 현재 위치 이동 (이미 한 번 도달했더라도 현재 갱신된 목표 위치로 따라 이동해줘야 함)
                owner.Rb.MovePosition(targetPos);

                // 한 번도 도달한 적 없으면
                if (!_isReached)
                {
                    _isReached = true; // 도착 플래그 true

                    if (owner.currentPattern.LookAtPlayerWhileMoving)
                    {
                        // 목표 위치에서 플레이어의 BasePosition을 바라보도록 회전
                        Vector3 dirToBase = owner.underwaterPlayerMove.BasePosition - targetPos;
                        // dirToBase.y = 0f;
                        if (dirToBase.sqrMagnitude > 0.001f)
                        {
                            Quaternion lookRot = Quaternion.LookRotation(dirToBase);
                            owner.transform.rotation = lookRot;
                            owner.Rb.MoveRotation(lookRot); // Rigidbody 사용 중이므로 함께 동기화
                        }
                    }

                    owner.currentPattern.OnReachedToTarget();
                }

                return;
            }

            Vector3 moveDir = toTarget.normalized;

            Vector3 nextPos = owner.Rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
            owner.Rb.MovePosition(nextPos);
            if (owner.currentPattern.LookAtPlayerWhileMoving)
            {
                // 플레이어를 바라보는 방향으로 회전
                Vector3 dirToPlayer = owner.underwaterPlayerMove.BasePosition - nextPos;
                // dirToPlayer.y = 0f;
                if (dirToPlayer.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
                    owner.Rb.MoveRotation(lookRot);
                }
            }
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            owner.currentPattern = null;

            if (owner.IsBehindPlayer)
            {
                owner.IsBehindPlayer = false;
            }

            // 최종 패턴 예약 중 -> 현재 상태가 끝났으므로 최종 패턴 실행
            if (owner.IsFinalPatternPending)
            {
                owner.StartFinalPattern();
            }
        }

        /// <summary>
        /// 현재 공격 패턴 설정
        /// </summary>
        private void DecideCurrentAttackPattern(DeepSeaMonsterController monster)
        {
            if (monster.IsExecutingFinalPattern) // 최종 패턴일 때
            {
                monster.CurrentFinalPatternCount++;
                if (monster.CurrentFinalPatternCount == 1 || monster.CurrentFinalPatternCount == monster.MaxFinalPatternCount)
                {
                    monster.currentPattern = new SwallowAttack(monster);
                }
                else
                {
                    monster.currentPattern = monster.AllPatternList[Random.Range(0, monster.AllPatternList.Count)];
                }
            }
            else
            {
                // 현재 패턴 결정
                if (monster.IsAmbush)
                    monster.currentPattern = new AmbushAttack(monster);
                else
                    monster.currentPattern = monster.AllPatternList[Random.Range(0, monster.AllPatternList.Count)];
            }

            Debug.Log("현재 공격 패턴: " + monster.currentPattern);
        }
    }
}