using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SunkenSkullPoint : BackroomEntity
{
    [SerializeField] private GameObject puddle; // 물 웅덩이
    [SerializeField] private GameObject sunkenSkull; // 가라앉은 해골

    private Image _waterSurfaceImg; // 수면 이미지

    private InputAction _spaceAction;

    private Coroutine _tiltCoroutine;

    private void Start()
    {
        _spaceAction = SubmarineInGameManager.instance.playerInput.actions.FindAction("Puzzle/Space");
        _waterSurfaceImg = BackroomManager.Instance.waterSurfaceImg;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SpawnSunkenSkull(other.transform);
        }
    }

    private void SpawnSunkenSkull(Transform playerTransform)
    {
        SubmarineInGameManager.instance.SetFocus(true); // 포커스

        puddle.transform.localScale = Vector3.one; // 물 웅덩이 크기 초기화
        puddle.SetActive(true); // 물 웅덩이 활성화
        sunkenSkull.SetActive(true); // 가라앉은 해골 활성화

        Vector3 spawnPos = playerTransform.position + playerTransform.forward * 1f;
        spawnPos.y -= 1.5f;
        sunkenSkull.transform.position = spawnPos;

        Sequence seq = DOTween.Sequence();

        seq.Append(Camera.main.transform.DOLookAt(spawnPos, 0.5f));
        seq.Join(puddle.transform.DOScale(new Vector3(3f, 3f, 1f), 0.5f));
        seq.Append(sunkenSkull.transform.DOMoveY(1.5f, 1f)
            .SetRelative());
        seq.Join(sunkenSkull.transform.DOLookAt(SubmarineInGameManager.instance.player.transform.position + Vector3.up * 1f, 1f));
        seq.Append(sunkenSkull.transform.DOMoveY(-1.5f, 1f)
            .SetRelative());

        seq.AppendCallback(() =>
        {
            SubmarineInGameManager.instance.SetCameraControllerEnable(true);
            sunkenSkull.SetActive(false); // 가라앉은 해골 비활성화
        });
        // seq.AppendCallback(() =>
        // {
        //     UpdateDrawnEffects(0.5f);
        // });
        // seq.Append(playerTransform.DOLocalMoveY(-1f, 1f)
        //     .SetRelative());
        // seq.Join(waterSurfaceImg.rectTransform.DOLocalMoveY(1f, 1f)
        //     .SetRelative());

        seq.OnComplete(() =>
        {
            SubmarineInGameManager.instance.playerInput.ActivateInput();
            _spaceAction.Enable();
            SubmarineInGameManager.instance.player.GetComponent<PlayerMove>().enabled = false;
            SubmarineInGameManager.instance.player.GetComponent<CharacterController>().enabled = false;
            StartCoroutine(DrawnCoroutine());
        });
    }

    private IEnumerator DrawnCoroutine()
    {
        float escapeGauge = 0.5f;
        float timer = 3f;

        _waterSurfaceImg.gameObject.SetActive(true);

        // float t = 0f;
        // while (t < 0.5f)
        // {
        //     escapeGauge -= 1f * Time.deltaTime;
        //     UpdateDrawnEffects(escapeGauge);
        //     t += Time.deltaTime;

        //     yield return null;
        // }

        // 스페이스 키 활성화

        while (escapeGauge > 0f)
        {
            timer -= Time.deltaTime;

            // 1. 게이지 감소 로직
            float decayMultiplier = (timer > 0) ? 1f : 5f; // 3초 지나면 5배 빠르게 감소
            escapeGauge -= 0.3f * decayMultiplier * Time.deltaTime;

            // 2. 스페이스 입력 시 증가
            if (_spaceAction != null && _spaceAction.triggered)
            {
                Debug.Log("escapeGauge: " + escapeGauge + " / " + (3f - timer));
                escapeGauge += 0.1f;
            }

            // 3. 결과 체크
            escapeGauge = Mathf.Clamp01(escapeGauge);

            UpdateDrawnEffects(escapeGauge);

            // 성공
            if (escapeGauge >= 1f)
            {
                Success();
                yield break;
            }

            yield return null;
        }

        // 실패
        Fail();
    }

    private void Success()
    {
        _waterSurfaceImg.gameObject.SetActive(false);
        GetComponent<Collider>().enabled = false;

        SubmarineInGameManager.instance.player.GetComponent<PlayerMove>().enabled = true;
        SubmarineInGameManager.instance.player.GetComponent<CharacterController>().enabled = true;
        SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제
        _spaceAction.Disable();

        Sequence seq = DOTween.Sequence();
        seq.Append(puddle.transform.DOScale(new Vector3(1f, 1f, 1f), 1f));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void Fail()
    {
        UpdateDrawnEffects(1f); // 초기화
        puddle.SetActive(false); // 물 웅덩이 비활성화
        _waterSurfaceImg.gameObject.SetActive(false);

        _spaceAction.Disable();
        KillPlayer(); // 플레이어 죽이기
        SubmarineInGameManager.instance.player.GetComponent<PlayerMove>().enabled = true;
        SubmarineInGameManager.instance.player.GetComponent<CharacterController>().enabled = true;
        SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제
    }

    // escapeGauge가 변할 때마다 호출
    private void UpdateDrawnEffects(float gauge)
    {
        // 게이지가 0일 때 -> 가장 가라앉은 상태

        float waterSurfaceY = Mathf.Lerp(0f, -960f, gauge);
        _waterSurfaceImg.rectTransform.anchoredPosition = new Vector3(0, waterSurfaceY, 0);

        float playerY = Mathf.Lerp(-1.5f, 0.1f, gauge);
        Vector3 currentPlayerPos = SubmarineInGameManager.instance.player.transform.position;
        SubmarineInGameManager.instance.player.transform.position = new Vector3(currentPlayerPos.x, playerY, currentPlayerPos.z);

        float cameraTargetX = Mathf.Lerp(-60f, 0f, gauge);
        // // 카메라의 로컬 회전값 적용
        // Camera.main.transform.localRotation = Quaternion.Euler(cameraXRotation, Camera.main.transform.localEulerAngles.y, 0f);

        // 이미 돌고 있는 코루틴이 있다면 멈추고 새로 시작 (드득거림 방지)
        if (_tiltCoroutine != null) StopCoroutine(_tiltCoroutine);
        _tiltCoroutine = StartCoroutine(SmoothRotateCamera(cameraTargetX));
    }

    private IEnumerator SmoothRotateCamera(float targetX)
    {
        float duration = 0.3f; // 0.3초 동안 부드럽게 이동 (조절 가능)
        float elapsed = 0f;
        float startX = Camera.main.transform.localEulerAngles.x;

        // EulerAngles 특성상 180도가 넘어가면 계산이 꼬일 수 있으니 보정
        if (startX > 180) startX -= 360;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 부드러운 커브(SmoothStep) 적용
            float currentX = Mathf.Lerp(startX, targetX, t);

            Vector3 rot = Camera.main.transform.localEulerAngles;
            Camera.main.transform.localRotation = Quaternion.Euler(currentX, rot.y, rot.z);

            yield return null; // 다음 프레임까지 대기
        }
    }
}
