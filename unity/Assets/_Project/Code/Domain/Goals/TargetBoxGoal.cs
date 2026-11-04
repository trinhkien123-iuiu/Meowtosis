namespace Meowtosis.Domain
{
    // Mọi hộp mục tiêu có mèo đúng màu hộp (mèo ngủ cũng tính).
    public sealed class TargetBoxGoal : ILevelGoal
    {
        public bool IsMet(Board board)
        {
            var level = board.Level;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                var target = level.TargetAt(cell);
                if (!target.HasValue) continue;
                if (!(board[cell] is Piece piece) || piece.Species != target.Value) return false;
            }
            return true;
        }
    }
}
