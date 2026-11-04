using System;
using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Một lần vuốt: mèo nhân đôi, bản sao vào ô kế bên theo hướng vuốt.
    // Ô đó có quân thì cả dãy bị đẩy tới ô trống gần nhất. Hết chỗ đẩy
    // (mép bàn, lỗ, màng chặn, hoặc mèo ngủ đứng như bức tường) thì bản sao thay thế quân kề.
    // Không vuốt được mèo ngủ và virus; bản sao không bao giờ thay thế mèo ngủ hoặc quân cùng loài.
    public sealed class SwipeRule
    {
        private readonly TurnResolver turnResolver;

        public SwipeRule(TurnResolver turnResolver)
        {
            this.turnResolver = turnResolver;
        }

        public bool TrySwipe(Board board, Move move, out MoveResult result) =>
            TrySwipe(board, board.Level.IndexOf(move.Row, move.Column), move.Direction, out result);

        public bool TrySwipe(Board board, int cell, Direction direction, out MoveResult result)
        {
            result = null;
            var level = board.Level;
            if (!(board[cell] is Piece parent) || !parent.CanSwipe) return false;
            var target = level.Step(cell, direction);
            if (target == Level.NoCell) return false;

            var line = new List<int>();
            var end = target;
            while (end != Level.NoCell && board[end] is Piece piece && !piece.IsAsleep)
            {
                line.Add(end);
                end = level.Step(end, direction);
            }
            var hasRoom = end != Level.NoCell && !board[end].HasValue;

            var pieces = board.CopyPieces();
            IReadOnlyList<CellShift> pushes = Array.Empty<CellShift>();
            Piece? replaced = null;
            if (hasRoom)
            {
                var shifts = new List<CellShift>(line.Count);
                for (var index = line.Count - 1; index >= 0; index--)
                {
                    var from = line[index];
                    var to = level.Step(from, direction);
                    pieces[to] = board[from];
                    shifts.Add(new CellShift(from, to));
                }
                pushes = shifts;
            }
            else
            {
                var neighbour = board[target].Value;
                if (neighbour.Species == parent.Species || neighbour.IsAsleep) return false;
                replaced = neighbour;
            }

            pieces[target] = new Piece(board.NextPieceId, parent.Species);
            var placed = board.With(pieces, board.NextPieceId + 1);
            result = new MoveResult(board, placed, cell, target, pushes, replaced, turnResolver.Resolve(placed), direction);
            return true;
        }

        // Mọi cú vuốt người chơi có thể thử: mỗi mèo thức hoặc vi khuẩn, mỗi hướng (có thể bị từ chối).
        public static IEnumerable<(int Cell, Direction Direction)> Candidates(Board board)
        {
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (!(board[cell] is Piece piece) || !piece.CanSwipe) continue;
                foreach (var direction in Directions.All) yield return (cell, direction);
            }
        }
    }
}
