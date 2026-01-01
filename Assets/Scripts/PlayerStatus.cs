using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 상태 관리
/// <para> - 버프/디버프 효과 함수들 </para>
/// </summary>
public class PlayerStatus : MonoBehaviour
{
    private bool _isStunned = false;
    public bool IsStunned => _isStunned;
    private float _speedScale = 1f;
    public float SpeedScale => _speedScale;

    // 스턴 효과
    public IEnumerator StunEffect()
    {
        _isStunned = true;
        yield return new WaitForSeconds(2f);
        _isStunned = false;
    }

    // 슬로우 효과
    public IEnumerator SlowEffect()
    {
        _speedScale = 0.2f;
        yield return new WaitForSeconds(2f);
        _speedScale = 1f;
    }
}
