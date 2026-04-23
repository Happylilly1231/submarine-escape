using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WireType { I, L, T, Cross, None }
public enum WireColor { Purple, Green, Red, Pink, Orange, Blue, Black, None }

[CreateAssetMenu(fileName = "New WireTile", menuName = "New WireTile/WireTile")]
public class WireData : ScriptableObject
{
    [Header("전선타일 기본 정보")]
    [SerializeField] private bool isFixed;
    [SerializeField] private Sprite wireImage;
    [SerializeField] private WireType wireType;
    [SerializeField] private List<Vector2Int> openDirections; // 연결구 방향 (위, 아래, 왼쪽, 오른쪽)
    [SerializeField] private WireColor wireColor;

    // 읽기 전용 접근
    public bool IsFixed => isFixed;
    public Sprite WireImage => wireImage;
    public WireType WireType => wireType;
    public List<Vector2Int> OpenDirections => openDirections;
    public WireColor WireColor => wireColor;
}
