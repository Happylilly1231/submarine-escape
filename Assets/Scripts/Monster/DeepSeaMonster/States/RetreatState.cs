using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DeepSeaMonsterStates
{
    public class RetreatState : IState<DeepSeaMonsterController>
    {
        private Vector3 _retreatDirection;
        private float _retreatTimer;
        private float _retreatSpeed = 30f;
        private float _rotateSpeed = 10f; // 회전 속도

        public void Enter(DeepSeaMonsterController owner)
        {
            _retreatTimer = 0f;

            owner.audioSource.DOFade(0.3f, 1.5f);

            // 1. 기본 퇴각 방향: 현재 바라보는 방향의 반대
            Vector3 defaultDir = -owner.transform.forward;

            // 2. 뒤쪽 지형 검사 (8m)
            if (Physics.Raycast(owner.transform.position, defaultDir, 8.0f, owner.obstacleLayerMask))
            {
                _retreatDirection = GetFreeRetreatDirection(owner);
            }
            else
            {
                _retreatDirection = defaultDir;
            }

            // 3. [선택 A] Enter 시점에 퇴각 방향을 즉시 마주보도록 회전시키고 싶다면:
            /*
            if (_retreatDirection != Vector3.zero)
            {
                owner.transform.rotation = Quaternion.LookRotation(_retreatDirection);
            }
            */
        }

        // Rigidbody 물리 연산이므로 FixedUpdate로 실행!
        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            _retreatTimer += Time.fixedDeltaTime;

            // 1. 회전 처리 (퇴각 방향 _retreatDirection을 바라보도록 회전)
            if (_retreatDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_retreatDirection);

                // Slerp로 부드럽게 회전
                Quaternion nextRotation = Quaternion.Slerp(
                    owner.Rb.rotation,
                    targetRotation,
                    _rotateSpeed * Time.fixedDeltaTime
                );

                owner.Rb.MoveRotation(nextRotation);
            }

            // 2. 이동 처리 (퇴각 방향으로 전속력 이동)
            Vector3 targetPos = owner.transform.position + (_retreatDirection * _retreatSpeed * Time.fixedDeltaTime);
            owner.Rb.MovePosition(targetPos);

            // 3. 1.5초 지나면 쿨다운 상태로 전환
            if (_retreatTimer > 1.5f)
            {
                owner.ChangeState(new HiddenState());
            }
        }

        // IState 구현상 Update를 비워둠 (FixedUpdate를 따로 사용하는 구조인 경우)
        public void Update(DeepSeaMonsterController owner) { }

        public void Exit(DeepSeaMonsterController owner) { }

        private Vector3 GetFreeRetreatDirection(DeepSeaMonsterController monster)
        {
            Vector3[] checkDirections = { Vector3.up, monster.transform.right, -monster.transform.right };

            foreach (var dir in checkDirections)
            {
                if (!Physics.Raycast(monster.transform.position, dir, 8.0f, monster.obstacleLayerMask))
                {
                    return dir;
                }
            }

            return Vector3.up;
        }
    }
}