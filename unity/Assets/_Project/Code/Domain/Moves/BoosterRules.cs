using System;
using System.Linq;

namespace Meowtosis.Domain
{
    // Hai booster làm đổi bàn chơi. Cả hai kết thúc lượt như một lần vuốt (virus vẫn lan).
    public sealed class BoosterRules
    {
        private readonly TurnResolver turnResolver;

        public BoosterRules(TurnResolver turnResolver)
        {
            this.turnResolver = turnResolver;
        }

        // Hoán đổi: biến một mèo thành mèo màu khác có trong level. Mèo ngủ thì thức dậy.
        // Không dùng được cho virus, mèo hai màu và con mèo cuối cùng của một màu.
        public bool TryMutate(Board board, int cell, Species color, out MoveResult result)
        {
            result = null;
            var level = board.Level;
            if (!(board[cell] is Piece piece) || piece.IsVirus || piece.IsTwoColor || piece.Species == color || !level.StartingColors.Contains(color)) return false;
            if (level.StartingColors.Contains(piece.Species) && board.Count(other => other.Species == piece.Species) == 1) return false;

            var pieces = board.CopyPieces();
            pieces[cell] = new Piece(board.NextPieceId, color);
            result = Finish(board, pieces, cell, cell, piece);
            return true;
        }

        // Nhân bản: bản sao của quân ở `from` mọc ở một ô trống bất kỳ, không đẩy gì.
        // Không dùng được cho virus và không cấy vào ô độc.
        public bool TryTransplant(Board board, int from, int to, out MoveResult result)
        {
            result = null;
            var level = board.Level;
            if (!(board[from] is Piece piece) || piece.IsVirus || !level.IsSlot(to) || level.IsToxic(to) || board[to].HasValue) return false;

            var pieces = board.CopyPieces();
            pieces[to] = new Piece(board.NextPieceId, piece.Species);
            result = Finish(board, pieces, from, to, null);
            return true;
        }

        private MoveResult Finish(Board board, Piece?[] pieces, int sourceCell, int placedCell, Piece? replaced)
        {
            var placed = board.With(pieces, board.NextPieceId + 1);
            return new MoveResult(board, placed, sourceCell, placedCell, Array.Empty<CellShift>(), replaced, turnResolver.Resolve(placed));
        }
    }
}
