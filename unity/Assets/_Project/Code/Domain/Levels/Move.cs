using System;
using System.Globalization;

namespace Meowtosis.Domain
{
    // Một nước vuốt ghi bằng hàng, cột và hướng. Hàng đếm từ trên, cột đếm từ trái, đều từ 0.
    public readonly struct Move
    {
        public Move(int row, int column, Direction direction)
        {
            Row = row;
            Column = column;
            Direction = direction;
        }

        public int Row { get; }

        public int Column { get; }

        public Direction Direction { get; }

        // Đọc "hàng cột hướng", ví dụ "0 2 right".
        public static Move Parse(string text)
        {
            var parts = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) throw new FormatException($"Cần 'hàng cột hướng' nhưng gặp '{text}'");
            var row = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var column = int.Parse(parts[1], CultureInfo.InvariantCulture);
            return new Move(row, column, Directions.Parse(parts[2]));
        }

        public override string ToString() => $"{Row} {Column} {Direction.ToWord()}";
    }
}
