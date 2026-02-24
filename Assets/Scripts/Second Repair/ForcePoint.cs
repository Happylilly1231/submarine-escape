using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ForcePoint : MonoBehaviour
{
    [SerializeField] private TorpedoTubeHandle torpedoTubeHandle;
    [SerializeField] private SpriteRenderer keyIconBackgroundRenderer;
    [SerializeField] private SpriteRenderer keyIconTextRenderer;

    public float CurrentAngleAmount { get; private set; } // 범위: -startAngle ~ CurrentRange (예: 현재 구간 - [100 ~ 300도] -> -100 ~ +200)
    public float Gauge { get; private set; } // 범위: 0 ~ 1
    public InputAction ForceKey { get; set; }

    private float _increaseAmount = 50f;
    private float _decreaseAmount = 5f;
    private float _fastDecreaseAmount = 100f;
    private float _circleMaxSize = 0.2f;
    private Transform _circleTransform;
    private Material _circleMaterial;
    private float enalbeTimer = 0f;
    private Color yellowColor = new Color(255f / 255f, 220f / 255f, 90f / 255f, 20f / 255f);
    private Color redColor = new Color(1, 0, 0, 20f / 255f);

    private void Awake()
    {
        _circleTransform = transform.GetChild(0);
        _circleMaterial = _circleTransform.GetComponent<Renderer>().material;
    }

    /// <summary>
    /// 활성화될 때마다 초기화(풀링 사용하므로)
    /// </summary>
    private void OnEnable()
    {
        CurrentAngleAmount = 0f;
        Gauge = 0f;
        enalbeTimer = 0f;
        transform.GetChild(0).localScale = new Vector3(0f, 0f, 1f);
        ForceKey.started += torpedoTubeHandle.OnForceKeyStarted;
        ForceKey.canceled += torpedoTubeHandle.OnForceKeyCanceled;
        SetKeyIconActive(true);

        CancelForce(torpedoTubeHandle.ForcePointCounts[torpedoTubeHandle.CurrentForcePartIndex]);
    }

    private void Update()
    {
        enalbeTimer += Time.deltaTime;
    }

    public void StartForce(int cnt)
    {
        StopAllCoroutines();
        StartCoroutine(ForceCoroutine(cnt));
    }

    public void CancelForce(int cnt)
    {
        StopAllCoroutines();
        StartCoroutine(CancelForceCoroutine(cnt));
    }

    private IEnumerator ForceCoroutine(int cnt)
    {
        float startAngle = torpedoTubeHandle.TargetAngles[torpedoTubeHandle.CurrentForcePartIndex] - torpedoTubeHandle.CurrentRange;

        while (Gauge < 1f)
        {
            CurrentAngleAmount += _increaseAmount * Time.deltaTime;
            if (CurrentAngleAmount < 0)
            {
                // 빨간 원 표시
                _circleMaterial.SetColor("_TintColor", redColor);
                float scale = (-CurrentAngleAmount) / startAngle * _circleMaxSize;
                _circleTransform.localScale = new Vector3(scale, scale, 1f);
            }
            else // 현재 각도 변화량이 0 이상일 때만 게이지 갱신(변화량 0부터 게이지 0)
            {
                Gauge = Mathf.Clamp01(CurrentAngleAmount / (torpedoTubeHandle.CurrentRange / cnt)); // 게이지 갱신

                _circleMaterial.SetColor("_TintColor", yellowColor);
                float scale = Gauge * _circleMaxSize;
                _circleTransform.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        // 게이지 채우기 성공
        torpedoTubeHandle.CurrentCompletePointCnt++; // 완료 개수 1 증가
        gameObject.SetActive(false); // 안 보이게 하기
        torpedoTubeHandle.CurrentPressingForcePoint = null;
        ForceKey.started -= torpedoTubeHandle.OnForceKeyStarted;
        ForceKey.canceled -= torpedoTubeHandle.OnForceKeyCanceled;
    }

    private IEnumerator CancelForceCoroutine(int cnt)
    {
        float startAngle = torpedoTubeHandle.TargetAngles[torpedoTubeHandle.CurrentForcePartIndex] - torpedoTubeHandle.CurrentRange;

        while (torpedoTubeHandle.CurrentAngle > 0)
        {
            if (CurrentAngleAmount < 0)
            {
                if (enalbeTimer < 1f)
                {
                    CurrentAngleAmount -= 1f * Time.deltaTime; // 빠르게 감소
                }
                else
                {
                    CurrentAngleAmount -= _fastDecreaseAmount * Time.deltaTime; // 빠르게 감소
                }
                // 빨간 원 표시
                _circleMaterial.SetColor("_TintColor", redColor);
                float scale = (-CurrentAngleAmount) / startAngle * _circleMaxSize;
                _circleTransform.localScale = new Vector3(scale, scale, 1f);
            }
            else // 현재 각도 변화량이 0 이상일 때만 게이지 갱신(변화량 0부터 게이지 0)
            {
                CurrentAngleAmount -= _decreaseAmount * Time.deltaTime; // 느리게 감소
                Gauge = Mathf.Clamp01(CurrentAngleAmount / (torpedoTubeHandle.CurrentRange / cnt)); // 게이지 갱신

                _circleMaterial.SetColor("_TintColor", yellowColor);
                float scale = Gauge * _circleMaxSize;
                _circleTransform.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }
    }

    public void SetKeyIconActive(bool isActive)
    {
        float alpha = isActive ? 1f : 0.1f;

        Color color = Color.white;
        color.a = alpha;
        keyIconBackgroundRenderer.color = color;

        color = Color.black;
        color.a = alpha;
        keyIconTextRenderer.color = color;
    }
}
