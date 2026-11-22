using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DeepSeaMonsterStates
{
    public class AmbushReadyState : IState<DeepSeaMonsterController>
    {
        private Sequence _seq;
        private bool _isDetectingMovement = false;

        public void Enter(DeepSeaMonsterController owner)
        {
            owner.monsterGeo.SetActive(false);
            _isDetectingMovement = false;

            _seq = DOTween.Sequence();
            _seq.AppendInterval(2f);
            _seq.AppendCallback(() =>
            {
                owner.heartBeatAudioSource.volume = 0.5f;
                owner.heartBeatAudioSource.Play();
            });
            _seq.Append(owner.heartBeatAudioSource.DOFade(1f, 5f));
            _seq.InsertCallback(5f, () =>
            {
                _isDetectingMovement = true;
            });
            _seq.Append(owner.heartBeatAudioSource.DOFade(0.5f, 3f));
            _seq.OnComplete(() =>
            {
                owner.heartBeatAudioSource.Stop();
                owner.ChangeState(new StealthMoveState());
            });
        }

        public void Update(DeepSeaMonsterController owner)
        {
            if (_isDetectingMovement)
            {
                // 움직임 감지 시 -> 기습 패턴 선택 및 공격 상태로 전환
                if (!owner.underwaterInput.IsStopping)
                {
                    owner.IsAmbush = true;
                    owner.ChangeState(new AttackState());
                }
            }
        }

        public void Exit(DeepSeaMonsterController owner)
        {
            if (_seq != null && _seq.IsActive())
            {
                _seq.Kill();
            }
            owner.heartBeatAudioSource.Stop();
        }
    }
}
