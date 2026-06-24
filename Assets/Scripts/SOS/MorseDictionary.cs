using System.Collections.Generic;

public static class MorseDictionary
{
    // 모스부호 -> 알파벳/숫자 변환 사전
    public static readonly Dictionary<string, string> MorseToText = new Dictionary<string, string>()
    {
        { "...ㅡㅡㅡ...", "SOS" },
        { ".....ㅡ...", "HAS" },
        { ".ㅡ..ㅡㅡ.ㅡ", "RPT" },
        { "..ㅡ...ㅡ.", "FIN" },
        { "......ㅡ..", "SIL" },
        { "ㅡ.ㅡㅡㅡㅡ..", "NOD" },
        { "ㅡㅡㅡㅡㅡㅡ.", "MON" }
    };
}