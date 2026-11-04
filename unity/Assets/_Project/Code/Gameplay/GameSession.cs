using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public enum SessionStatus
    {
        Playing,
        Won,
        Lost,
    }

    // Một lượt chơi một level: bàn hiện tại, các nước đã đi và lịch sử để hoàn tác.
    // Thắng hoặc thua thì không nhận thêm nước, nhưng vẫn hoàn tác được.
    public sealed class GameSession
    {
        private readonly Stack<PlayedMove> history = new Stack<PlayedMove>();

        public GameSession(LevelDefinition definition)
        {
            Definition = definition;
            Level = new Level(definition);
            Board = Level.CreateStartBoard();
        }

        public LevelDefinition Definition { get; }

        public Level Level { get; }

        public Board Board { get; private set; }

        public int MovesMade => history.Count;

        public bool CanUndo => history.Count > 0;

        // Các bàn trước đó, gần nhất trước.
        public IEnumerable<Board> PreviousBoards => history.Select(move => move.Before);

        public SessionStatus Status
        {
            get
            {
                if (GameOutcome.IsLost(Board)) return SessionStatus.Lost;
                return GameOutcome.IsComplete(Board) ? SessionStatus.Won : SessionStatus.Playing;
            }
        }

        public bool TryPlay(IGameCommand command, out MoveResult result)
        {
            result = null;
            if (Status != SessionStatus.Playing || !command.TryApply(Board, out result)) return false;

            history.Push(new PlayedMove(Board, command.Booster));
            Board = result.After;
            return true;
        }

        // Lùi một nước; `refundedBooster` là booster nước đó đã tiêu (None nếu là vuốt thường).
        public bool TryUndo(out BoosterKind refundedBooster)
        {
            refundedBooster = BoosterKind.None;
            if (!CanUndo) return false;
            var move = history.Pop();
            Board = move.Before;
            refundedBooster = move.Booster;
            return true;
        }

        public void Restart()
        {
            history.Clear();
            Board = Level.CreateStartBoard();
        }

        private readonly struct PlayedMove
        {
            public PlayedMove(Board before, BoosterKind booster)
            {
                Before = before;
                Booster = booster;
            }

            public Board Before { get; }

            public BoosterKind Booster { get; }
        }
    }
}
