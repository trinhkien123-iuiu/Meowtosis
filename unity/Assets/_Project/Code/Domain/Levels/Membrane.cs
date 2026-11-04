namespace Meowtosis.Domain
{
    // Màng một chiều trên cạnh giữa một ô và ô kề theo Direction.
    // Quân chỉ đi qua được theo hướng đó; virus cũng bị chặn như vậy. Màng không chặn việc đánh thức.
    public readonly struct Membrane
    {
        public Membrane(int row, int column, Direction direction)
        {
            Row = row;
            Column = column;
            Direction = direction;
        }

        public int Row { get; }

        public int Column { get; }

        public Direction Direction { get; }
    }
}
