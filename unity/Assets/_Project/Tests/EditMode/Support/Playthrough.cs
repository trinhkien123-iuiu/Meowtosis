using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Tests
{
    // Chơi lần lượt các nước vuốt ghi dạng "hàng cột hướng" và giữ lại kết quả.
    // Dừng ở nước đầu tiên bị từ chối.
    internal sealed class Playthrough
    {
        private Playthrough(Board board)
        {
            Board = board;
        }

        public Board Board { get; private set; }

        public MoveResult LastResult { get; private set; }

        public bool LastMoveRefused { get; private set; }

        public IReadOnlyList<string> Rows => Board.ToRows();

        public bool IsLost => GameOutcome.IsLost(Board);

        public bool IsWon => GameOutcome.IsWon(Board);

        public int VirusCount => Board.Count(piece => piece.IsVirus);

        public IEnumerable<TurnEvent> LastEvents(TurnEventKind kind) =>
            LastResult.Stages.SelectMany(stage => stage.Events).Where(turnEvent => turnEvent.Kind == kind);

        public static Playthrough Run(Board start, IEnumerable<string> moves)
        {
            var playthrough = new Playthrough(start);
            foreach (var move in moves)
            {
                if (!Rules.Swipes.TrySwipe(playthrough.Board, Move.Parse(move), out var result))
                {
                    playthrough.LastMoveRefused = true;
                    playthrough.LastResult = null;
                    break;
                }
                playthrough.LastResult = result;
                playthrough.Board = result.After;
            }
            return playthrough;
        }
    }
}
