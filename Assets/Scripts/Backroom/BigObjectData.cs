using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BigObjectType { Barrel, SmallBox, BigBox }; // 거대 오브젝트 타입

public class BigObjectData : MonoBehaviour
{
    public BigObjectType bigObjectType;
}
