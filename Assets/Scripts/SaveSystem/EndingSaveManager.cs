using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

/// <summary>
/// 단일 엔딩의 기록 데이터
/// </summary>
[Serializable]
public class EndingRecord
{
    public EEndingType endingType;      // 엔딩 종류
    public bool isUnlocked;             // 해금 여부
    public float firstTime;             // 최초 달성 시간
    public float bestTime;              // 최단 달성 시간
}

/// <summary>
/// 전체 엔딩 저장 데이터
/// </summary>
[Serializable]
public class EndingSaveData
{
    public EEndingType latestEnding;
    public List<EndingRecord> records = new List<EndingRecord>();
}

/// <summary>
/// 엔딩 데이터 암호화 저장용 클래스
/// <para> encryptedData: AES 암호화된 데이터</para>
/// <para> hmac: HMAC-SHA256 해시</para>
/// </summary>
[Serializable]
public class EncryptedEndingSaveData
{
    public string encryptedData;
    public string hmac;
}

public class EndingSaveManager : MonoBehaviour
{
    private static string saveFilePath =>
        Path.Combine(Application.persistentDataPath, "EndingData.dat");
    private EndingSaveData currentSaveData;
    public static EndingSaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadEndingData();
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// 처음 실행 시 모든 엔딩을 미해금 상태로 기본 생성
    /// </summary>
    /// <returns></returns>
    private EndingSaveData CreateDefaultEndingData()
    {
        EndingSaveData newData = new EndingSaveData();

        foreach (EEndingType type in Enum.GetValues(typeof(EEndingType)))
        {
            newData.records.Add(new EndingRecord
            {
                endingType = type,
                isUnlocked = false,
                firstTime = 0f,
                bestTime = float.MaxValue
            });
        }

        currentSaveData = newData;
        SaveEndingData();
        return newData;
    }

