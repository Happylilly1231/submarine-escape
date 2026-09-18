using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepSeaMonsterAppearCutScene : MonoBehaviour
{
    private DeepSeaPlayerMove _deepSeaPlayerMove;
    private DeepSeaMonsterController _deepSeaMonsterController;
    private DeepSeaCameraController _deepSeaCameraController;

    public static DeepSeaMonsterAppearCutScene Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _deepSeaPlayerMove = FindAnyObjectByType<DeepSeaPlayerMove>();
        _deepSeaMonsterController = FindAnyObjectByType<DeepSeaMonsterController>();
        _deepSeaCameraController = FindAnyObjectByType<DeepSeaCameraController>();
    }

    /// <summary>
    /// 괴물 등장 연출 재생
    /// </summary>
    public void PlayMonsterAppearSequence()
    {
        StartCoroutine(MonsterAppearSequenceCoroutine());
    }

    /// <summary>
    /// 괴물 등장 연출 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator MonsterAppearSequenceCoroutine()
    {
        // 카메라 입력 막기
        _deepSeaCameraController.SetInputEnabled(false);
        _deepSeaCameraController.IsInCutScene = true;

        // 연출 시작 시점의 원래 회전값(정면) 저장
        Quaternion startRot = _deepSeaPlayerMove.transform.rotation;

        // 위로 상승 목표 위치 지정
        Vector3 targetPos = _deepSeaPlayerMove.transform.position + Vector3.up * 25f;
        float moveDuration = 5f;

        // 위치 이동 시작 (5초 동안)
        _deepSeaPlayerMove.MoveToLocationForCutscene(targetPos, moveDuration);

        // 괴물 소리 재생
        AudioManager.Instance.PlayGlobalOneShot(_deepSeaMonsterController.spawnSound);

        // 이동하는 5초 동안 1초 간격으로 위/아래 두리번거리기
        float timer = 0f;
        while (timer < moveDuration)
        {
            // 위 바라보기
            Quaternion upRot = startRot * Quaternion.Euler(-70f, 0f, 0f);
            _deepSeaPlayerMove.RotateToForCutscene(upRot, 2.5f);
            yield return new WaitForSeconds(2.5f);
            timer += 2.5f;

            if (timer >= moveDuration) break;

            // 아래 바라보기
            Quaternion downRot = startRot * Quaternion.Euler(70f, 0f, 0f);
            _deepSeaPlayerMove.RotateToForCutscene(downRot, 2.5f);
            yield return new WaitForSeconds(2.5f);
            timer += 2.5f;
        }

        // 연출 종료 후 플레이어 조작권 복구
        _deepSeaPlayerMove.ReleaseCutsceneControl();

        // 괴물 FSM 시작
        _deepSeaMonsterController.StartFSM();
    }



}
