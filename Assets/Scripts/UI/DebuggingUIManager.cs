using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 디버깅할 때(꼭 본씬에서 시작해야 함(타이틀씬부터 시작하면 버튼에 할당되는 함수를 찾을 수 없어 오류남))만 사용할 디버깅 UI 매니저
/// </summary>
public class DebuggingUIManager : MonoBehaviour
{
    [SerializeField] private Door crewRoomDoor; // 선원실 문
    [SerializeField] private Button lightToggleButton; // 불 켜기/끄기 버튼
    [SerializeField] private Button alertButton; // 경보 발생 버튼
    [SerializeField] private Button unlockCrewRoomDoorButton; // 선원실 문 잠금 해제 버튼
    [SerializeField] private Button enterBackroomButton; // 백룸 진입 버튼
    [SerializeField] private Button escapeBackroomButton; // 백룸 탈출 버튼
    [SerializeField] private Button fillExtractorButton; // 추출기 피 채우기 버튼
    [SerializeField] private Button cureButton; // 치료 버튼

    private ObjectiveManager objectiveManager;
    private PowerSwitch powerSwitch;

    private void Start()
    {
        // // 인게임 매니저가 없을 때 이 컴포넌트 비활성화 (타이틀씬부터 시작했다는 얘기이므로(함수 찾을 수 없음))
        // if (SubmarineInGameManager.instance == null)
        // {
        //     enabled = false;
        //     return;
        // }

        // // 버튼 할당
        // lightToggleButton.onClick.AddListener(() => LightingManager.instance.LightToggle(!LightingManager.instance.IsPowerOn));
        // alertButton.onClick.AddListener(SubmarineInGameManager.instance.AlertOn);
        // unlockCrewRoomDoorButton.onClick.AddListener(UnlockCrewRoomDoor);
        // escapeBackroomButton.onClick.AddListener(EscapeBackroom);
        // enterBackroomButton.onClick.AddListener(EnterBackroom);
        // fillExtractorButton.onClick.AddListener(FillBioDataExtractor);
        // cureButton.onClick.AddListener(CureImmediately);
    }

    /// <summary>
    /// 디버깅 활성화/비활성화 (활성화 -> 버튼 연결 등 / 비활성화 -> 버튼 연결 초기화 등)
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveDebugging(bool isActive)
    {
        if (isActive)
        {
            // 버튼 함수 연결
            lightToggleButton.onClick.AddListener(() => powerSwitch.TogglePower(!LightingManager.instance.IsPowerOn));
            alertButton.onClick.AddListener(SubmarineInGameManager.instance.AlertOn);
            unlockCrewRoomDoorButton.onClick.AddListener(UnlockCrewRoomDoor);
            escapeBackroomButton.onClick.AddListener(EscapeBackroom);
            enterBackroomButton.onClick.AddListener(EnterBackroom);
            fillExtractorButton.onClick.AddListener(FillBioDataExtractor);
            cureButton.onClick.AddListener(CureImmediately);
        }
        else
        {
            lightToggleButton.onClick.RemoveAllListeners();
            alertButton.onClick.RemoveAllListeners();
            unlockCrewRoomDoorButton.onClick.RemoveAllListeners();
            escapeBackroomButton.onClick.RemoveAllListeners();
            enterBackroomButton.onClick.RemoveAllListeners();
            fillExtractorButton.onClick.RemoveAllListeners();
            cureButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 메인 씬의 브릿지가 문 오브젝트의 주소를 실시간으로 등록해줌
    /// </summary>
    public void RegisterCrewRoomDoor(Door door)
    {
        this.crewRoomDoor = door;
    }

    public void RegisterScript(PowerSwitch mainScenePowerSwitch, ObjectiveManager objectiveManager)
    {
        powerSwitch = mainScenePowerSwitch;
        this.objectiveManager = objectiveManager;
    }

    /// <summary>
    /// 선원실 문 잠금 해제
    /// </summary>
    public void UnlockCrewRoomDoor()
    {
        crewRoomDoor.isLocked = false;
        objectiveManager.CompleteObjective("EscapeCrewRoom");
        Debug.Log("[Debug] ✅ 성공 - 선원실 문 잠금 해제 완료");
    }

    public void EnterBackroom()
    {
        if (!BackroomManager.Instance.isPlayingBackroom)
        {
            if (BackroomManager.Instance.InnerMonsterSpinalCord.IsBioDataExtractorSelected())
            {
                MenuUIController.instance.ToggleMenu();
                BackroomManager.Instance.InnerMonsterSpinalCord.Interact(); // 괴물 척수 상호작용(주사기 꽂고 백룸 진입)
                Debug.Log("[Debug] ✅ 성공 - 백룸 진입");
            }
            else
            {
                Debug.Log("[Debug] ❌ 실패 - 생체 데이터 추출기를 들고 있지 않습니다!");
            }
        }
        else
            Debug.Log("[Debug] ❌ 실패 - 이미 백룸에 있습니다!");
    }

    /// <summary>
    /// 백룸 탈출
    /// </summary>
    public void EscapeBackroom()
    {
        if (BackroomManager.Instance.isPlayingBackroom)
        {
            MenuUIController.instance.ToggleMenu();
            BackroomManager.Instance.EscapeBackroom(SubmarineInGameManager.instance.playerMove);
            Debug.Log("[Debug] ✅ 성공 - 백룸 탈출");
        }
        else
            Debug.Log("[Debug] ❌ 실패 - 백룸 플레이 중이 아닙니다!");
    }

    /// <summary>
    /// 생체 데이터 추출기 피 채우기
    /// </summary>
    public void FillBioDataExtractor()
    {
        // 지금 들고 있는 오브젝트가 생체 데이터 추출기일 때만 -> 해당 추출기 피 채움
        if (SubmarineInGameManager.instance.ItemEquipController.HasItem && SubmarineInGameManager.instance.ItemEquipController.HeldItemObject.TryGetComponent(out BioDataExtractor bioDataExtractor))
        {
            bioDataExtractor.Fill();
        }
        else
        {
            Debug.Log("[Debug] ❌ 실패 - 생체 데이터 추출기 아이템을 들고 있지 않습니다!");
        }
    }

    /// <summary>
    /// 즉시 괴물화 치료
    /// </summary>
    public void CureImmediately()
    {
        objectiveManager.CompleteObjective("AdministerCure");
        PlayerMutation playerMutation = SubmarineInGameManager.instance.player.GetComponent<PlayerMutation>();
        if (!playerMutation.IsCured)
        {
            MenuUIController.instance.ToggleMenu();
            playerMutation.InjectSerum(true); // 성공 치료제 투여
            // playerMutation.Cure();
            Debug.Log("[Debug] ✅ 성공 - 치료 완료");
        }
        else
        {
            Debug.Log("[Debug] 이미 치료되었습니다!");
        }
    }
}
