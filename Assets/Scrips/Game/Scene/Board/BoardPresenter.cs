using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BoardPresenter : MonoBehaviour
{
    // 当前棋盘六边形的逻辑半径。
    private const float CellRadius = 1f;
    [SerializeField] private Transform _gridRoot;
    private readonly Transform[,] _anchors = new Transform[BoardModel.ColumnCount, BoardModel.RowCount];
    private readonly UnitActor[,] _actors = new UnitActor[BoardModel.ColumnCount, BoardModel.RowCount];

    private BoardController _boardController;
    private EventBus _eventBus;

    private UnitActorManager _unitActorManager;
    private bool _initialized;

    private bool _unitVisible = true;

    public float CellSpacing => Mathf.Sqrt(3f) * CellRadius;

    public void Initialize(
        BoardController boardController,
        EventBus eventBus, UnitActorManager unitActorManager)
    {
        if (_initialized) return;
        if (_gridRoot == null)
            throw new InvalidOperationException("board root is not assigned.");
        _boardController = boardController ?? throw new ArgumentNullException(nameof(boardController));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _unitActorManager = unitActorManager ?? throw new ArgumentNullException(nameof(unitActorManager));

        InitializeAnchors();
        RenderAll();

        _eventBus.Subscribe(GameEvents.BoardChanged, OnBoardChanged);
        _initialized = true;
    }

    private void OnBoardChanged(BoardCellChanged change)
    {
        RenderCell(change.Col, change.Row);
    }

    private void RenderCell(int col, int row)
    {
        int instanceId =
            _boardController.GetCellInstanceId(col, row);

        var actor = _actors[col, row];

        // 数据为空，对应格子不显示棋子。
        if (instanceId == 0)
        {
            RemoveActor(col, row);
            return;
        }

        if (actor != null &&
          actor.InstanceId == instanceId &&
          actor.transform.parent == _anchors[col, row])
        {
            return;
        }

        RemoveActor(col, row);

        try
        {
            int configId = _boardController.GetCellConfigId(col, row);

            actor = _unitActorManager.GetOrCreate(instanceId, configId);

            if (actor)
            {
                actor.transform.SetParent(
                    _anchors[col, row], false);

                actor.transform.localPosition = Vector3.zero;
                actor.transform.localRotation = Quaternion.identity;
                actor.gameObject.SetActive(_unitVisible);

                _actors[col, row] = actor;
            }
        }
        catch
        {
            RemoveActor(col, row);
            throw;
        }
    }

    private void RenderAll()
    {
        for (int col = 0; col < BoardModel.ColumnCount; col++)
        {
            for (int row = 0; row < BoardModel.RowCount; row++)
            {
                RenderCell(col, row);
            }
        }
    }
    private void InitializeAnchors()
    {
        for (int col = 0; col < BoardModel.ColumnCount; col++)
        {
            for (int row = 0; row < BoardModel.RowCount; row++)
            {
                string cellName = $"Cell_{col}_{row}";
                var anchor = _gridRoot.Find(cellName);

                if (anchor == null)
                    throw new InvalidOperationException(
                        $"GridRoot 下找不到 {cellName}。");

                _anchors[col, row] = anchor;
            }
        }
    }

    public bool TryGetCell(
        Vector3 worldPosition,
        out int col,
        out int row
        )
    {
        col = -1;
        row = -1;

        if(!_initialized || _gridRoot == null)
            return false;

        //世界坐标转到棋盘局部空间，与锚点使用同一套坐标
        Vector3 localPosition = _gridRoot.InverseTransformPoint(worldPosition);

        float nearestDistance = float.PositiveInfinity;
        int nearestCol = -1;
        int nearestRow = -1;

        //找到最近的格子中心，只比较地面上XZ的距离
        for(int c = 0; c < BoardModel.ColumnCount; c++)
        {
            for(int r = 0; r < BoardModel.RowCount; r++)
            {
                Vector3 offset = localPosition - _anchors[c, r].localPosition;

                float distance = offset.x * offset.x + offset.z * offset.z;

                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestCol = c;
                nearestRow = r;
            }
        }

        //最近格心只是候选，还要确认落点在六边形内部
        Vector3 difference = localPosition - 
        _anchors[nearestCol, nearestRow].localPosition;

        float dx = Mathf.Abs(difference.x);
        float dz = Mathf.Abs(difference.z);
        float sqrt3 = Mathf.Sqrt(3f);

        const float epsilon = 0.0001f;

        if(dx > sqrt3 * CellRadius * 0.5f + epsilon ||
            dz > CellRadius + epsilon ||
            dx + sqrt3 * dz > sqrt3 * CellRadius + epsilon)
        {
            return false;
        }

        col = nearestCol;
        row = nearestRow;
        return true;
    }

    private void RemoveActor(int col, int row)
    {
        _actors[col, row] = null;
    }

    public Vector3 GetCellLocalPosition(int col, int row)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "BoardPresenter 尚未初始化。");

        if (col < 0 || col >= BoardModel.ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(col));

        if (row < 0 || row >= BoardModel.RowCount)
            throw new ArgumentOutOfRangeException(nameof(row));
        
        return _anchors[col, row].localPosition;
    }

    public void SetUnitsVisible(bool visible)
    {
        _unitVisible = visible;

        foreach (var actor in _actors)
        {
            if (actor != null)
            {
                actor.gameObject.SetActive(visible);
            }
        }
    }

    private void OnDestroy()
    {
        _eventBus?.UnSubscribe(
            GameEvents.BoardChanged,
            OnBoardChanged);

        for (int col = 0; col < BoardModel.ColumnCount; col++)
        {
            for (int row = 0; row < BoardModel.RowCount; row++)
            {
                RemoveActor(col, row);
            }
        }

        _initialized = false;
    }
}
