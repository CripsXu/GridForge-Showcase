using System;

public sealed class BoardModel : IModel
{
    public const int ColumnCount = 7;
    public const int RowCount = 8;
    public const int PlayerRowCount = 4;

    private readonly UnitInstance[,] _cells = new UnitInstance[ColumnCount, RowCount];

    public void Initialize() {}

    public bool IsValidCell(int col, int row)
    {
        return col >= 0 && col < ColumnCount && 
        row >= 0 && row < RowCount;
    }

    public bool IsPlayerCell(int col, int row)
    {
        return IsValidCell(col, row) && row < PlayerRowCount;
    }

    public UnitInstance GetCell(int col, int row)
    {
        ValidateCell(col, row);
        return _cells[col, row];
    }

    internal void Place(int col, int row, UnitInstance unit)
    {
        ValidateCell(col, row);

        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        if (_cells[col, row] != null)
            throw new InvalidOperationException("目标格子已经有棋子。");

        foreach (var existing in _cells)
        {
            if (existing != null &&
                existing.InstanceId == unit.InstanceId)
            {
                throw new InvalidOperationException(
                    "同一个棋子不能占据多个格子。");
            }
        }

        _cells[col, row] = unit;
    }

    private void ValidateCell(int col, int row)
    {
        if (col < 0 || col >= ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(col));

        if (row < 0 || row >= RowCount)
            throw new ArgumentOutOfRangeException(nameof(row));
    }

    public bool TryFindCell(
    int instanceId,
    out int col,
    out int row)
    {
        col = -1;
        row = -1;

        if (instanceId <= 0)
            return false;

        for (int c = 0; c < ColumnCount; c++)
        {
            for (int r = 0; r < RowCount; r++)
            {
                var unit = _cells[c, r];

                if (unit == null || unit.InstanceId != instanceId)
                    continue;

                col = c;
                row = r;
                return true;
            }
        }

        return false;
    }

    internal void Move(
        int fromCol,
        int fromRow,
        int toCol,
        int toRow)
    {
        ValidateCell(fromCol, fromRow);
        ValidateCell(toCol, toRow);

        var unit = _cells[fromCol, fromRow];

        if (unit == null)
            throw new InvalidOperationException("来源格子没有棋子。");

        if (_cells[toCol, toRow] != null)
            throw new InvalidOperationException("目标格子已经有棋子。");

        // 完成所有检查后，再迁移原来的实例。
        _cells[fromCol, fromRow] = null;
        _cells[toCol, toRow] = unit;
    }

    internal UnitInstance Remove(int col, int row)
    {
        ValidateCell(col, row);

        var unit = _cells[col, row];
        
        if (unit == null)
            throw new InvalidOperationException("来源格子没有棋子。");

        _cells[col, row] = null;
        return unit;
    }


    public void Dispose()
    {
        Array.Clear(_cells, 0, _cells.Length);
    }
}