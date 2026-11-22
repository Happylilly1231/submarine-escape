using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaMonsterStates
{
    public class StealthMoveState : IState<DeepSeaMonsterController>
    {
        private float _currentAngle;
        private float _angularSpeed = 1.2f; // 초당 회전 각도(라디안)
        private int _direction = 1;         // 1: 시계, -1: 반시계
        private float _dirChangeTimer;
        private float _cooldownTimer;

        private bool _isWaitingForPlayerAtCenter = false;
        private Coroutine _waitCoroutine = null;

        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(false);

            _currentAngle = Random.Range(0f, Mathf.PI * 2f);
            _direction = Random.value > 0.5f ? 1 : -1;
            _dirChangeTimer = Random.Range(1.5f, 3.0f);
            _cooldownTimer = 0f;
            _isWaitingForPlayerAtCenter = false;

            // 이동 소리 재생
            owner.audioSource.clip = owner.moveSound;
            owner.audioSource.Play();
        }

        public void Update(DeepSeaMonsterController owner)
        {
            if (_isWaitingForPlayerAtCenter) return;

            _dirChangeTimer -= Time.deltaTime;
            if (_dirChangeTimer <= 0f)
            {
                _direction *= -1;
                _dirChangeTimer = Random.Range(1.5f, 3.0f);
            }

            _cooldownTimer += Time.deltaTime;
            if (_cooldownTimer >= owner.AttackCoolDown)
            {
                if (CheckPlayerAtCenter(owner))
                {
                    bool isAmbushReady = Random.value <= 0.3f;
                    if (isAmbushReady)
                        owner.ChangeState(new AmbushReadyState());
                    else
                        owner.ChangeState(new AttackState());
                }
            }
        }

        public void FixedUpdate(DeepSeaMonsterController owner)
        {
            _currentAngle += _angularSpeed * _direction * Time.fixedDeltaTime;
            float dynamicRadius = 11f + Mathf.Sin(Time.fixedTime * 1.5f) * 4f;

            Vector3 offset = new Vector3(
                Mathf.Cos(_currentAngle) * dynamicRadius,
                Mathf.Sin(_currentAngle * 0.5f) * 2.5f,
                Mathf.Sin(_currentAngle) * dynamicRadius
            );

            Vector3 targetPos = owner.underwaterPlayerMove.BasePosition + offset;
            owner.Rb.MovePosition(targetPos);
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            if (_waitCoroutine != null)
            {
                owner.StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            owner.audioSource.Stop(); // 이동 소리 정지
        }

        private bool CheckPlayerAtCenter(DeepSeaMonsterController owner)
        {
            if (owner.underwaterPlayerMove.IsDodging || owner.underwaterPlayerMove.IsUpwardDashing)
            {
                _waitCoroutine = owner.StartCoroutine(CheckPlayerAtCenterRoutine(owner));
                return false;
            }
            else
                return true;
        }

        private IEnumerator CheckPlayerAtCenterRoutine(DeepSeaMonsterController owner)
        {
            _isWaitingForPlayerAtCenter = true;
            UnderwaterPlayerMove playerMove = owner.underwaterPlayerMove;

            if (playerMove.IsDodging)
                yield return new WaitUntil(() => playerMove.IsDodging == false);
            else if (playerMove.IsUpwardDashing)
                yield return new WaitUntil(() => playerMove.IsUpwardDashing == false);

            _isWaitingForPlayerAtCenter = false;

            bool isAmbushReady = Random.value <= 0.3f;
            if (isAmbushReady)
                owner.ChangeState(new AmbushReadyState());
            else
                owner.ChangeState(new AttackState());
        }
    }
}