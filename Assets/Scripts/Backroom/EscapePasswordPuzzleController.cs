using System.Collections;
using System.Collections.Generic;
using NavKeypad;
using UnityEngine;
public class EscapePasswordPuzzleController : MonoBehaviour
{
    [SerializeField] private InteractableKeypad escapeDoorInteractableKeypad;
    [SerializeField] private Door escapeDoor; // 탈출 문 (비대 방의 중앙 방 문)
    [SerializeField] private Transform[] shelfTransforms;
    [SerializeField] private GameObject[] bigObjects; // 거대 오브젝트 배열

    private Keypad _keypad;

    private BigObjectType _currentBigObjectType;
    private int _currentGarbageCanCount = 0;
    private int _currentSelectedBigObjectCount = 0;

    private string _currentPassword = "";

    private void Awake()
    {
        _keypad = escapeDoorInteractableKeypad.GetComponent<Keypad>();
    }

    private void OnEnable()
    {
        _keypad.OnAccessGranted.AddListener(escapeDoorInteractableKeypad.UnLock);
        _keypad.OnAccessDenied.AddListener(escapeDoorInteractableKeypad.ExitPuzzle);
    }

    private void OnDisable()
    {
        _keypad.OnAccessGranted.RemoveListener(escapeDoorInteractableKeypad.UnLock);
        _keypad.OnAccessDenied.RemoveListener(escapeDoorInteractableKeypad.ExitPuzzle);
    }

    public void SetUpPuzzle()
    {
        int id;

        id = Random.Range(0, 2);
        for (int i = 0; i < bigObjects.Length; i++)
        {
            bigObjects[i].SetActive(i == id);
            if (i == id)
            {
                _currentBigObjectType = bigObjects[i].GetComponent<BigObjectData>().bigObjectType;
            }
        }

        _currentGarbageCanCount = 1;
        _currentSelectedBigObjectCount = 1;

        foreach (Transform shelfTransform in shelfTransforms)
        {
            id = Random.Range(0, 2);
            for (int i = 0; i < shelfTransform.childCount; i++)
            {
                shelfTransform.GetChild(i).gameObject.SetActive(i == id);
                if (i == id)
                {
                    ShelfData shelfData = shelfTransform.GetChild(i).GetComponent<ShelfData>();
                    _currentGarbageCanCount += shelfData.garbageCanCount;
                    switch (_currentBigObjectType)
                    {
                        case BigObjectType.Barrel:
                            _currentSelectedBigObjectCount += shelfData.barrelCount;
                            break;
                        case BigObjectType.SmallBox:
                            _currentSelectedBigObjectCount += shelfData.smallBoxCount;
                            break;
                        case BigObjectType.BigBox:
                            _currentSelectedBigObjectCount += shelfData.bigBoxCount;
                            break;
                    }
                }
            }
        }

        CalculatePassword(); // 비밀번호 계산
    }

    /// <summary>
    /// 비밀번호 계산
    /// </summary>
    public void CalculatePassword()
    {
        _currentPassword = (_currentGarbageCanCount * 100 + _currentSelectedBigObjectCount).ToString();
        _keypad.keypadCombo = _currentPassword;
        Debug.Log("현재 비밀번호: " + _currentPassword);
    }

    /// <summary>
    /// 퍼즐 초기화
    /// </summary>
    public void ResetPuzzle()
    {
        escapeDoorInteractableKeypad.ResetKeypad();
    }
}
