using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public enum LossCause
    {
        Unknown,
        Cleared,
        Replaced,
        Poisoned,
        Eaten,
    }

    // Vì sao nước vừa đi làm thua: màu nào hết mèo và điều gì đã lấy đi những con cuối cùng.
    public sealed class LossReport
    {
        private LossReport(Species color, LossCause cause, int count)
        {
            Color = color;
            Cause = cause;
            Count = count;
        }

        public Species Color { get; }

        public LossCause Cause { get; }

        // Số mèo màu đó mất ở bước cuối cùng.
        public int Count { get; }

        // Trả null nếu nước đi không làm mất màu nào.
        public static LossReport Of(MoveResult move)
        {
            var extinct = GameOutcome.ExtinctColors(move.After);
            if (extinct.Count == 0) return null;
            var color = extinct[0];

            foreach (var stage in move.Stages.Reverse())
            {
                var lost = stage.Events.Where(turnEvent => turnEvent.Piece.Species == color && CauseOf(turnEvent.Kind) != LossCause.Unknown).ToList();
                if (lost.Count > 0) return new LossReport(color, CauseOf(lost[0].Kind), lost.Count);
            }
            return move.Replaced is Piece replaced && replaced.Species == color
                ? new LossReport(color, LossCause.Replaced, 1)
                : new LossReport(color, LossCause.Unknown, 0);
        }

        private static LossCause CauseOf(TurnEventKind kind) => kind switch
        {
            TurnEventKind.Cleared => LossCause.Cleared,
            TurnEventKind.Poisoned => LossCause.Poisoned,
            TurnEventKind.Eaten => LossCause.Eaten,
            _ => LossCause.Unknown,
        };
    }
}
