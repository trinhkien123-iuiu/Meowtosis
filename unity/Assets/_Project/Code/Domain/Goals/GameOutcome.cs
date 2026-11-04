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

        // Một màu tuyệt chủng khi lúc đầu có mặt mà nay không còn con nào, kể cả đang ngủ.
        // Vi khuẩn và virus không bao giờ tính.
        public static IReadOnlyList<Species> ExtinctColors(Board board) =>
            board.Level.StartingColors.Where(color => !board.Contains(color)).ToList();

        public static bool IsLost(Board board)
        {
            foreach (var color in board.Level.StartingColors)
            {
                if (!board.Contains(color)) return true;
            }
            return false;
        }

        public static bool IsWon(Board board) => IsComplete(board) && !IsLost(board);
    }
}
