using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public PlayerInput playerInput;
    public PlayerCameraController playerCameraController;
    public PlayerMove playerMove;
    public PlayerInteractor playerInteractor;
    [SerializeField] private GameObject playerGeo;

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 플레이어 외형 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetPlayerGeoActive(bool isActive)
    {
        playerGeo.SetActive(isActive);
    }

    /// <summary>
    /// 플레이어 움직임(이동, 회전) 가능 여부 설정
    /// </summary>
    /// <param name="canMove">가능 여부</param>
    public void SetPlayerCanMove(bool canMove)
    {
        playerCameraController.enabled = canMove;
        playerMove.SetMoveable(canMove);
    }

    /// <summary>
    /// 카메라 컨트롤러 활성화 여부 설정
    /// </summary>
    /// <param name="isEnable">활성화 여부</param>
    public void SetCameraControllerEnable(bool isEnable)
    {
        playerCameraController.enabled = isEnable;
    }
}
