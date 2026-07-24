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
/// <para>- 실제 시간이 아니라 이 게임 시간에 제어받는 것: 심해 괴물, 플레이어 괴물화</para>
/// </summary>
public class GameTime : MonoBehaviour
{
    public static GameTime Instance { get; private set; }
    public float TimeSinceStart { get; private set; } // 게임 시작 이후로 흐른 시간

    [SerializeField] private TextMeshProUGUI timeText;

    private bool _isPausing = false;

    // 특정 시간에 실행될 액션들을 모아두는 리스트 (예약 명단)
    private List<TimerReservation> reservations = new List<TimerReservation>();

    public bool isBackroomPlaying = false; // 백룸 플레이 중 여부

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
        // Debug.Log(_isPausing + " / " + TimeSinceStart);
        if (GameManager.instance.IsPausing || _isPausing)
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

    /// <summary>
    /// 백룸에서 시간 정지 여부 설정(포커스에서도 시간 정지/해제를 하기 때문에 그거에서 풀리지 않도록 따로 함수 만들어서 해줌)
    /// </summary>
    /// <param name="isPause"></param>
    public void SetPauseInBackroom(bool isPause)
    {
        isBackroomPlaying = isPause;
        _isPausing = isPause;
    }

    /// <summary>
    /// 게임 시간 정지 여부 설정
    /// </summary>
    /// <param name="isPause">정지 여부</param>
    public void SetPause(bool isPause)
    {
        if (!isBackroomPlaying)
            _isPausing = isPause;
    }

    /// <summary>
    /// 게임 시간 설정
    /// </summary>
    /// <param name="playTime"></param>
    public void SetTime(float playTime)
    {
        TimeSinceStart = playTime;
    }

    /// <summary>
    /// 이벤트 예약
    /// </summary>
    /// <param name="delay"></param>
    /// <param name="callback"></param>
    /// <param name="periodic"></param>
    /// <returns></returns>
    public TimerReservation ReserveEvent(float delay, Action callback, bool periodic)
    {
        TimerReservation newReservation = new TimerReservation
        {
            targetTime = TimeSinceStart + delay,
            action = callback,
            isPeriodic = periodic
        };

        reservations.Add(newReservation);
        return newReservation; // 취소 가능하도록 예약 객체 반환
    }

    /// <summary>
    /// 이벤트 취소
    /// </summary>
    /// <param name="reservation">예약 객체</param>
    public void CancelEvent(TimerReservation reservation)
    {
        if (reservation != null && reservations.Contains(reservation))
        {
            reservations.Remove(reservation);
        }
    }
}
