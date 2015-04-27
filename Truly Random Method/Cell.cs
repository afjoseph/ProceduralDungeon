public class Cell
{
    public enum CellType
    {
        NULL,
        WALL,
        FLOOR,
        DOOR
    };

    public CellType Type { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }

    public Cell(CellType type, int x, int y)
    {
        Type = type;
        X = x;
        Y = y;
    }

    public void ChangeType(CellType type)
    {
        Type = type;
    }

}