using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Tiến độ các điều kiện thắng, cho viên mục tiêu dưới thanh sao.
    public readonly struct GoalProgress
    {
        private GoalProgress(int filledCells, int cellsToFill, int boxesDone, int boxCount)
        {
            FilledCells = filledCells;
            CellsToFill = cellsToFill;
            BoxesDone = boxesDone;
            BoxCount = boxCount;
        }

        // Ô đã có mèo (virus không tính), trên tổng số ô phải lấp.
        public int FilledCells { get; }

        public int CellsToFill { get; }

        // Hộp đã có mèo đúng màu, trên tổng số hộp (0 nếu level không có hộp).
        public int BoxesDone { get; }

        public int BoxCount { get; }

        public static GoalProgress Of(Board board)
        {
            var level = board.Level;
            int filled = 0, boxesDone = 0, boxCount = 0;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                var piece = board[cell];
                if (level.IsSlot(cell) && !level.IsToxic(cell) && piece.HasValue && !piece.Value.IsVirus) filled++;

                var target = level.TargetAt(cell);
                if (!target.HasValue) continue;
                boxCount++;
                if (piece.HasValue && piece.Value.Species == target.Value) boxesDone++;
            }
            return new GoalProgress(filled, level.CellsToFill, boxesDone, boxCount);
        }
    }
}
