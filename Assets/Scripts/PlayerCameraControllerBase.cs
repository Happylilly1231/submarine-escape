using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerCameraControllerBase : MonoBehaviour
{
    [SerializeField] protected Transform cameraPos; // 카메라 위치
    public Transform CameraPos => cameraPos;

    protected bool _canControlInput = true;

    // 흔들림
    protected float _shakeIntensity = 0f;

    /// <summary>
    /// 플레이어 조작 입력 가능 여부 설정
    /// </summary>
    public void SetInputEnabled(bool isEnabled)
    {
        _canControlInput = isEnabled;
    }

    /// <summary>
    /// 카메라 위치 변경
    /// </summary>
    /// <param name="pos">위치</param>
    public void SetCameraPos(Transform pos)
    {
        cameraPos = pos;
    }

    /// <summary>
    /// 흔들림 설정
    /// </summary>
    /// <param name="intensity">흔들림 강도</param>
    public void SetShakeIntensity(float intensity)
    {
        _shakeIntensity = intensity;
    }
}
