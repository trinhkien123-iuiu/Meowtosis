using System.Collections.Generic;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Các ô một nước đi chạm tới, để hướng dẫn làm tối phần còn lại của bàn: ô mèo được vuốt, đường bản sao đi qua
    // (cả đoạn trượt trên băng), và mọi ô đổi khác trong lượt (dãy bị đẩy, quân bị thay, bộ 3 bị khử, ô độc, virus lan).
    public static class MoveFocus
    {
        public static IReadOnlyList<int> CellsOf(MoveResult move, Level level)
        {
            var cells = new List<int> { move.SourceCell };
            if (move.SwipeDirection.HasValue)
            {
                for (var cell = level.Step(move.SourceCell, move.SwipeDirection.Value);
                     cell != Level.NoCell && cell != move.PlacedCell;
                     cell = level.Step(cell, move.SwipeDirection.Value))
                {
                    cells.Add(cell);
                }
            }
            for (var cell = 0; cell < move.Before.CellCount; cell++)
            {
                if (cells.Contains(cell)) continue;
                var changed = move.Before[cell]?.Id != move.Placed[cell]?.Id || move.Placed[cell]?.Id != move.After[cell]?.Id;
                if (changed) cells.Add(cell);
            }
            return cells;
        }
    }
}
