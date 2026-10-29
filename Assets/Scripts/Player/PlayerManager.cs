using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어와 관련된 변수, 함수 관리 (여기서 참조해서 편하게 사용할 수 있도록)
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("통용")]
    public PlayerInput playerInput;
    [SerializeField] private GameObject playerGeo;
    public PlayerAbility playerAbility;
    public PlayerStat playerStat;
    public PlayerMoveBase playerMoveBase;
    public PlayerCameraControllerBase playerCameraControllerBase;

    // 잠수함씬(본씬) 전용
    public PlayerMove playerMove { get; private set; }
    public PlayerCameraController playerCameraController { get; private set; }
    public PlayerInteractor playerInteractor;

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            playerMove = playerMoveBase as PlayerMove;
            playerCameraController = playerCameraControllerBase as PlayerCameraController;
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
    public void SetPlayerCanMoveAndCameraEnable(bool canMove)
    {
        playerMoveBase.SetMoveable(canMove);
        SetCameraControllerEnable(canMove);
    }

    /// <summary>
    /// 카메라 컨트롤러 활성화 여부 설정
    /// </summary>
    /// <param name="isEnable">활성화 여부</param>
    public void SetCameraControllerEnable(bool isEnable)
    {
        playerCameraControllerBase.enabled = isEnable;
    }

    /// <summary>
    /// 플레이어가 괴물로 변했는지 체크 함수
    /// </summary>
    /// <returns></returns>
    public bool CheckMonsterTransformation()
    {
        return GameManager.instance.IsAbilityActive && playerAbility != null && playerAbility.IsTransformed; // 먼저 능력 사용 가능 여부를 보고 그 다음에 PlayerAbility 스크립트의 isTransformed 변수를 확인 (능력 사용 불가면 PlayerAbility 스크립트가 꺼져있기 때문)
    }
}
