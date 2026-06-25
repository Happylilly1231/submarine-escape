using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private Light flashlightLight; // 손전등의 라이트 컴포넌트

    private void Update()
    {
        if (FocusManager.Instance.CurrentPuzzleController != null) flashlightLight.enabled = false;
    }

    public void Use()
    {
        Debug.Log("사용");
        flashlightLight.enabled = !flashlightLight.enabled;
    }
}
