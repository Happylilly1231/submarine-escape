using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시간 관리
/// <para>- 게임 시작 이후로 흐른 시간 변수</para>
/// </summary>
public class GameTime : MonoBehaviour
{
    public static GameTime Instance { get; private set; }
    public float TimeSinceStart { get; private set; } // 게임 시작 이후로 흐른 시간

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        if (SubmarineInGameManager.instance.IsPausing)
            return;

        TimeSinceStart += Time.deltaTime;
    }
}
