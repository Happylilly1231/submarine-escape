using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;

public class DeepSeaIntroCutScene : MonoBehaviour
{
    public static DeepSeaIntroCutScene Instance { get; private set; }

    [Header("=== Camera ===")]
    [SerializeField] private DeepSeaCameraController cameraController;

    [Header("=== Player ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerMovePoint;

    [Header("=== Submarine ===")]
    [SerializeField] private Transform submarine;
    [SerializeField] private Transform submarineFallPoint;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip explosion;
    [SerializeField] private AudioClip groaning;
    [SerializeField] private AudioClip falling;

    public bool IsCutScene { get; private set; } = false;

    private DeepSeaMonsterController _deepSeaMonsterController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _deepSeaMonsterController = FindObjectOfType<DeepSeaMonsterController>();
        _deepSeaMonsterController.StopFSM();
    }

    private void Start()
    {
        StartCutScene();
    }

    public void StartCutScene()
    {
        StartCoroutine(PlayCutScene());
    }

    private IEnumerator PlayCutScene()
    {
        // =====================================
        // 1. 게임 조작 잠금
        // =====================================

        InputManager.instance.DisableAllInputs(); // 모든 인풋 비활성화

        Debug.Log("심해 인트로 컷씬 시작");

        IsCutScene = true;

        // =====================================
        // 2. 플레이어 이동
        // =====================================

        // 제자리에서 좌우로 둘러보기
        yield return StartCoroutine(LookAround());

        // 목표 위치를 바라보도록 몸 회전
        yield return StartCoroutine(RotateTowardsTarget());

        // 목표 위치까지 이동
        yield return StartCoroutine(MoveToTarget());

        Debug.Log("플레이어 이동 완료");

        // =====================================
        // 3. 잠수함을 바라봄
        // =====================================

        yield return StartCoroutine(LookDown());

        // =====================================
        // 6. 잠수함 추락하면서 아래 바라봄
        // =====================================

        yield return StartCoroutine(FallSubmarineAndLookDown());

        yield return new WaitForSeconds(2f);

        // =====================================
        // 7. 플레이어 고개를 듬
        // =====================================

        yield return StartCoroutine(LookUp());

        // =====================================
        // 8. 컷씬 종료
        // =====================================

        IsCutScene = false;
        cameraController.EndCutScene();

        EndCutScene();
    }

    /// <summary>
    /// 두 회전 사이를 부드럽게 회전
    /// </summary>
    private IEnumerator RotatePlayerAndCamera(Quaternion startRotation, Quaternion targetRotation, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            Quaternion currentRotation =
                Quaternion.Slerp(startRotation, targetRotation, t);

            // 플레이어 몸 회전
            player.rotation = currentRotation;

            // 카메라도 같은 방향을 바라보도록 설정
            cameraController.SetCutSceneRotation(currentRotation);


            yield return null;
        }

        player.rotation = targetRotation;
        cameraController.SetCutSceneRotation(targetRotation);
    }

    /// <summary>
    /// 제자리에서 좌우로 고개를 돌림
    /// </summary>
    private IEnumerator LookAround()
    {
        float lookDuration = 1.5f;
        Quaternion startRotation = player.rotation;

        // 오른쪽
        Quaternion rightRotation =
            startRotation * Quaternion.Euler(0f, 60f, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(startRotation, rightRotation, lookDuration)
        );
        yield return new WaitForSeconds(0.5f);

        // 오른쪽에서 위쪽을 바라보며 왼쪽으로 이동
        Quaternion upperLeftRotation =
            startRotation * Quaternion.Euler(-40f, 0f, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(rightRotation, upperLeftRotation, lookDuration * 2f)
        );

        // 왼쪽
        Quaternion leftRotation =
            startRotation * Quaternion.Euler(0f, -45f, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(upperLeftRotation, leftRotation, lookDuration)
        );
        yield return new WaitForSeconds(0.5f);

        // 다시 정면
        Quaternion lastRotation =
            startRotation * Quaternion.Euler(5f, 0f, 0f);

        yield return StartCoroutine(
            RotatePlayerAndCamera(leftRotation, lastRotation, lookDuration * 0.7f)
        );
    }

    /// <summary>
    /// 목표 위치 방향으로 몸을 회전
    /// </summary>
    private IEnumerator RotateTowardsTarget()
    {
        Vector3 direction = playerMovePoint.position - player.position;

        // Y축 방향만 사용
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            yield break;

        Quaternion startRotation = player.rotation;

        // 몸은 목표 방향만 바라봄
        Quaternion playerTargetRotation =
            Quaternion.LookRotation(direction);

        // 카메라는 목표 방향 + 아래쪽 15도
        Quaternion cameraTargetRotation =
            Quaternion.LookRotation(direction) * Quaternion.Euler(15f, 0f, 0f);

        float elapsedTime = 0f;
        float duration = 2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            // 몸 회전
            Quaternion currentPlayerRotation =
                Quaternion.Slerp(startRotation, playerTargetRotation, t);

            player.rotation = currentPlayerRotation;

            // 카메라 회전
            Quaternion currentCameraRotation =
                Quaternion.Slerp(startRotation, cameraTargetRotation, t);

            cameraController.SetCutSceneRotation(currentCameraRotation);

            yield return null;
        }

