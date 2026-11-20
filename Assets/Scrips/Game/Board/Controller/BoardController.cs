using System;

public sealed class BoardController : IController
{
    private readonly BoardModel _boardModel;
    private readonly BenchModel _benchModel;
    private readonly EventBus _eventBus;

    private bool _initialized;
    private bool _disposed;

    internal BoardController(
        BoardModel boardModel,
        BenchModel benchModel,
        EventBus eventBus)
    {
        _boardModel = boardModel
            ?? throw new ArgumentNullException(nameof(boardModel));

        _benchModel = benchModel
            ?? throw new ArgumentNullException(nameof(benchModel));

        _eventBus = eventBus
            ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoardController));

        _initialized = true;
    }

    private void ThrowIfNotReady()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoardController));

        if (!_initialized)
            throw new InvalidOperationException(
                "BoardController 尚未初始化。");
    }

    public void Dispose()
    {
        _disposed = true;
        _initialized = false;
    }

    public BoardPlacementResult TryPlaceFromBench(
    int instanceId,
    int col,
    int row)
    {
        ThrowIfNotReady();

        // 先检查所有正常的操作失败条件。
        if (!_boardModel.IsValidCell(col, row))
            return BoardPlacementResult.InvalidCell;

        if (!_boardModel.IsPlayerCell(col, row))
            return BoardPlacementResult.NotPlayerCell;

        if (_boardModel.GetCell(col, row) != null)
            return BoardPlacementResult.CellOccupied;

        if (!_benchModel.TryFindSlot(instanceId, out int benchIndex))
            return BoardPlacementResult.UnitNotInBench;

        // 迁移原来的实例，保持棋子身份。
        var unit = _benchModel.GetSlot(benchIndex);

        _boardModel.Place(col, row, unit);
        _benchModel.Remove(benchIndex);

        // 两边数据都完成变更，再通知表现层。
        PublishChanges(benchIndex, col, row);

        return BoardPlacementResult.Success;
    }
    private void PublishChanges(int benchIndex, int col, int row)
    {
        try
        {
            _eventBus.Publish(GameEvents.BenchChanged, benchIndex);
        }
        catch (AggregateException error)
        {
            UnityEngine.Debug.LogException(error);
        }

        try
        {
            _eventBus.Publish(
                GameEvents.BoardChanged,
                new BoardCellChanged(col, row));
        }
        catch (AggregateException error)
        {
            UnityEngine.Debug.LogException(error);
        }
    }

    public int GetCellInstanceId(int col, int row)
    {
        ThrowIfNotReady();

        var unit = _boardModel.GetCell(col, row);
        return unit?.InstanceId ?? 0;
    }

    public int GetCellConfigId(int col, int row)
    {
        ThrowIfNotReady();

        var unit = _boardModel.GetCell(col, row);
        return unit?.ConfigId ?? 0;
    }

    public BoardPlacementResult TryPlaceUnit(
    int instanceId,
    int col,
    int row)
    {
        ThrowIfNotReady();

        if (!_boardModel.IsValidCell(col, row))
            return BoardPlacementResult.InvalidCell;

        if (!_boardModel.IsPlayerCell(col, row))
            return BoardPlacementResult.NotPlayerCell;

        if (_boardModel.GetCell(col, row) != null)
            return BoardPlacementResult.CellOccupied;

        // 不在棋盘上，就使用已有的备战席上阵流程。
        if (!_boardModel.TryFindCell(
            instanceId,
            out int fromCol,
            out int fromRow))
        {
            return TryPlaceFromBench(instanceId, col, row);
        }

        _boardModel.Move(fromCol, fromRow, col, row);

        // 两格数据修改完成，再通知表现层。
        PublishBoardChanged(fromCol, fromRow);
        PublishBoardChanged(col, row);

        return BoardPlacementResult.Success;
    }

    public BenchPlacementResult TryReturnToBench(int instanceId, int benchIndex)
    {
        ThrowIfNotReady();
        if (benchIndex < 0 || benchIndex >= BenchModel.Capacity)
            return BenchPlacementResult.InvalidSlot;

        if (!_boardModel.TryFindCell(instanceId, out int col, out int row))
            return BenchPlacementResult.UnitNotFound;

        if (!_boardModel.IsPlayerCell(col, row))
            return BenchPlacementResult.NotPlayerCell;

        if (_benchModel.TryFindSlot(instanceId, out _))
            return BenchPlacementResult.InvalidState;
    
        var source = _boardModel.GetCell(col, row);
        var target = _benchModel.GetSlot(benchIndex);

        if ((target != null && _boardModel.TryFindCell(target.InstanceId, out _ , out _)))
        {
            return BenchPlacementResult.InvalidState;
        }

        _boardModel.Remove(col, row);
        if (target != null)
        {
            _benchModel.Remove(benchIndex);
            _boardModel.Place(col, row, target);
        }

        _benchModel.Place(benchIndex, source);

        PublishChanges(benchIndex, col, row);

        return BenchPlacementResult.Success;
    }

    private void PublishBoardChanged(int col, int row)
    {
        try
        {
            _eventBus.Publish(
                GameEvents.BoardChanged,
                new BoardCellChanged(col, row));
        }
        catch (AggregateException error)
        {
            UnityEngine.Debug.LogException(error);
        }
    }
}