using System;

namespace Meowtosis.Domain
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
    }

    public static class Directions
    {
        // Cùng thứ tự với prototype, để bộ giải thử nước theo cùng thứ tự và ra cùng lời giải.
        public static readonly Direction[] All = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        public static Direction Opposite(this Direction direction) => direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => Direction.Left,
        };

        public static int RowOffset(this Direction direction) => direction switch
        {
            Direction.Up => -1,
            Direction.Down => 1,
            _ => 0,
        };

        public static int ColumnOffset(this Direction direction) => direction switch
        {
            Direction.Left => -1,
            Direction.Right => 1,
            _ => 0,
        };

        // Từ dùng trong file level: "up", "down", "left" hoặc "right".
        public static string ToWord(this Direction direction) => direction.ToString().ToLowerInvariant();

        public static Direction Parse(string word) => word switch
        {
            "up" => Direction.Up,
            "down" => Direction.Down,
            "left" => Direction.Left,
            "right" => Direction.Right,
            _ => throw new FormatException($"Không rõ hướng '{word}'"),
        };
    }
}
