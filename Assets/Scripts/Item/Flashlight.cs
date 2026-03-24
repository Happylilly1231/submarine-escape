using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private Light flashlightLight; // 손전등의 라이트 컴포넌트
    private CrewRoom1KeyPad _crewRoom1KeyPad;
    private CrewRoom1KeyPadPanel _crewRoom1KeyPadPanel;

    void Awake()
    {
        _crewRoom1KeyPad = FindObjectOfType<CrewRoom1KeyPad>();
        _crewRoom1KeyPadPanel = FindObjectOfType<CrewRoom1KeyPadPanel>();
    }

    public void Use()
    {
        if ((_crewRoom1KeyPad != null && _crewRoom1KeyPad.IsFocused) || (_crewRoom1KeyPadPanel != null && _crewRoom1KeyPadPanel.IsFocused)) return;
        flashlightLight.enabled = !flashlightLight.enabled;
    }
}
