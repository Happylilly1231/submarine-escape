using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조명 관리
/// </summary>
public class LightingManager : MonoBehaviour
{
    [SerializeField] private Transform[] lightParents; // 조명 부모 객체들
    private Light[][] _lightsArray; // Light 이차원 배열(부모 객체 index, 해당 부모의 자식 간의 index)
    private Material[][] _bulbMaterialsArray; // 전구 머티리얼 이차원 배열

    public bool IsPowerOn { get; private set; } // 현재 전력 복구 여부(=불 켜져있는지 여부)

    public event Action<bool> OnLightChanged; // 조명 켜지거나 꺼질 때 이벤트

    // 싱글톤 변수
    public static LightingManager instance;

    /// <summary>
    /// 싱글톤 구현
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 이차원 배열 동적 할당
        _lightsArray = new Light[lightParents.Length][];
        _bulbMaterialsArray = new Material[lightParents.Length][];

        // Light & 전구 머티리얼 배열 초기화
        for (int i = 0; i < lightParents.Length; i++)
        {
            _lightsArray[i] = lightParents[i].GetComponentsInChildren<Light>();

            _bulbMaterialsArray[i] = new Material[_lightsArray[i].Length];
            for (int j = 0; j < _lightsArray[i].Length; j++)
            {
                Transform bulbTransform = _lightsArray[i][j].transform.parent.GetChild(0); // Light 컴포넌트 객체의 부모의 첫번째 자식이 전구임
                _bulbMaterialsArray[i][j] = bulbTransform.GetComponent<Renderer>().material;
            }
        }

        // 초기 설정
        LightToggle(false); // 조명 끄기
        Debug.Log("조명을 껐습니다.");
    }

    /// <summary>
    /// 조명 켜기/끄기
    /// </summary>
    /// <param name="isTurnOn">조명 켜는지 여부</param>
    public void LightToggle(bool isTurnOn)
    {
        IsPowerOn = isTurnOn;

        // 배열에 저장된 모든 Light 컴포넌트의 활성화 여부 설정
        for (int i = 0; i < _lightsArray.Length; i++)
        {
            for (int j = 0; j < _lightsArray[i].Length; j++)
            {
                _lightsArray[i][j].enabled = isTurnOn;
            }
        }

        Color currenBulbMaterialColor; // 현재 전구 머티리얼 색
        if (isTurnOn) // 조명이 켜지면
        {
            FXManager.instance.BloomOn(0.5f); // 매우 약한 발광 효과
            currenBulbMaterialColor = Color.white; // 현재 전구 머티리얼 색을 하얀색으로 변경 
        }
        else // 조명이 꺼지면
        {
            FXManager.instance.BloomOn(4f); // 강한 발광 효과
            currenBulbMaterialColor = Color.black; // 현재 전구 머티리얼 색을 검은색으로 변경
        }

        // 배열에 저장된 모든 전구 머티리얼 색을 현재 전구 머티리얼 색으로 변경
        for (int i = 0; i < _lightsArray.Length; i++)
        {
            for (int j = 0; j < _lightsArray[i].Length; j++)
            {
                _bulbMaterialsArray[i][j].color = currenBulbMaterialColor;
            }
        }

        // 조명 켜지거나 꺼질 때 이벤트 알림
        OnLightChanged?.Invoke(isTurnOn); // 인자는 켜진 여부
    }
}
