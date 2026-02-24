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
    public float FastDecreaseAmount { get; set; } = 0f;
    public float Gauge { get; private set; } // 범위: 0 ~ 1
    public InputAction ForceKey { get; set; }

    private float _increaseAmount = 100f;
    private float _decreaseAmount = 20f;
    // private float _fastDecreaseAmount = 400f;
    private float _circleMaxSize = 0.2f;
    private Transform _circleTransform;
    private Material _circleMaterial;
    private float _enableTimer = 0f;
    private Color _greenColor = new Color(0f, 1f, 0f, 0.2f);
    private Color _orangeColor = new Color(255f / 255f, 165f / 255f, 0f, 0.2f);
    private Color _yellowColor = new Color(255f / 255f, 255f / 255f, 0f, 0.2f);
    private Color _redColor = new Color(1f, 0f, 0f, 0.2f);

    private float _minCurrentAngleAmount = 0f;

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
        _minCurrentAngleAmount = 0f;
        CurrentAngleAmount = 0f;
        Gauge = 0f;
        _enableTimer = 0f;
        transform.GetChild(0).localScale = new Vector3(0f, 0f, 1f);
        ForceKey.started += torpedoTubeHandle.OnForceKeyStarted;
        ForceKey.canceled += torpedoTubeHandle.OnForceKeyCanceled;
        SetKeyIconActive(true);

        CancelForce(torpedoTubeHandle.ForcePointCounts[torpedoTubeHandle.CurrentForcePartIndex]);
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
        // float startAngle = torpedoTubeHandle.TargetAngles[torpedoTubeHandle.CurrentForcePartIndex] - torpedoTubeHandle.CurrentRange;

        // while (Gauge < 1f)
        // {
        //     CurrentAngleAmount += _increaseAmount * Time.deltaTime;
        //     if (CurrentAngleAmount < 0)
        //     {
        //         // 빨간 원 표시
        //         _circleMaterial.SetColor("_TintColor", redColor);
        //         float scale = (-CurrentAngleAmount) / startAngle * _circleMaxSize;
        //         _circleTransform.localScale = new Vector3(scale, scale, 1f);
        //     }
        //     else // 현재 각도 변화량이 0 이상일 때만 게이지 갱신(변화량 0부터 게이지 0)
        //     {
        //         Gauge = Mathf.Clamp01(CurrentAngleAmount / (torpedoTubeHandle.CurrentRange / cnt)); // 게이지 갱신

        //         _circleMaterial.SetColor("_TintColor", yellowColor);
        //         float scale = Gauge * _circleMaxSize;
        //         _circleTransform.localScale = new Vector3(scale, scale, 1f);
        //     }

        //     yield return null;
        // }

        // // 게이지 채우기 성공
        // torpedoTubeHandle.CurrentCompletePointCnt++; // 완료 개수 1 증가
        // gameObject.SetActive(false); // 안 보이게 하기
        // torpedoTubeHandle.CurrentPressingForcePoint = null;
        // ForceKey.started -= torpedoTubeHandle.OnForceKeyStarted;
        // ForceKey.canceled -= torpedoTubeHandle.OnForceKeyCanceled;

        // 최소 각도량 갱신
        if (CurrentAngleAmount < _minCurrentAngleAmount)
            _minCurrentAngleAmount = CurrentAngleAmount;

        _circleMaterial.SetColor("_TintColor", _greenColor);

        float scale;
        while (Gauge < 1f)
        {
            CurrentAngleAmount += _increaseAmount * Time.deltaTime;
            // if (CurrentAngleAmount < 0f)
            //     _circleMaterial.SetColor("_TintColor", _yellowColor);
            // else
            //     _circleMaterial.SetColor("_TintColor", _greenColor);
            Gauge = Mathf.Clamp01((CurrentAngleAmount - _minCurrentAngleAmount) / ((torpedoTubeHandle.CurrentRange / cnt) - _minCurrentAngleAmount)); // 게이지 갱신
            scale = Gauge * _circleMaxSize;
            _circleTransform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        if (_enableTimer < 0.7f)
            _enableTimer += Time.deltaTime;

        // 게이지 채우기 성공
        torpedoTubeHandle.CompleteForce(this);
    }

    private IEnumerator CancelForceCoroutine(int cnt)
    {
        // float startAngle = torpedoTubeHandle.TargetAngles[torpedoTubeHandle.CurrentForcePartIndex] - torpedoTubeHandle.CurrentRange;

        // while (torpedoTubeHandle.CurrentAngle > 0)
        // {
        //     if (CurrentAngleAmount < 0)
        //     {
        //         if (enalbeTimer < 1f)
        //         {
        //             CurrentAngleAmount -= 1f * Time.deltaTime; // 빠르게 감소
        //         }
        //         else
        //         {
        //             CurrentAngleAmount -= _fastDecreaseAmount * Time.deltaTime; // 빠르게 감소
        //         }
        //         // 빨간 원 표시
        //         _circleMaterial.SetColor("_TintColor", redColor);
        //         float scale = (-CurrentAngleAmount) / startAngle * _circleMaxSize;
        //         _circleTransform.localScale = new Vector3(scale, scale, 1f);
        //     }
        //     else // 현재 각도 변화량이 0 이상일 때만 게이지 갱신(변화량 0부터 게이지 0)
        //     {
        //         CurrentAngleAmount -= _decreaseAmount * Time.deltaTime; // 느리게 감소
        //         Gauge = Mathf.Clamp01(CurrentAngleAmount / (torpedoTubeHandle.CurrentRange / cnt)); // 게이지 갱신

        //         _circleMaterial.SetColor("_TintColor", yellowColor);
        //         float scale = Gauge * _circleMaxSize;
        //         _circleTransform.localScale = new Vector3(scale, scale, 1f);
        //     }

        //     yield return null;
        // }

        // _circleMaterial.SetColor("_TintColor", _redColor);

        float scale;
        while (torpedoTubeHandle.CurrentAngle > 0)
        {
            if (_enableTimer < 0.7f)
            {
                _enableTimer += Time.deltaTime;
                CurrentAngleAmount -= 1f * Time.deltaTime; // 매우 느리게 감소
                _circleMaterial.SetColor("_TintColor", _orangeColor);
                Gauge = Mathf.Clamp01((CurrentAngleAmount - _minCurrentAngleAmount) / ((torpedoTubeHandle.CurrentRange / cnt) - _minCurrentAngleAmount)); // 게이지 갱신
                scale = Gauge * _circleMaxSize;
            }
            else
            {
                if (CurrentAngleAmount < _minCurrentAngleAmount)
                {
                    CurrentAngleAmount -= FastDecreaseAmount * Time.deltaTime; // 빠르게 감소
                    _circleMaterial.SetColor("_TintColor", _redColor);
                    Gauge = 0f;
                    scale = _circleMaxSize;
                }
                else
                {
                    CurrentAngleAmount -= _decreaseAmount * Time.deltaTime; // 느리게 감소
                    _circleMaterial.SetColor("_TintColor", _orangeColor);
                    Gauge = Mathf.Clamp01((CurrentAngleAmount - _minCurrentAngleAmount) / ((torpedoTubeHandle.CurrentRange / cnt) - _minCurrentAngleAmount)); // 게이지 갱신
                    scale = Gauge * _circleMaxSize;
                }
            }
            // Gauge = Mathf.Clamp01((CurrentAngleAmount - _minCurrentAngleAmount) / ((torpedoTubeHandle.CurrentRange / cnt) - _minCurrentAngleAmount)); // 게이지 갱신
            // scale = Gauge * _circleMaxSize;
            _circleTransform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }
    }

    public void SetKeyIconActive(bool isActive)
    {
        float alpha = isActive ? 1f : 0.5f;

        Color color = Color.white;
        color.a = alpha;
        keyIconBackgroundRenderer.color = color;

        color = Color.black;
        color.a = alpha;
        keyIconTextRenderer.color = color;
    }

    private void OnDisable()
    {
        ForceKey.started -= torpedoTubeHandle.OnForceKeyStarted;
        ForceKey.canceled -= torpedoTubeHandle.OnForceKeyCanceled;
    }
}
