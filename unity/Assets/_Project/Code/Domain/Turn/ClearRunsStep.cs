using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Mọi dãy ngang hoặc dọc từ 3 quân cùng loài trở lên biến mất.
    // Mèo ngủ cũng tính; virus không bao giờ tạo dãy.
    public sealed class ClearRunsStep : ITurnStep
    {
        private const int MinimumRunLength = 3;

        public TurnStage Apply(Board board)
        {
            var runCells = FindRuns(board);
            if (runCells.Count == 0) return TurnStage.Unchanged(board);

            var pieces = board.CopyPieces();
            var events = new List<TurnEvent>(runCells.Count);
            foreach (var cell in runCells)
            {
                events.Add(new TurnEvent(TurnEventKind.Cleared, cell, board[cell].Value));
                pieces[cell] = null;
            }
            return new TurnStage(board.With(pieces), events);
        }

        // Các ô thuộc một dãy, mỗi ô một lần, theo thứ tự ô.
        public static IReadOnlyList<int> FindRuns(Board board)
        {
            var level = board.Level;
            var inRun = new bool[board.CellCount];
            for (var row = 0; row < level.Height; row++) MarkRuns(board, level.IndexOf(row, 0), 1, level.Width, inRun);
            for (var column = 0; column < level.Width; column++) MarkRuns(board, column, level.Width, level.Height, inRun);

            var cells = new List<int>();
            for (var cell = 0; cell < inRun.Length; cell++)
            {
                if (inRun[cell]) cells.Add(cell);
            }
            return cells;
        }

        // Duyệt một đường dài `length` ô (một hàng khi stride là 1, một cột khi stride là bề rộng).
        private static void MarkRuns(Board board, int firstCell, int stride, int length, bool[] inRun)
        {
            var runStart = 0;
            for (var position = 1; position <= length; position++)
            {
                var previous = RunSpecies(board, firstCell + (position - 1) * stride);
                var current = position < length ? RunSpecies(board, firstCell + position * stride) : null;
                if (current.HasValue && current == previous) continue;

                if (previous.HasValue && position - runStart >= MinimumRunLength)
                {
                    for (var member = runStart; member < position; member++) inRun[firstCell + member * stride] = true;
                }
                runStart = position;
            }
        }

        // Loài mà ô góp vào dãy, hoặc null với ô trống và virus.
        private static Species? RunSpecies(Board board, int cell) =>
            board[cell] is Piece piece && !piece.IsVirus ? piece.Species : (Species?)null;
    }
}
