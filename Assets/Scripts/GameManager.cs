using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전반의 플레이와 관련된 변수, 함수 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 싱글톤 변수

    // 정지
    private bool mIsPausing = false; // 정지 중인지 여부
    public bool IsPausing { get => mIsPausing; set => mIsPausing = value; }

    /// <summary>
    /// 싱글톤 구현
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mIsPausing = false; // 정지 X
    }
}
