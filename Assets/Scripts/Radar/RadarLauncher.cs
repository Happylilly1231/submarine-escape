using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FireMode { Real, Animation } // 발사 모드 - 실제 / 애니메이션
public enum ExplosionTarget { DeepSeaMonster, Submarine2 } // 폭발 타겟 - 심해 괴물, 다른 잠수함

public class RadarLauncher : MonoBehaviour
{
    private RadarController _radarController;
    private RadarDisplay _radarDisplay;

    private float _torpedoSpeed = 50f; // 어뢰 속도
    private float _fadeDuration = 3f;
    private float _explosionDistance = 10f; // 폭발 반경

    private RadarTarget deepSeaMonster;
    private RadarTarget submarine2;

    private float _monsterWaitTime = 300f; // 심해 괴물 재등장 대기 시간: 5분

    private Coroutine _currentFireAnimationLoopCoroutine = null;
    private Coroutine _currentFireAnimCoroutine = null;

    private float _animationCoolDown = 2f;

    private void Awake()
    {
        _radarController = GetComponent<RadarController>();
        _radarDisplay = GetComponent<RadarDisplay>();

        deepSeaMonster = _radarController.deepSeaMonster;
        submarine2 = _radarController.submarine2;
    }

    /// <summary>
    /// 발사 반복 애니메이션 재생
    /// </summary>
    public void PlayFireAnimationLoop()
    {
        StopFireAnimationLoop();
        _currentFireAnimationLoopCoroutine = StartCoroutine(PlayFireAnimationLoopCoroutine());
    }

    private IEnumerator PlayFireAnimationLoopCoroutine()
    {
        while (!_radarController.IsFiring)
        {
            _currentFireAnimCoroutine = StartCoroutine(FireCoroutine(FireMode.Animation));
            yield return _currentFireAnimCoroutine;

            yield return new WaitForSeconds(_animationCoolDown);
        }
    }

    /// <summary>
    /// 발사 반복 애니메이션 재생 중지
    /// </summary>
    public void StopFireAnimationLoop()
    {
        Debug.Log(_currentFireAnimationLoopCoroutine);
        if (_currentFireAnimationLoopCoroutine != null)
        {
            StopCoroutine(_currentFireAnimationLoopCoroutine);
            if (_currentFireAnimCoroutine != null)
            {
                StopCoroutine(_currentFireAnimCoroutine);
            }
            _radarDisplay.ResetUIAfterFire(); // UI 초기화
            _radarDisplay.ResetExplosionRange();
            _currentFireAnimationLoopCoroutine = null;
        }
    }

    /// <summary>
    /// 실제 발사
    /// </summary>
    public void RealFire()
    {
        // 현재 실행 중이던 발사 코루틴 중지
        StopFireAnimationLoop();

        // 실제 어뢰 발사
        StartCoroutine(FireCoroutine(FireMode.Real));
        _radarDisplay.UpdateFireButtonActive(); // 발사 버튼 활성화 여부 갱신

        // 경보 발생
        SubmarineInGameManager.instance.AlertOn();
    }