        // 최종 회전
        player.rotation = playerTargetRotation;
        cameraController.SetCutSceneRotation(cameraTargetRotation);
    }

    /// <summary>
    /// 목표 위치까지 이동
    /// </summary>
    private IEnumerator MoveToTarget()
    {
        Vector3 startPosition = player.position;
        Vector3 targetPosition = playerMovePoint.position;

        float moveDuration = 5f;
        float elapsedTime = 0f;

        bool explosionPlayed = false;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / moveDuration);

            player.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            if (!explosionPlayed && elapsedTime >= 3f)
            {
                if (audioSource != null && explosion != null)
                {
                    Debug.Log("폭발음 재생!");
                    audioSource.PlayOneShot(explosion);
                    explosionPlayed = true;
                }
            }

            yield return null;
        }

        // 최종 위치 보정
        player.position = targetPosition;
    }

    /// <summary>
    /// 아래를 바라봄
    /// </summary>
    /// <returns></returns>
    private IEnumerator LookDown()
    {
        Quaternion startRotation = cameraController.transform.rotation;

        Quaternion targetRotation = startRotation * Quaternion.Euler(40f, 80f, 0f);

        float elapsedTime = 0f;
        float duration = 2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            Quaternion currentRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            player.rotation = currentRotation;

            cameraController.SetCutSceneRotation(currentRotation);

            yield return null;
        }

        cameraController.SetCutSceneRotation(targetRotation);
    }

    /// <summary>
    /// 잠수함이 아래로 추락
    /// </summary>
    private IEnumerator FallSubmarineAndLookDown()
    {
        // =====================================
        // 잠수함이 아래로 기울어짐
        // =====================================

        if (audioSource != null && groaning != null)
        {
            audioSource.clip = groaning;
            audioSource.time = 18f;
        }

        // 잠수함
        Quaternion startSubmarineRotation = submarine.rotation;
        Vector3 startSubmarinePosition = submarine.position;

        Quaternion targetSubmarineRotation =
            startSubmarineRotation * Quaternion.Euler(30f, 0f, 0f);
        Vector3 targetSubmarinePosition =
            startSubmarinePosition + submarine.forward * 10f;

        float elapsedTime = 0f;
        float tiltDuration = 5f;

        audioSource.Play();

        while (elapsedTime < tiltDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / tiltDuration);

            // 잠수함 회전
            submarine.rotation =
                        Quaternion.Slerp(startSubmarineRotation, targetSubmarineRotation, t);

            // 잠수함 앞으로 이동
            submarine.position =
                Vector3.Lerp(startSubmarinePosition, targetSubmarinePosition, t);

            yield return null;
        }

        audioSource.Stop();

        submarine.rotation = targetSubmarineRotation;
        submarine.position = targetSubmarinePosition;

        // =====================================
        // 잠수함: 아래로 추락
        // 플레이어: 떨어지는 잠수함을 따라 같이 고개가 내려감
        // =====================================

        if (audioSource != null && groaning != null)
        {
            audioSource.clip = falling;
        }

        // 잠수함
        startSubmarinePosition = submarine.position;
        Vector3 fallPosition = submarineFallPoint.position;

        startSubmarineRotation = submarine.rotation;
        targetSubmarineRotation =
            startSubmarineRotation * Quaternion.Euler(40f, 0f, 0f);

        // 플레이어
        Quaternion startPlayerRotation = player.rotation;
        Quaternion targetPlayerRotation =
            startPlayerRotation * Quaternion.Euler(10f, 0f, 0f);

        elapsedTime = 0f;
        float fallDuration = 2f;

        audioSource.Play();

        while (elapsedTime < fallDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / fallDuration);

            // 추락은 점점 가속
            float fallT = t * t;

            submarine.position =
                Vector3.Lerp(startSubmarinePosition, fallPosition, fallT);
            submarine.rotation =
                Quaternion.Slerp(startSubmarineRotation, targetSubmarineRotation, t);

            Quaternion currentRotation =
                Quaternion.Slerp(startPlayerRotation, targetPlayerRotation, t);

            // 카메라도 같은 방향을 바라보도록 설정
            cameraController.SetCutSceneRotation(currentRotation);

            yield return null;
        }

        submarine.gameObject.SetActive(false);
        cameraController.SetCutSceneRotation(targetPlayerRotation);
    }

    /// <summary>
    /// 위를 바라봄
    /// </summary>
    /// <returns></returns>
    private IEnumerator LookUp()
    {
        Quaternion startRotation = cameraController.transform.rotation;

        Quaternion targetRotation = startRotation * Quaternion.Euler(-50f, 0f, 0f);

        float elapsedTime = 0f;
        float duration = 2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            Quaternion currentRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            cameraController.SetCutSceneRotation(currentRotation);

            yield return null;
        }

        cameraController.SetCutSceneRotation(targetRotation);
    }

    /// <summary>
    /// 컷씬 종료 후 심해 설명 UI 활성화
    /// </summary>
    private void EndCutScene()
    {
        Debug.Log("심해 인트로 컷씬 종료");

        DeepSeaUIManager.Instance.SetDeepSeaGuide();

        FocusManager.Instance.PushFocusState(GameFocusState.DeepSeaUI);
    }
}
