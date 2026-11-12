using System;
using System.Collections.Generic;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public enum HintKind
    {
        Move,           // vuốt Move; còn ít nhất MovesLeft nước
        Undo,           // đi lệch đường thắng: hoàn tác UndoSteps nước rồi xin gợi ý lại
        NotWinnable,    // từ đây không thắng được nữa
        TooHard,        // thế cờ rối quá, chưa tìm ra trong thời gian cho phép
    }

    public readonly struct Hint
    {
        public Hint(HintKind kind, Move move = default, int movesLeft = 0, int undoSteps = 0)
        {
            Kind = kind;
            Move = move;
            MovesLeft = movesLeft;
            UndoSteps = undoSteps;
        }

        public HintKind Kind { get; }

        public Move Move { get; }

        public int MovesLeft { get; }

        public int UndoSteps { get; }
    }

    // Gợi ý như prototype: còn đi đúng lời giải đã lưu thì lấy luôn nước tiếp (tức thì); lệch thì giải lại có giới hạn
    // thời gian; vẫn không ra thì chỉ số nước cần hoàn tác để về thế cờ gần nhất còn nằm trên lời giải đã biết.
    public sealed class HintAdvisor
    {
        public static readonly TimeSpan DefaultTimeLimit = TimeSpan.FromMilliseconds(600);

        private readonly SwipeRule swipeRule;
        private readonly Solver solver;
        private LevelDefinition knownDefinition;
        private Dictionary<string, int> knownPositions;

        public HintAdvisor(SwipeRule swipeRule, Solver solver)
        {
            this.swipeRule = swipeRule;
            this.solver = solver;
        }

        public Hint Suggest(GameSession session, TimeSpan timeLimit)
            => Suggest(session.Board, session.Definition, session.PreviousBoards, timeLimit);

        // Callers running in the background must pass a captured board and copied history.
        public Hint Suggest(Board current, LevelDefinition definition, IEnumerable<Board> previousBoards, TimeSpan timeLimit)
        {
            var positions = KnownPositions(definition, current.Level);
            var solution = definition.Solution;
            if (positions.TryGetValue(current.ToKey(), out var step) && step < solution.Count)
                return new Hint(HintKind.Move, solution[step], solution.Count - step);

            var solved = solver.Solve(current, Solver.DefaultMaxStates, timeLimit);
            if (solved.IsSolved && solved.Moves.Count > 0) return new Hint(HintKind.Move, solved.Moves[0], solved.Moves.Count);

            var stepsBack = 0;
            foreach (var board in previousBoards)
            {
                stepsBack++;
                if (positions.ContainsKey(board.ToKey())) return new Hint(HintKind.Undo, undoSteps: stepsBack);
            }
            return new Hint(solved.Status == SolveStatus.LimitReached ? HintKind.TooHard : HintKind.NotWinnable);
        }

        // Khoá của mọi thế cờ trên lời giải đã lưu, kèm số nước đã đi tới đó. Tính một lần cho mỗi level.
        private Dictionary<string, int> KnownPositions(LevelDefinition definition, Level level)
        {
            if (ReferenceEquals(knownDefinition, definition)) return knownPositions;
            knownDefinition = definition;
            knownPositions = new Dictionary<string, int>();
            var board = level.CreateStartBoard();
            knownPositions[board.ToKey()] = 0;
            var solution = definition.Solution;
            for (var step = 0; step < solution.Count; step++)
            {
                if (!swipeRule.TrySwipe(board, solution[step], out var result)) break;
                board = result.After;
                knownPositions[board.ToKey()] = step + 1;
            }
            return knownPositions;
        }
    }
}
