using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerReservation
{
    public float targetTime;
    public Action action;
    public bool isPeriodic; // 주기적
}

/// <summary>
/// 게임 시간 관리
/// <para>- 게임 시작 이후로 흐른 시간 변수</para>
/// </summary>
public class GameTime : MonoBehaviour
{
    public static GameTime Instance { get; private set; }
    public float TimeSinceStart { get; private set; } // 게임 시작 이후로 흐른 시간

    [SerializeField] private TextMeshProUGUI timeText;

    private bool _isPausing = false;

    // 특정 시간에 실행될 액션들을 모아두는 리스트 (예약 명단)
    private List<TimerReservation> reservations = new List<TimerReservation>();

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
        if (SubmarineInGameManager.instance.IsPausing || _isPausing)
            return;

        TimeSinceStart += Time.deltaTime;
        TimeSpan timeSpan = TimeSpan.FromSeconds(TimeSinceStart);
        string timerText = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        timeText.text = timerText;

        // 예약된 목록 중 시간이 된 액션을 실행
        for (int i = reservations.Count - 1; i >= 0; i--)
        {
            if (TimeSinceStart >= reservations[i].targetTime)
            {
                reservations[i].action?.Invoke();
                if (reservations[i].isPeriodic)
                {
                    reservations[i].targetTime += reservations[i].targetTime;
                }
                else
                {
                    reservations.RemoveAt(i); // 실행 후 삭제
                }
            }
        }
    }

    public void SetPause(bool isPausing)
    {
        _isPausing = isPausing;
    }

    public void ReserveEvent(float delay, Action callback, bool periodic)
    {
        reservations.Add(new TimerReservation
        {
            targetTime = TimeSinceStart + delay,
            action = callback,
            isPeriodic = periodic
        });
    }
}
