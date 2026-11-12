using System.Collections.Generic;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Các ô một nước đi chạm tới, để hướng dẫn làm tối phần còn lại của bàn: ô mèo được vuốt, đường bản sao đi qua
    // (cả đoạn trượt trên băng), và mọi ô đổi khác trong lượt (dãy bị đẩy, quân bị thay, bộ 3 bị khử, ô độc, virus lan).
    // Mỗi virus trên bàn cũng được chừa sáng cùng ô nó đang nhìn tới, để hướng dẫn chỉ được vào ánh mắt virus.
    // Mèo hai màu cũng được chừa sáng để người chơi thấy rõ bong bóng suy nghĩ và đếm lùi đổi màu.
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
            AddVirusGaze(move, level, cells);
            AddTwoColorCats(move, cells);
            return cells;
        }

        // Mèo hai màu: luôn chừa sáng để người chơi thấy bong bóng suy nghĩ và đếm lùi đổi màu
        private static void AddTwoColorCats(MoveResult move, List<int> cells)
        {
            for (var cell = 0; cell < move.Before.CellCount; cell++)
            {
                if (move.Before[cell]?.IsTwoColor == true || move.After[cell]?.IsTwoColor == true)
                {
                    if (!cells.Contains(cell)) cells.Add(cell);
                }
            }
        }

        // Virus và ô ngay trước mắt nó (ô nó sẽ lan tới ở lượt này), nếu còn trong bàn.
        private static void AddVirusGaze(MoveResult move, Level level, List<int> cells)
        {
            for (var cell = 0; cell < move.Before.CellCount; cell++)
            {
                if (!(move.Before[cell] is Piece piece) || !piece.IsVirus) continue;
                if (!cells.Contains(cell)) cells.Add(cell);
                var ahead = level.Step(cell, piece.Facing);
                if (ahead != Level.NoCell && !cells.Contains(ahead)) cells.Add(ahead);
            }
        }
    }
}