    /// <summary>
    /// 엔딩 데이터 전체 불러오기 (파일이 없으면 새 데이터 생성)
    /// <para> HMAC 검증 후 AES 복호화 진행 </para>
    /// </summary>
    /// <returns>엔딩 데이터</returns>
    public EndingSaveData LoadEndingData()
    {
        if (!File.Exists(saveFilePath)) // 파일이 없으면 초기화된 데이터 반환
        {
            currentSaveData = CreateDefaultEndingData();
            return currentSaveData;
        }
        try
        {
            // 파일 읽기
            string encryptedJson = File.ReadAllText(saveFilePath);

            // JSON -> EncryptedEndingSaveData 변환
            EncryptedEndingSaveData encryptedData =
                JsonUtility.FromJson<EncryptedEndingSaveData>(encryptedJson);

            // 데이터 유효성 검사
            if (encryptedData == null || string.IsNullOrEmpty(encryptedData.encryptedData) || string.IsNullOrEmpty(encryptedData.hmac))
            {
                throw new Exception("엔딩 데이터 파일이 손상되었습니다.");
            }

            // HMAC 검증 - 데이터 변조 여부 확인
            bool isVaild = SaveEncryption.VerifyHMAC(encryptedData.encryptedData, encryptedData.hmac);
            if (!isVaild)
            {
                throw new Exception("엔딩 데이터가 변조되었습니다.");
            }

            // AES 복호화
            string decryptedJson = SaveEncryption.Decrypt(encryptedData.encryptedData);

            // JSON -> EndingSaveData 변환
            currentSaveData = JsonUtility.FromJson<EndingSaveData>(decryptedJson);
            if (currentSaveData == null)
            {
                throw new Exception("엔딩 데이터 변환에 실패했습니다.");
            }

            if (currentSaveData.records == null)
            {
                currentSaveData.records = new List<EndingRecord>();
            }

            ValidateMissingEndings(currentSaveData); // 개발 중 엔딩 타입 추가할 수 있으니

            return currentSaveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"엔딩 데이터 불러오기 실패: {e.Message}");

            // 데이터가 손상되었을 경우 백업 후 초기화
            BackupCorruptedSave();

            currentSaveData = CreateDefaultEndingData();
            return currentSaveData;
        }
    }

    /// <summary>
    /// 엔딩 데이터 파일에 저장
    /// </summary>
    public void SaveEndingData()
    {
        if (currentSaveData == null)
        {
            Debug.LogError("엔딩 데이터가 없습니다.");
            return;
        }

        try
        {
            // 엔딩 데이터 -> JSON 변환
            string json = JsonUtility.ToJson(currentSaveData, false);

            // AES 암호화 및 HMAC 생성
            string encryptedData = SaveEncryption.Encrypt(json);
            string hmac = SaveEncryption.CreateHMAC(encryptedData);

            // EncryptedEndingSaveData 객체 생성
            EncryptedEndingSaveData encryptedSaveData = new EncryptedEndingSaveData
            {
                encryptedData = encryptedData,
                hmac = hmac
            };

            // JSON으로 변환 후 파일 저장
            string saveJson = JsonUtility.ToJson(encryptedSaveData, false);
            File.WriteAllText(saveFilePath, saveJson);
            Debug.Log($"엔딩 데이터 저장 완료: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"엔딩 데이터 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 엔딩 달성 시 엔딩 데이터 저장
    /// </summary>
    /// <param name="ending"></param>
    public void SaveEnding(EEndingType ending)
    {
        if (currentSaveData == null) LoadEndingData();

        float currentTime = GameTime.Instance.TimeSinceStart;
        currentSaveData.latestEnding = ending; // 최신 달성 엔딩 갱신

        // 기존에 수집된 엔딩이 있는지 확인
        EndingRecord record = currentSaveData.records.Find(r => r.endingType == ending);

        // 엔딩 기록이 없으면 새로 생성, 있으면 갱신
        if (record == null)
        {
            // 새로운 엔딩 기록 생성
            record = new EndingRecord
            {
                endingType = ending,
                isUnlocked = false,
                firstTime = 0f,
                bestTime = float.MaxValue
            };
            currentSaveData.records.Add(record);
        }

        if (!record.isUnlocked) // 최초 해금
        {
            record.isUnlocked = true;
            record.firstTime = currentTime;
            record.bestTime = currentTime;
        }
        else // 이미 해금됨
        {
            // 최단 기록 갱신 여부 확인
            if (currentTime < record.bestTime)
            {
                record.bestTime = currentTime;
            }
        }

        // JSON 파일로 저장
        SaveEndingData();
        Debug.Log($"{ending} 엔딩이 수집되었습니다!");
    }

    /// <summary>
    /// 개발 중 엔딩 타입이 추가되었을 경우 세이브 데이터에 자동으로 반영
    /// </summary>
    private void ValidateMissingEndings(EndingSaveData data)
    {
        bool isUpdated = false;
        foreach (EEndingType type in Enum.GetValues(typeof(EEndingType)))
        {
            if (!data.records.Exists(r => r.endingType == type))
            {
                data.records.Add(new EndingRecord
                {
                    endingType = type,
                    isUnlocked = false,
                    firstTime = 0f,
                    bestTime = float.MaxValue
                });
                isUpdated = true;
            }
        }

        if (isUpdated) SaveEndingData();
    }

    private void BackupCorruptedSave()
    {
        try
        {
            if (!File.Exists(saveFilePath)) return;

            string backupFilePath =
                Path.Combine(
                    Application.persistentDataPath,
                    $"EndingData_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.dat");

            File.Copy(saveFilePath, backupFilePath, true);

            Debug.Log($"손상된 엔딩 데이터 백업 완료: {backupFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"손상된 엔딩 데이터 백업 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 가장 최근에 달성한 엔딩 조회
    /// </summary>
    /// <returns>가장 최근에 달성한 엔딩 타입</returns>
    public EEndingType GetLatestEndingType()
    {
        if (currentSaveData == null) LoadEndingData();
        return currentSaveData.latestEnding;
    }

    /// <summary>
    /// 특정한 엔딩 데이터 조회
    /// </summary>
    /// <param name="ending">특정 엔딩</param>
    /// <returns>해당 엔딩 데이터</returns>
    public EndingRecord GetEndingRecord(EEndingType ending)
    {
        if (currentSaveData == null) LoadEndingData();
        return currentSaveData.records.Find(r => r.endingType == ending);
    }
}
