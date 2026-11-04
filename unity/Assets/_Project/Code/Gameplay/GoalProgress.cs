using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Tiến độ các điều kiện thắng, cho dòng nhiệm vụ trên màn hình ("Lấp kín 3/9 · Hộp 1/2 · Vi khuẩn còn 2").
    public readonly struct GoalProgress
    {
        private GoalProgress(int filledCells, int cellsToFill, int boxesDone, int boxCount, int killRemaining)
        {
            FilledCells = filledCells;
            CellsToFill = cellsToFill;
            BoxesDone = boxesDone;
            BoxCount = boxCount;
            KillRemaining = killRemaining;
        }

        // Ô đã có mèo hoặc vi khuẩn (virus không tính), trên tổng số ô phải lấp.
        public int FilledCells { get; }

        public int CellsToFill { get; }

        // Hộp đã có mèo đúng màu, trên tổng số hộp (0 nếu level không có hộp).
        public int BoxesDone { get; }

        public int BoxCount { get; }

        // Số quân của loài phải diệt còn trên bàn (0 nếu level không có).
        public int KillRemaining { get; }

        public static GoalProgress Of(Board board)
        {
            var level = board.Level;
            int filled = 0, boxesDone = 0, boxCount = 0, killRemaining = 0;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                var piece = board[cell];
                if (level.IsSlot(cell) && !level.IsToxic(cell) && piece.HasValue && !piece.Value.IsVirus) filled++;
                if (piece.HasValue && piece.Value.Species == level.KillSpecies) killRemaining++;

                var target = level.TargetAt(cell);
                if (!target.HasValue) continue;
                boxCount++;
                if (piece.HasValue && piece.Value.Species == target.Value) boxesDone++;
            }
            return new GoalProgress(filled, level.CellsToFill, boxesDone, boxCount, killRemaining);
        }
    }
}