    /// <summary>
    /// 어뢰 발사 코루틴
    /// </summary>
    /// <param name="fireMode">발사 모드(실제/애니메이션)</param>
    /// <returns></returns>
    public IEnumerator FireCoroutine(FireMode fireMode)
    {
        // 변수 초기화
        Vector3 originPos = Vector3.zero; // 원점
        Vector3 targetPos = _radarController.GetCurrentRealSelectPos(); // 목표 위치 -> 현재 선택 위치의 실제 좌표(플레이 상)
        Vector3 currentTorpedoPos = originPos; // 현재 어뢰 위치 -> 원점으로 초기화
        Vector3 dir = (targetPos - originPos).normalized; // 방향 계산
        List<ExplosionTarget> currentExplosionTargets = new List<ExplosionTarget>(); // 현재 폭발 가능 타겟

        // 실제 발사 여부에 따른 로직
        if (fireMode == FireMode.Real) // 실제 발사
        {
            // 발사 중으로 설정
            _radarController.IsFiring = true;

            UseTorpedo(); // 어뢰 소모
        }
        else if (fireMode == FireMode.Animation) // 애니메이션
        {
            // 선 색 변경!!!
        }

        // 어뢰와 선 초기화
        _radarDisplay.SetColorBeforFire(fireMode);
        _radarDisplay.SetFireTorpedoActive(true); // 발사 어뢰 활성화
        _radarDisplay.UpdateFiringTorpedoUI(originPos); // 발사 중인 어뢰 위치 초기화

        // 어뢰 이동 -> 타겟에 닿으면 폭발
        while (currentExplosionTargets.Count == 0) // 폭발 가능한 타겟이 없는 동안
        {
            // 정지 중일 때 -> 아무것도 안 함
            if (SubmarineInGameManager.instance.IsPausing)
                yield return null;

            // 어뢰 위치 업데이트
            currentTorpedoPos += dir * _torpedoSpeed * Time.deltaTime;
            _radarDisplay.UpdateFiringTorpedoUI(currentTorpedoPos);

            // 목표 위치에 도달했는지 여부에 따른 폭발 로직
            if (Vector3.Distance(currentTorpedoPos, targetPos) <= _torpedoSpeed * Time.deltaTime) // 현재 목표 위치에 도달 시 - 현 잠수함과 닿아도 폭발 가능
            {
                // 폭발 가능한 타겟 가져오기
                currentExplosionTargets = GetExplosionAvaiableTargets(currentTorpedoPos, _explosionDistance + 2.2f); // 폭발 거리(+ 타겟의 반지름보다 약간 작은 값(타겟에 스쳐도 폭발은 되어야 하므로)) 내 타겟

                // 폭발 보여주기
                _radarDisplay.ShowExplosion(currentTorpedoPos, fireMode, currentExplosionTargets.Count > 0);

                // 거리 내 타겟이 있으면 -> 폭발
                if (currentExplosionTargets.Count > 0)
                {
                    if (fireMode == FireMode.Real)
                    {
                        foreach (ExplosionTarget currentExplosionTarget in currentExplosionTargets)
                        {
                            // 현 잠수함이 폭발 반경 내에 있는 경우 -> 게임 오버
                            if (Vector3.Distance(currentTorpedoPos, Vector3.zero) <= _explosionDistance + 2.2f)
                            {
                                GameManager.instance.GameOver(EEndingType.SubmarineExplode); // 게임 오버
                                yield break; // 코루틴 자체 종료
                            }

                            RealExplodeWithTarget(currentExplosionTarget);
                        }
                    }
                }

                break; // 폭발이 완료되었으므로 발사 종료
            }
            else // 아직 목표 위치까지 이동 중일 때 - 현 잠수함과 닿아도 폭발하는 경우 X
            {
                // 폭발 가능한 타겟 가져오기
                currentExplosionTargets = GetExplosionAvaiableTargets(currentTorpedoPos, _torpedoSpeed * Time.deltaTime); // 어뢰가 현재 프레임에 이동하는 거리 내 타겟

                // 거리 내 타겟이 있으면 -> 폭발
                if (currentExplosionTargets.Count > 0)
                {
                    _radarDisplay.ShowExplosion(currentTorpedoPos, fireMode, true); // 폭발 보여주기

                    if (fireMode == FireMode.Real)
                    {
                        foreach (ExplosionTarget currentExplosionTarget in currentExplosionTargets)
                        {
                            RealExplodeWithTarget(currentExplosionTarget);
                        }
                    }

                    break; // 폭발이 완료되었으므로 발사 종료
                }
            }

            yield return null;
        }

        // 발사 후
        if (fireMode == FireMode.Real) // 실제 발사
        {
            _radarController.IsFiring = false; // 발사 중 아님으로 설정
        }
        else
        {
            _currentFireAnimCoroutine = null;
        }
        _radarDisplay.ResetUIAfterFire(); // UI 초기화
    }

    /// <summary>
    /// 어뢰 소모
    /// </summary>
    private void UseTorpedo()
    {
        _radarDisplay.DeactivateOneTorpedo(_radarController.CurrentTorpedoIndex); // 현재 발사할 어뢰 UI에서 비활성화
        _radarController.CurrentTorpedoIndex++;
        if (_radarController.CurrentTorpedoIndex < 3)
        {
            _radarDisplay.UpdateCurrentTorpedoStateUI();
        }
    }

