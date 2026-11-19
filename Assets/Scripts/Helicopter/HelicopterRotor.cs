using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterRotor : MonoBehaviour
{
    [Header("Rotor Transforms")]
    [SerializeField] private Transform mainRotor; // 메인 프로펠러
    [SerializeField] private Transform tailRotor; // 꼬리 프로펠러

    [Header("Rotation Speed")]
    [SerializeField] private float mainRotorSpeed = 1200f; // 초당 회전 각도
    [SerializeField] private float tailRotorSpeed = 1800f;

    public bool IsMainRotorActive { get; set; } = false; // 메인 로터 활성화 여부

    private void Update()
    {
        if (!IsMainRotorActive) return;
        // 메인 로터: 로컬 Y축 기준 회전
        if (mainRotor != null)
        {
            mainRotor.Rotate(Vector3.up, mainRotorSpeed * Time.deltaTime, Space.Self);
        }

        // 테일 로터: 로컬 X축 기준 회전 (모델 축이 다르면 Vector3.forward 등으로 변경)
        if (tailRotor != null)
        {
            tailRotor.Rotate(Vector3.up, tailRotorSpeed * Time.deltaTime, Space.Self);
        }
    }
}
