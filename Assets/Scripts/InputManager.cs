using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // 맵 상태와 개별 액션 상태를 모두 저장하기 위한 딕셔너리들
    private Dictionary<string, bool> actionMapStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> individualActionStates = new Dictionary<string, bool>();

    // 싱글톤 변수
    public static InputManager instance;

    /// <summary>
    /// 싱글톤 구현 (실행 순서 빠르게 설정 안했으므로 Start부터 찾아야 함)
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

    /// <summary>
    /// 모든 입력 비활성화
    /// </summary>
    public void DisableAllInputs()
    {
        PlayerManager.Instance.playerInput.currentActionMap.Disable();
        PlayerManager.Instance.playerInput.actions.FindActionMap("Permanent")?.Disable();
    }

    /// <summary>
    /// 1. 모든 맵과 그 안의 개별 액션 상태까지 전부 기록하고 끕니다.
    /// </summary>
    public void SaveAndDisableAllInputs()
    {
        actionMapStates.Clear();
        individualActionStates.Clear();

        foreach (var map in PlayerManager.Instance.playerInput.actions.actionMaps)
        {
            // 맵 상태 저장
            actionMapStates[map.name] = map.enabled;

            // 🌟 맵 안의 개별 액션들의 켜짐/꺼짐 상태까지 낱낱이 기록
            foreach (var action in map.actions)
            {
                // 액션의 고유한 경로 정보(예: "Player/Attack")를 키값으로 사용
                string actionKey = $"{map.name}/{action.name}";
                individualActionStates[actionKey] = action.enabled;
            }

            // 기록이 끝난 후 안전하게 꺼줌
            map.Disable();
        }
    }

    /// <summary>
    /// 2. 저장해 두었던 맵과 개별 액션 상태를 원래대로 실시간 조율하며 복구합니다.
    /// </summary>
    public void RestoreInputsFromSnapshot()
    {
        foreach (var map in PlayerManager.Instance.playerInput.actions.actionMaps)
        {
            if (actionMapStates.TryGetValue(map.name, out bool wasMapEnabled))
            {
                if (wasMapEnabled)
                {
                    // 우선 맵을 켭니다 (이때 내부 액션들이 일단 다 켜짐)
                    map.Enable();

                    // 🌟 그 후, 원래 꺼져 있어야 했던 개별 액션들만 콕 집어서 다시 꺼줍니다!
                    foreach (var action in map.actions)
                    {
                        string actionKey = $"{map.name}/{action.name}";
                        if (individualActionStates.TryGetValue(actionKey, out bool wasActionEnabled))
                        {
                            if (!wasActionEnabled)
                            {
                                action.Disable(); // 원래 꺼져 있던 개별 액션 차단
                            }
                        }
                    }
                }
                else
                {
                    // 원래 꺼져 있던 맵은 그냥 꺼진 상태 유지
                    map.Disable();
                }
            }
        }
    }

    /// <summary>
    /// 액션 맵 전환 (Permenant 액션 맵도 추가적으로 활성화)
    /// </summary>
    public void SwitchActionMapWithPermanent(string actionMapName)
    {
        PlayerManager.Instance.playerInput.SwitchCurrentActionMap(actionMapName);
        PlayerManager.Instance.playerInput.actions.FindActionMap("Permanent")?.Enable();
    }
}
