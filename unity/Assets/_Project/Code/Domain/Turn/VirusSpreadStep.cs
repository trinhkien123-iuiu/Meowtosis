using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Mỗi virus trên bàn lan thêm một ô theo hướng nó quay: vào ô trống, hoặc ăn
    // mèo hay vi khuẩn đang đứng đó. Không lan ra ngoài bàn, qua màng chặn hay vào virus khác.
    // Virus vừa mọc trong lượt này phải đợi lượt sau mới lan.
    public sealed class VirusSpreadStep : ITurnStep
    {
        public TurnStage Apply(Board board)
        {
            var sources = new List<int>();
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (board[cell] is Piece piece && piece.IsVirus) sources.Add(cell);
            }
            if (sources.Count == 0) return TurnStage.Unchanged(board);

            var level = board.Level;
            var pieces = board.CopyPieces();
            var events = new List<TurnEvent>();
            var nextPieceId = board.NextPieceId;
            foreach (var source in sources)
            {
                var facing = pieces[source].Value.Facing;
                var target = level.Step(source, facing);
                if (target == Level.NoCell || (pieces[target] is Piece blocker && blocker.IsVirus)) continue;

                if (pieces[target] is Piece eaten) events.Add(new TurnEvent(TurnEventKind.Eaten, target, eaten));
                var grown = new Piece(nextPieceId++, Species.Virus, false, facing);
                pieces[target] = grown;
                events.Add(new TurnEvent(TurnEventKind.VirusSpread, target, grown, source));
            }
            return events.Count == 0 ? TurnStage.Unchanged(board) : new TurnStage(board.With(pieces, nextPieceId), events);
        }
    }
}
