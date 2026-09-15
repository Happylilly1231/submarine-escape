using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PlayerItemStates
{
    public bool IsDivingsuitEquipped; // 잠수복 착용 여부
    public bool HasOxygenCapsule; // 산소 캡슐 장착 여부
    public bool HasThermalProtector; // 열 보호 장치 장착 여부
    public bool HasHelicopterLocator; // 헬리콥터 좌표 입력 손목시계 장착 여부
}

public class DeepSeaBridge : MonoBehaviour
{
    public static DeepSeaBridge Instance { get; private set; }
    public PlayerItemStates PlayerItemStates { get; private set; }
    public bool HasContactedHQ { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }


    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드되면 자동으로 호출되는 함수
    /// <para> - 심해 씬으로 전환 시 심해 씬 인트로 컷씬 실행 </para>
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "DeepSeaItemScene")
        {
            DeepSeaIntroCutScene.Instance.StartCutScene();
        }
    }

    /// <summary>
    /// 플레이어 아이템(잠수복, 산소 캡슐, 열 보호 장치, 헬리콥터 좌표 입력 손목시계) 설정
    /// </summary>
    /// <param name="data"></param>
    public void SetPlayerItemStates(PlayerItemStates data)
    {
        PlayerItemStates = data;
    }

    /// <summary>
    /// 플레이어가 본부와 통신을 했는지 설정
    /// </summary>
    /// <param name="contacted"></param>
    public void SetContactedHQ(bool contacted)
    {
        HasContactedHQ = contacted;
    }
}
