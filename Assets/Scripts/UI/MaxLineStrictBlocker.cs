using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MaxLineStrictBlocker : MonoBehaviour
{
    private TMP_InputField _inputField;
    private const int MAX_LINE_COUNT = 15; // 정해진 최대 줄 수

    void Start()
    {
        _inputField = GetComponent<TMP_InputField>();

        // 글자가 화면에 그려지기 직전에 줄 수를 검사하는 이벤트 연결
        _inputField.onValidateInput += ValidateInput;
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // 블록 지정/오버플로우 상황에서 charIndex가 문자열 길이를 벗어나지 않도록 안전하게 제한
        int safeIndex = Mathf.Clamp(charIndex, 0, text.Length);

        // 현재 텍스트에 유저가 누른 글자를 가상으로 합성
        string testText = text.Insert(safeIndex, addedChar.ToString());

        // 만약 유저가 누른 키가 엔터(줄바꿈)라면 -> TextMeshPro가 빈 줄을 인식할 수 있도록 가상 문자열 맨 뒤에 글자('X')를 강제로 더해줌
        if (addedChar == '\n' || addedChar == '\r')
        {
            testText += "X";
        }

        // 텍스트 메시 프로의 가상 계산 기능을 이용해, 이 글자가 추가되면 총 몇 줄이 될지 미리 계산
        TMP_TextInfo textInfo = _inputField.textComponent.GetTextInfo(testText);

        // 계산된 가상 줄 수가 제한(15줄)을 초과한다면
        if (textInfo.lineCount > MAX_LINE_COUNT)
        {
            // '\0'을 리턴하여 입력을 완전히 무시(차단)
            return '\0';
        }

        // 15줄 이내라면 정상 입력 허용
        return addedChar;
    }
}
