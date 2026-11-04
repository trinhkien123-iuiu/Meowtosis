using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Ô độc giết mọi thứ đứng trên nó.
    public sealed class PoisonStep : ITurnStep
    {
        public TurnStage Apply(Board board)
        {
            var level = board.Level;
            Piece?[] pieces = null;
            List<TurnEvent> events = null;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (!level.IsToxic(cell) || !(board[cell] is Piece piece)) continue;
                if (pieces == null)
                {
                    pieces = board.CopyPieces();
                    events = new List<TurnEvent>();
                }
                pieces[cell] = null;
                events.Add(new TurnEvent(TurnEventKind.Poisoned, cell, piece));
            }
            return pieces == null ? TurnStage.Unchanged(board) : new TurnStage(board.With(pieces), events);
        }
    }
}
