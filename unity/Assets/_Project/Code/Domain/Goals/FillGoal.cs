namespace Meowtosis.Domain
{
    // Mọi ô của bàn (trừ ô độc) có mèo hoặc vi khuẩn. Virus không tính là đã lấp.
    public sealed class FillGoal : ILevelGoal
    {
        public bool IsMet(Board board)
        {
            var level = board.Level;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (!level.IsSlot(cell) || level.IsToxic(cell)) continue;
                if (!(board[cell] is Piece piece) || piece.IsVirus) return false;
            }
            return true;
        }
    }
}
