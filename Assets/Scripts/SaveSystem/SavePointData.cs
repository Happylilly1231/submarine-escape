using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SavePointDataCollection
{
    public List<SavePointData> savePointList = new List<SavePointData>();
}

[Serializable]
public class SavePointData
{
    public ESavePointType savePointType; // 세이브 기점
    public bool isUnlocked;              // 해당 기점 해금 여부
    public string saveDate;              // 갱신된 날짜
    public float playTime;              // 플레이 타임

    [Header("[ Player States ]")]
    public SerializableVector3 playerPosition;              // 플레이어 위치
    public PlayerStatsData playerStats;                     // 플레이어 스탯
    public List<PlayerInventorySlot> playerInventory = new List<PlayerInventorySlot>();   // 인벤토리
    public int mutationStage;                               // 괴물화 진행 단계
    public bool isCureInjected;                             // 치료제 투여 여부

    // [Header("[ Monster States ]")]
    // public SerializableVector3 insideMonsterPosition;  // 내부 괴물 위치
    // public SerializableVector3 outsideMonsterPosition; // 외부 괴물 위치
    // public bool isInsideMonsterBerserk;                // 내부 괴물 폭주 여부

    public List<WorldItemSaveData> worldItems = new List<WorldItemSaveData>(); // 아이템 오브젝트들

    [Header("[ Objective States ]")]
    public List<ObjectiveProgress> mainObjectives = new List<ObjectiveProgress>();
    public List<ObjectiveProgress> subObjectives = new List<ObjectiveProgress>();
}

/// <summary>
/// 플레이어 스텟 데이터
/// <para> - 플레이어 hp, stamina 관리 </para>
/// </summary>
[Serializable]
public class PlayerStatsData
{
    public float hp;
    public float stamina;
}

/// <summary>
/// 플레이어 인벤토리 슬롯 데이터
/// <para> - 해당 인벤토리 슬롯에 있는 아이템, 개수, 인스턴스 번호 관리 </para>
/// </summary>
[Serializable]
public class PlayerInventorySlot
{
    public string itemName;
    public int itemcnt;
    public int itemInstanceNum;
}

/// <summary>
/// 씬에 있는 아이템 데이터
/// </summary>
[Serializable]
public class WorldItemSaveData
{
    public string itemName;
    public string uniqueID;
    public bool isDropped;
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 scale;
}

[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

public enum ESavePointType
{
    CrewKeyPad, // 선원실 탈출
    PowerRestoration, // 전력 복구
    TorpedoLoaded, // 어뢰관 장전
    TorpedoFirstLaunch, // 어뢰 1차 발사
    TorpedoSecondLaunch, // 어뢰 2차 발사
    TorpedoThirdLaunch, // 어뢰 3차 발사
    CureInjected // 치료제 투여
}
