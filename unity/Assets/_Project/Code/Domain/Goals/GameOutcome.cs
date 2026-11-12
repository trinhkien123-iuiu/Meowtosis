using System.Collections.Generic;
using System.Linq;

namespace Meowtosis.Domain
{
    // Kiểm tra thắng/thua, dùng chung cho phiên chơi, Gợi ý và bộ giải.
    public static class GameOutcome
    {
        // Mọi điều kiện thắng của level đều đạt. Bàn vẫn có thể đang thua: xem IsWon.
        public static bool IsComplete(Board board)
        {
            foreach (var goal in board.Level.Goals)
            {
                if (!goal.IsMet(board)) return false;
            }
            return true;
        }

        // Một màu tuyệt chủng khi lúc đầu có mặt mà nay không còn con nào, kể cả đang ngủ,
        // hoặc là màu yêu cầu của hộp mục tiêu mà nay không còn con nào (kể cả mèo hai màu mang màu đó).
        // Vi khuẩn và virus không bao giờ tính.
        public static IReadOnlyList<Species> ExtinctColors(Board board)
        {
            var extinct = board.Level.StartingColors.Where(color => !board.Contains(color)).ToList();
            if (board.Level.HasTargets)
            {
                for (var cell = 0; cell < board.CellCount; cell++)
                {
                    var target = board.Level.TargetAt(cell);
                    if (target.HasValue && !CanProduceColor(board, target.Value) && !extinct.Contains(target.Value))
                    {
                        extinct.Add(target.Value);
                    }
                }
            }
            return extinct;
        }

        public static bool IsLost(Board board)
        {
            foreach (var color in board.Level.StartingColors)
            {
                if (!board.Contains(color)) return true;
            }
            if (board.Level.HasTargets)
            {
                for (var cell = 0; cell < board.CellCount; cell++)
                {
                    var target = board.Level.TargetAt(cell);
                    if (target.HasValue && !CanProduceColor(board, target.Value)) return true;
                }
            }
            return false;
        }

        private static bool CanProduceColor(Board board, Species color)
        {
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (board[cell] is Piece piece)
                {
                    if (piece.Species == color || (piece.IsTwoColor && piece.OtherColor == color))
                        return true;
                }
            }
            return false;
        }

        public static bool IsWon(Board board) => IsComplete(board) && !IsLost(board);
    }
}
