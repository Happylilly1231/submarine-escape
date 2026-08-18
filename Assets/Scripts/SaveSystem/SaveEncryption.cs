using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 세이브 파일 암호화 및 변조 검증 담당
/// <para>AES 암호화 + HMAC-SHA256 무결성 검증 사용</para>
/// </summary>
public static class SaveEncryption
{
    // AES 암호화용 비밀키
    private const string EncryptionSecret =
        "A9xK2mP7qL4vR8sT1wY5zC3dF6gH0jN2";

    // HMAC 변조 검증용 비밀키
    private const string HmacSecret =
        "M3nB8vC1xZ5aS7dF9gH2jK4lP6qW0eR3";


    /// <summary>
    /// 문자열을 AES 암호화
    /// <para>매 저장마다 랜덤 IV 생성</para>
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("암호화할 데이터가 없습니다.");
        }

        byte[] key = GetEncryptionKey();

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;

            // AES 기본 Block Size = 128bit = 16byte
            aes.GenerateIV();

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream =
                           new CryptoStream(
                               memoryStream,
                               encryptor,
                               CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(
                            plainBytes,
                            0,
                            plainBytes.Length
                        );

                        cryptoStream.FlushFinalBlock();
                    }

                    byte[] encryptedBytes =
                        memoryStream.ToArray();

                    // IV + 암호화된 데이터 합치기
                    byte[] result =
                        new byte[aes.IV.Length + encryptedBytes.Length];

                    Buffer.BlockCopy(
                        aes.IV,
                        0,
                        result,
                        0,
                        aes.IV.Length
                    );

                    Buffer.BlockCopy(
                        encryptedBytes,
                        0,
                        result,
                        aes.IV.Length,
                        encryptedBytes.Length
                    );

                    return Convert.ToBase64String(result);
                }
            }
        }
    }


    /// <summary>
    /// AES 복호화
    /// <para>저장된 데이터에서 IV를 분리하여 복호화</para>
    /// </summary>
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("복호화할 데이터가 없습니다.");
        }

        byte[] fullData =
            Convert.FromBase64String(encryptedText);

        // 최소한 IV 16byte는 존재해야 함
        if (fullData.Length <= 16)
        {
            throw new Exception("잘못된 암호화 데이터입니다.");
        }

        byte[] key = GetEncryptionKey();

        byte[] iv = new byte[16];
        byte[] encryptedBytes =
            new byte[fullData.Length - 16];

        // IV와 암호화된 데이터 분리
        Buffer.BlockCopy(
            fullData,
            0,
            iv,
            0,
            iv.Length
        );

        Buffer.BlockCopy(
            fullData,
            iv.Length,
            encryptedBytes,
            0,
            encryptedBytes.Length
        );

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (ICryptoTransform decryptor =
                   aes.CreateDecryptor())
            {
                using (MemoryStream memoryStream =
                       new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cryptoStream =
                           new CryptoStream(
                               memoryStream,
                               decryptor,
                               CryptoStreamMode.Read))
                    {
                        using (StreamReader reader =
                               new StreamReader(cryptoStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// 데이터의 HMAC-SHA256 생성
    /// <para>파일 변조 여부 확인용</para>
    /// </summary>
    public static string CreateHMAC(string data)
    {
        byte[] hmacKey =
            GetHmacKey();

        using (HMACSHA256 hmac =
               new HMACSHA256(hmacKey))
        {
            byte[] dataBytes =
                Encoding.UTF8.GetBytes(data);

            byte[] hash =
                hmac.ComputeHash(dataBytes);

            return Convert.ToBase64String(hash);
        }
    }


    /// <summary>
    /// 저장된 HMAC과 현재 데이터의 HMAC 비교
    /// </summary>
    public static bool VerifyHMAC(
        string data,
        string savedHmac)
    {
        if (string.IsNullOrEmpty(data) ||
            string.IsNullOrEmpty(savedHmac))
        {
            return false;
        }

        string calculatedHmac =
            CreateHMAC(data);

        byte[] calculatedBytes =
            Convert.FromBase64String(calculatedHmac);

        byte[] savedBytes =
            Convert.FromBase64String(savedHmac);

        // FixedTimeEquals 사용 - 타이밍 공격 방지
        return CryptographicOperations.FixedTimeEquals(
            calculatedBytes,
            savedBytes
        );
    }


    /// <summary>
    /// AES-256용 정확히 32byte Key 생성
    /// </summary>
    private static byte[] GetEncryptionKey()
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] secretBytes =
                Encoding.UTF8.GetBytes(EncryptionSecret);

            return sha256.ComputeHash(secretBytes);
        }
    }


    /// <summary>
    /// HMAC-SHA256용 Key 생성
    /// </summary>
    private static byte[] GetHmacKey()
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] secretBytes =
                Encoding.UTF8.GetBytes(HmacSecret);

            return sha256.ComputeHash(secretBytes);
        }
    }
}