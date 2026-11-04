using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Mèo ngủ đứng cạnh mèo thức cùng màu thì thức dậy. Lặp lại tới khi không còn thay đổi,
    // nên việc thức dậy lan dây chuyền. Màng không chặn việc đánh thức.
    public sealed class WakeStep : ITurnStep
    {
        public TurnStage Apply(Board board)
        {
            var events = new List<TurnEvent>();
            var woken = WakeAll(board, events);
            return new TurnStage(woken, events);
        }

        // Cũng dùng cho bàn lúc đầu. Truyền events = null khi không cần ghi lại.
        internal static Board WakeAll(Board board, List<TurnEvent> events)
        {
            var level = board.Level;
            var pieces = board.CopyPieces();
            var anyWoken = false;
            for (var changed = true; changed;)
            {
                changed = false;
                for (var cell = 0; cell < pieces.Length; cell++)
                {
                    if (!(pieces[cell] is Piece piece) || !piece.IsAsleep || !HasAwakeNeighbour(level, pieces, cell, piece.Species)) continue;

                    var awake = piece.WakeUp();
                    pieces[cell] = awake;
                    events?.Add(new TurnEvent(TurnEventKind.WokeUp, cell, awake));
                    changed = true;
                    anyWoken = true;
                }
            }
            return anyWoken ? board.With(pieces) : board;
        }

        private static bool HasAwakeNeighbour(Level level, Piece?[] pieces, int cell, Species species)
        {
            foreach (var neighbour in level.Neighbours(cell))
            {
                if (pieces[neighbour] is Piece other && !other.IsAsleep && other.Species == species) return true;
            }
            return false;
        }
    }
}
