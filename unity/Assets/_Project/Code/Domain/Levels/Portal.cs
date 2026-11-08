namespace Meowtosis.Domain
{
    // Cặp cổng dịch chuyển: đi ra khỏi ô (Row, Column) theo Direction thì vào ô (TargetRow, TargetColumn),
    // và đi ra khỏi ô đó theo hướng ngược lại thì vào ô thứ nhất. Hướng đi giữ nguyên sau khi qua cổng.
    // Cạnh có cổng phải nhìn ra ngoài bàn (mép hoặc lỗ). Cổng không chiếm ô; khử 3 và đánh thức vẫn chỉ tính ô kề thật.
    public readonly struct Portal
    {
        public Portal(int row, int column, Direction direction, int targetRow, int targetColumn)
        {
            Row = row;
            Column = column;
            Direction = direction;
            TargetRow = targetRow;
            TargetColumn = targetColumn;
        }

        public int Row { get; }

        public int Column { get; }

        public Direction Direction { get; }

        public int TargetRow { get; }

        public int TargetColumn { get; }
    }
}
