public readonly struct BoardCellChanged
{
    public int Col {get; }
    public int Row { get; }

    public BoardCellChanged(int col, int row)
    {
        Col = col;
        Row = row;
    }
}