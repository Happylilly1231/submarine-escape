using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DeepSeaMonsterStates
{
    public class StealthMoveState : IState<DeepSeaMonsterController>
    {
        private float _currentAngle;
        private float _angularSpeed = 0.5f; // 초당 회전 각도(라디안)
        private float _minRadius = 14f;
        private float _radiusVariation = 3f;
        private float _moveFollowSpeed = 8f;
        private int _direction = 1;         // 1: 시계, -1: 반시계
        private float _dirChangeTimer;
        private float _cooldownTimer;

        // 발광 연출
        private float _pulseTimer;
        private float _pulseSpeed = 2.2f;        // 빛이 일렁이는 속도
        private float _minLightIntensity = 0.1f; // 가장 어두울 때 밝기 (거의 꺼진 듯 희미함)
        private float _maxLightIntensity = 1.2f; // 가장 밝을 때 밝기 (은은한 발광)
        private float _originalLightIntensity;   // 은신 종료 후 복구할 원래 밝기

        private bool _isWaitingForPlayerAtCenter = false;
        private Coroutine _waitCoroutine = null;

        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(false);

            // 발광 라이트는 켜두고 원래 밝기 저장
            if (owner.monsterLight != null)
            {
                _originalLightIntensity = owner.monsterLight.intensity;
                owner.monsterLight.enabled = true;
                owner.monsterLight.intensity = _minLightIntensity;
            }

            if (owner.IsExecutingFinalPattern)
            {
                owner.monsterLight.enabled = false;
            }

            Vector3 dirToMonster = owner.Rb.position - owner.underwaterPlayerMove.BasePosition;
            _currentAngle = Mathf.Atan2(dirToMonster.z, dirToMonster.x);
            // _currentAngle = Random.Range(0f, Mathf.PI * 2f);

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
            // 최종 패턴 예약 중 -> 최종 패턴 즉시 실행
            if (owner.IsFinalPatternPending)
            {
                owner.StartFinalPattern();
                return;
            }

            if (!owner.IsExecutingFinalPattern)
                UpdateStealthLight(owner);

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
                    bool isAmbushReady = !owner.energyGunController.IsFullCharged && !owner.IsExecutingFinalPattern && Random.value <= 0.3f;
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

            float dynamicRadius = (_minRadius + _radiusVariation) + Mathf.Sin(Time.fixedTime * 1f) * _radiusVariation;

            Vector3 offset = new Vector3(
                Mathf.Cos(_currentAngle) * dynamicRadius,
                Mathf.Sin(_currentAngle * 0.5f) * 2f,
                Mathf.Sin(_currentAngle) * dynamicRadius
            );

            Vector3 targetPos = owner.underwaterPlayerMove.BasePosition + offset;

            Vector3 nextPos = Vector3.MoveTowards(owner.Rb.position, targetPos, _moveFollowSpeed * Time.fixedDeltaTime);
            owner.Rb.MovePosition(nextPos);
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            if (_waitCoroutine != null)
            {
                owner.StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            // 은신 종료 시 원래 빛 밝기로 복구
            if (owner.monsterLight != null)
            {
                owner.monsterLight.intensity = _originalLightIntensity;
            }

            if (owner.IsExecutingFinalPattern)
                owner.monsterLight.enabled = true;

            owner.audioSource.Stop(); // 이동 소리 정지
        }

        /// <summary>
        /// 심해 발광 기관처럼 빛이 서서히 약해졌다가 희미하게 켜지는 연출
        /// </summary>
        private void UpdateStealthLight(DeepSeaMonsterController owner)
        {
            if (owner.monsterLight == null) return;

            _pulseTimer += Time.deltaTime * _pulseSpeed;
            // 0 ~ 1 사이를 부드럽게 왕복 (Sin 파형 보정)
            float t = (Mathf.Sin(_pulseTimer) + 1f) * 0.5f;
            owner.monsterLight.intensity = Mathf.Lerp(_minLightIntensity, _maxLightIntensity, t);
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