    /// <summary>
    /// 폭발 가능 타겟들 가져오기
    /// </summary>
    /// <param name="currentTorpedoPos">현재 어뢰 위치</param>
    /// <param name="availableDistance">폭발 가능 거리</param>
    /// <returns>폭발 가능 타겟 리스트</returns>
    private List<ExplosionTarget> GetExplosionAvaiableTargets(Vector3 currentTorpedoPos, float availableDistance)
    {
        List<ExplosionTarget> explosionTargets = new List<ExplosionTarget>();

        Vector3[] targetPositions = { deepSeaMonster.CurrentPos, submarine2.CurrentPos };

        for (int i = 0; i < targetPositions.Length; i++)
        {
            // 현재 어뢰와 타겟과의 3차원 거리가 폭발 가능 거리 내에 있을 때 -> 폭발 가능 타겟 리스트에 추가
            if (Vector3.Distance(currentTorpedoPos, targetPositions[i]) <= availableDistance) // Z좌표까지 고려할 때 반경 내에 있음
            {
                explosionTargets.Add((ExplosionTarget)i);
            }
        }

        return explosionTargets;
    }

    /// <summary>
    /// 타겟과의 실제 폭발
    /// </summary>
    /// <param name="explosionTarget">폭발 타겟</param>
    private void RealExplodeWithTarget(ExplosionTarget explosionTarget)
    {
        switch (explosionTarget)
        {
            case ExplosionTarget.DeepSeaMonster: // 심해 괴물에 닿으면 -> 즉시 폭발, 심해 괴물 맞춤
                Debug.Log("심해 괴물 닿아서 폭발");
                HitMonster(); // 심해 괴물 맞춤
                break;
            case ExplosionTarget.Submarine2: // 다른 잠수함에 닿으면 -> 즉시 폭발, 이후 레이더에서 아예 사라짐(통신 불가)
                Debug.Log("다른 잠수함 닿아서 폭발");
                submarine2.IsCurrentActive = false; // 다른 잠수함 위치 갱신 중 아님으로 설정
                StartCoroutine(_radarDisplay.FadeInOut(false, submarine2, _fadeDuration)); // 다른 잠수함 페이드 아웃되면서 물러남(이후 아예 사라짐)
                break;
        }
    }

    /// <summary>
    /// 심해 괴물 맞춤
    /// </summary>
    private void HitMonster()
    {
        SubmarineInGameManager.instance.DeepSeaMonsterController.OnTorpedoHit(); // 심해 괴물 어뢰 맞았을 때 함수 호출
        SubmarineInGameManager.instance.IsFireSuccess = true;
        StartCoroutine(RunAwayMonster()); // 심해 괴물 도망
    }

    /// <summary>
    /// 심해 괴물 도망 코루틴
    /// </summary>
    private IEnumerator RunAwayMonster()
    {
        // 심해 괴물 위치 갱신 중 아님으로 설정
        deepSeaMonster.IsCurrentActive = false;

        // 심해 괴물 타이머 0부터 다시 시작하도록 초기화
        _radarController.ResetMonsterTimer();

        // 심해 괴물 좌표 텍스트 초기화
        deepSeaMonster.posText.text = "";

        // 후퇴
        yield return StartCoroutine(_radarDisplay.FadeInOut(false, deepSeaMonster, _fadeDuration)); // 심해 괴물 페이드 아웃되면서 물러남(코루틴 완료될 때까지 대기)

        // 대기
        yield return new WaitForSeconds(_monsterWaitTime); // 심해 괴물 다시 나타날 때까지 대기 시간만큼 대기

        // 재등장
        _radarController.CurrentMonsterPeriodIndex++;
        deepSeaMonster.IsCurrentActive = true; // 심해 괴물 위치 갱신 중으로 설정
        _radarController.MonsterStartAngle += 90f; // 시작 위치 변경을 위해 시작 각도 90 더해주기
        StartCoroutine(_radarDisplay.FadeInOut(true, deepSeaMonster, _fadeDuration)); // 심해 괴물 페이드 인되면서 나타남
    }
}
