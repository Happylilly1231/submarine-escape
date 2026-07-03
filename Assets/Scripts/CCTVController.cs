using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class CCTVController : PuzzleController
{
    protected override bool IsHoverRequired => false;
    protected override bool IsMouseRequiredAtFirst => true;

    [Header("비디오 플레이어")]
    public VideoPlayer[] videoPlayers = new VideoPlayer[4];
    public RenderTexture[] videoScreens = new RenderTexture[4];

    [SerializeField] private GameObject interactUI;

    // 영상이 한 번이라도 끝까지 재생되었는지 플래그
    private bool isVideoFinished = false;

    public override void Start()
    {
        base.Start();

        PreLoadVideos();
    }

    /// <summary>
    /// 4개의 CCTV 영상을 미리 로딩하는 함수
    /// </summary>
    private void PreLoadVideos()
    {
        foreach (var player in videoPlayers)
        {
            if (player != null)
            {
                player.playOnAwake = false; // 자동 재생 방지
                player.time = 0; // 시작 시간 초기화
                player.Prepare(); // 비디오 준비(로딩)
            }
        }
        Debug.Log("4개의 cctv 영상을 미리 로딩");
    }

    #region 퍼즐 시작/종료
    public override void ActivatePuzzle()
    {
        interactUI.SetActive(false);

        base.ActivatePuzzle();
    }

    public override void StartPuzzle()
    {
        base.StartPuzzle();

        // 4개의 CCTV 영상 동시 재생
        foreach (var player in videoPlayers)
        {
            if (player != null)
            {
                player.Play();
            }
        }
    }

    public override void ExitPuzzle()
    {
        base.ExitPuzzle();

        interactUI.SetActive(true);

        // 4개의 CCTV 영상 동시 정지
        foreach (var player in videoPlayers)
        {
            if (player != null && player.isPlaying)
            {
                player.Pause();
            }
        }
    }
    #endregion
}
