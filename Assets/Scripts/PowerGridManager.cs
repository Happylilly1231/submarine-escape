using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PowerGridManager : MonoBehaviour
{
    public static PowerGridManager instance;

    [SerializeField] private GraphicRaycaster raycaster; // UI용 레이캐스터
    private ElectricalBox _electricalBox;
    private PlayerInput _playerInput; // 플레이어 입력 컴포넌트
    private InputAction _powerPuzzleAction;
    private PointerEventData _pointerData;
    private WireTile _selectedTile; // 드래그 시작 타일
    private WireTile _lastTile; // 드래그 중 직전 타일
    private WireColor _activeColor; // 현재 긋고 있는 전선의 색상
    private bool _isDragging = false; // 현재 드래그 중인지 확인
    private List<WireTile> _currentPathTiles = new List<WireTile>(); // 현재 드래그 경로 저장
    private List<Vector2Int> _pathEntryDirections = new List<Vector2Int>(); // 들어온 방향 저장
    private HashSet<WireColor> _completedColors = new HashSet<WireColor>(); // 완성된 색상 저장

    void Awake()
    {
        instance = this;

        _electricalBox = FindObjectOfType<ElectricalBox>();
        _playerInput = SubmarineInGameManager.instance.player.GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            _powerPuzzleAction = _playerInput.actions["PowerPuzzle"];
            _powerPuzzleAction.started += OnPowerPuzzle;
            _powerPuzzleAction.canceled += OnPowerPuzzle;
        }
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        // Grid_Container 하위의 모든 WireTile을 가져와서 이미지 설정
        WireTile[] allTiles = GetComponentsInChildren<WireTile>();

        WireTile[] finalOrder = new WireTile[allTiles.Length];

        List<WireTile> movableTiles = new List<WireTile>();
        List<int> movableTargetIndices = new List<int>();

        for (int i = 0; i < allTiles.Length; i++)
        {
            allTiles[i].ApplyData(); // 데이터 적용 (전선 모양 켜기)

            if (allTiles[i].Data.IsFixed)
            {
                finalOrder[i] = allTiles[i];
            }
            else
            {
                movableTiles.Add(allTiles[i]);
                movableTargetIndices.Add(i);
            }
        }

        // 인덱스 리스트를 무작위로 섞기
        for (int i = 0; i < movableTargetIndices.Count; i++)
        {
            int randomIndex = Random.Range(i, movableTargetIndices.Count);
            int temp = movableTargetIndices[randomIndex];
            movableTargetIndices[randomIndex] = movableTargetIndices[i];
            movableTargetIndices[i] = temp;
        }

        // 섞인 인덱스를 타일들에 다시 할당
        for (int i = 0; i < movableTiles.Count; i++)
        {
            int targetIdx = movableTargetIndices[i];
            finalOrder[targetIdx] = movableTiles[i];
        }

        for (int i = 0; i < finalOrder.Length; i++)
        {
            if (finalOrder[i] != null)
            {
                finalOrder[i].transform.SetAsLastSibling();
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void OnPowerPuzzle(InputAction.CallbackContext context)
    {
        if (context.started) // 마우스를 누른 순간
        {
            _selectedTile = GetTileUnderPointer();
            if (_selectedTile != null && _selectedTile.Data.IsFixed && _selectedTile.Data.WireType == WireType.I)
            {
                _activeColor = _selectedTile.Data.WireColor;
                if (_completedColors.Contains(_activeColor))
                {
                    _completedColors.Remove(_activeColor);
                    ResetCurrentPathColor();
                }
                _lastTile = _selectedTile;
                _isDragging = true;

                // 경로 리스트 초기화 및 시작 타일 추가
                _currentPathTiles.Clear();
                _pathEntryDirections.Clear();
                _currentPathTiles.Add(_selectedTile);
                _pathEntryDirections.Add(Vector2Int.zero);
            }
        }
        else if (context.canceled) // 마우스를 뗀 순간
        {
            if (_isDragging)
            {
                if (_lastTile != null && _lastTile != _selectedTile &&
                _lastTile.Data.IsFixed && _lastTile.Data.WireColor == _activeColor)
                {
                    _completedColors.Add(_activeColor);
                    Debug.Log($"<color=cyan>{_activeColor} 전선 연결 완성!</color>");
                    Debug.Log(_completedColors.Count);
                    if (_completedColors.Count == 4)
                    {
                        _electricalBox.ForceExitMode(true);
                    }
                }
                else
                {
                    Debug.Log("<color=red>연결 실패: 초기화합니다.</color>");
                    ResetCurrentPathColor();
                }
            }
            else // 드래그가 아니었을 때만 스왑 시도
            {
                WireTile targetTile = GetTileUnderPointer();
                if (_selectedTile != null && targetTile != null)
                {
                    TrySwap(_selectedTile, targetTile);
                }
            }

            _isDragging = false;
            _lastTile = null;
            _selectedTile = null;
            _currentPathTiles.Clear(); // 경로 초기화
        }
    }

    void Update()
    {
        // 드래그 중일 때만 매 프레임 마우스 아래의 타일을 검사
        if (_isDragging && _lastTile != null)
        {
            WireTile currentTile = GetTileUnderPointer();

            if (currentTile != null)
            {
                // 마우스가 경로 리스트의 바로 이전 타일로 되돌아갔을 때
                if (_currentPathTiles.Count > 1 && currentTile == _currentPathTiles[_currentPathTiles.Count - 2])
                {
                    UndoLastConnection();
                }
                // 마우스가 마지막 타일과 다른 새로운 타일에 닿았을 때
                else if (currentTile != _lastTile && !_currentPathTiles.Contains(currentTile))
                {
                    ProcessConnection(_lastTile, currentTile);
                }
            }
        }
    }

    private void ResetCurrentPathColor()
    {
        WireTile[] allTiles = GetComponentsInChildren<WireTile>();
        foreach (WireTile tile in allTiles)
        {
            // 시작/끝 고정 타일의 자기 색상은 유지
            if (tile.Data.IsFixed && tile.Data.WireColor == _activeColor)
            {
                tile.UpdateVisuals();
                continue;
            }
            tile.RemoveColor(_activeColor);
        }
    }

    /// <summary>
    /// 마지막 연결을 취소하는 함수
    /// </summary>
    private void UndoLastConnection()
    {
        if (_currentPathTiles.Count <= 1) return;

        WireTile tileToRemove = _currentPathTiles[_currentPathTiles.Count - 1];
        WireTile targetTile = _currentPathTiles[_currentPathTiles.Count - 2];

        // 현재 타일(tileToRemove)은 경로에서 빠지므로 해당 색상 완전 제거
        tileToRemove.RemoveColor(_activeColor);

        // 되돌아갈 타일(targetTile)에서 방금 나갔던 방향의 색상만 제거
        Vector2Int entryDirOfRemoved = _pathEntryDirections[_pathEntryDirections.Count - 1];
        Vector2Int exitDirFromTarget = entryDirOfRemoved * -1;
        targetTile.RemoveDirectionColor(exitDirFromTarget, _activeColor);

        // 리스트 데이터 갱신
        _currentPathTiles.RemoveAt(_currentPathTiles.Count - 1);
        _pathEntryDirections.RemoveAt(_pathEntryDirections.Count - 1);
        _lastTile = targetTile;

        // 타겟 타일 비주얼 강제 업데이트 (남아있는 입구 방향 등 다시 그리기)
        _lastTile.UpdateVisuals();
    }

    private void ProcessConnection(WireTile from, WireTile to)
    {
        if (IsAdjacent(from.transform.GetSiblingIndex(), to.transform.GetSiblingIndex()))
        {
            Vector2Int outDir = GetDirection(from, to);
            Vector2Int inDir = outDir * -1;

            if (from.Data.OpenDirections.Contains(outDir) && to.Data.OpenDirections.Contains(inDir))
            {
                // 이전 타일에서 나가는 방향 칠하기
                from.SetDirectionColor(outDir, _activeColor);

                // 새 타일로 들어오는 방향 칠하기
                to.SetDirectionColor(inDir, _activeColor);

                _lastTile = to;
                _currentPathTiles.Add(to);
                _pathEntryDirections.Add(inDir);
            }
        }
    }

    private WireTile GetTileUnderPointer()
    {
        // 현재 마우스/터치 위치 가져오기
        Vector2 pointerPos = Mouse.current.position.ReadValue();

        // UI 레이캐스트 실행
        _pointerData = new PointerEventData(EventSystem.current) { position = pointerPos };
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(_pointerData, results);

        if (results.Count > 0)
        {
            WireTile tile = results[0].gameObject.GetComponentInParent<WireTile>();
            return tile;
        }
        return null;
    }

    public void TrySwap(WireTile start, WireTile target)
    {
        // 조건 검사 (둘 다 존재, 시작점은 전선이 있고, 타겟은 None이며 고정되지 않음)
        if (start == target || target.Data.IsFixed || target.Data.WireType != WireType.None) return;
        if (start.Data.IsFixed || start.Data.WireType == WireType.None) return;

        // 인접성 체크 후 Swap 진행
        if (IsAdjacent(start.transform.GetSiblingIndex(), target.transform.GetSiblingIndex()))
        {
            PerformSwap(start, target);
        }
    }

    /// <summary>
    /// 두 타일 간의 방향 벡터를 구하는 함수
    /// </summary>
    private Vector2Int GetDirection(WireTile from, WireTile to)
    {
        int idx1 = from.transform.GetSiblingIndex();
        int idx2 = to.transform.GetSiblingIndex();

        int row1 = idx1 / 10, col1 = idx1 % 10;
        int row2 = idx2 / 10, col2 = idx2 % 10;

        return new Vector2Int(col2 - col1, row1 - row2);
    }

    /// <summary>
    /// 인접한 칸인지 계산 (상하좌우)
    /// </summary>
    private bool IsAdjacent(int idx1, int idx2)
    {
        int row1 = idx1 / 10, col1 = idx1 % 10;
        int row2 = idx2 / 10, col2 = idx2 % 10;
        return Mathf.Abs(row1 - row2) + Mathf.Abs(col1 - col2) == 1;
    }

    private void PerformSwap(WireTile a, WireTile b)
    {
        // 두 타일에 걸려있는 모든 전선 경로 파괴
        ClearPathsForTile(a);
        ClearPathsForTile(b);

        // 하이라키 상의 위치(SiblingIndex) 교체 (GridLayoutGroup 대응)
        int indexA = a.transform.GetSiblingIndex();
        int indexB = b.transform.GetSiblingIndex();

        // 애니메이션 연출 (부드러운 이동을 위해 SiblingIndex 교체 전 위치 기억)
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        a.transform.DOMove(posB, 0.05f);
        b.transform.DOMove(posA, 0.05f).OnComplete(() =>
        {
            // 실제 데이터 순서 변경
            a.transform.SetSiblingIndex(indexB);
            b.transform.SetSiblingIndex(indexA);

            // 레이아웃 즉시 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        });
    }

    private void ClearPathsForTile(WireTile tile)
    {
        // 타일이 현재 가지고 있는 활성 색상 리스트 복사
        List<WireColor> colorsToRemove = new List<WireColor>(tile.ActiveColors);

        foreach (WireColor color in colorsToRemove)
        {
            // 해당 색상이 완성 목록에 있었다면 제거
            if (_completedColors.Contains(color))
            {
                _completedColors.Remove(color);
            }

            // 2. 판 전체에서 해당 색상 경로 삭제
            ResetSpecificColorPath(color);
        }
    }

    private void ResetSpecificColorPath(WireColor color)
    {
        WireTile[] allTiles = GetComponentsInChildren<WireTile>();
        foreach (WireTile tile in allTiles)
        {
            tile.RemoveColor(color);
        }
    }
}
