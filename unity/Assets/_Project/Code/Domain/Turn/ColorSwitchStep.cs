using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Mèo hai màu (đang thức) đếm lùi một lượt; về 0 thì đổi sang màu kia rồi đếm lại từ chu kỳ.
    // Chạy ngay trước khử 3 nên màu mới tham gia khử 3 của chính lượt đó. Mèo ngủ không đếm.
    public sealed class ColorSwitchStep : ITurnStep
    {
        public TurnStage Apply(Board board)
        {
            Piece?[] pieces = null;
            List<TurnEvent> events = null;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (!(board[cell] is Piece piece) || !piece.IsTwoColor || piece.IsAsleep) continue;
                if (pieces == null)
                {
                    pieces = board.CopyPieces();
                    events = new List<TurnEvent>();
                }
                var counted = piece.CountDown();
                pieces[cell] = counted;
                // Đổi màu: màu cũ nằm ở OtherColor của quân mới.
                if (counted.Species != piece.Species) events.Add(new TurnEvent(TurnEventKind.ColorChanged, cell, counted));
            }
            return pieces == null ? TurnStage.Unchanged(board) : new TurnStage(board.With(pieces), events);
        }
    }
}
